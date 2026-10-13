using System;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.CMU14.AdminAI;
using Content.Tools.AdminAI.Spike;
using NUnit.Framework;
using Robust.Server;
using Robust.Shared;
using Robust.Shared.Utility;
using Robust.UnitTesting;

namespace Content.IntegrationTests.CMU14.AdminAI;

/// <summary>Actual Content.Server transport on an isolated engine server thread, without game prototypes or players.</summary>
[TestFixture, NonParallelizable, Category("LocalIPC")]
public sealed class AdminAIServerIpcTests
{
    private static (string Socket, string Resources) Paths()
    {
        var socket = Environment.GetEnvironmentVariable("RUCMU_AI_TEST_SOCKET");
        var resources = Environment.GetEnvironmentVariable("RUCMU_AI_ENGINE_RESOURCES");
        if (string.IsNullOrWhiteSpace(socket) || string.IsNullOrWhiteSpace(resources))
            Assert.Ignore("Set RUCMU_AI_TEST_SOCKET and RUCMU_AI_ENGINE_RESOURCES for the real server-thread IPC spike.");
        return (socket!, resources!);
    }

    private static RobustIntegrationTest.ServerIntegrationInstance StartServer(string resources)
    {
        return new(new RobustIntegrationTest.ServerIntegrationOptions
        {
            ContentStart = false,
            ContentAssemblies = [],
            Options = new ServerOptions
            {
                LoadConfigAndUserData = false,
                LoadContentResources = false,
                PrototypeDirectory = new ResPath("/AdminAIStageAEmptyPrototypes"),
                MountOptions = new MountOptions(dirMounts: [resources], zipMounts: []),
            },
        });
    }

    [Test]
    public async Task CompiledContentServerCanCheckWorkerFromServerThread()
    {
        var paths = Paths();
        using var worker = new MockWorker(paths.Socket);
        using var lifetime = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        var workerTask = worker.RunAsync(lifetime.Token);
        UdsAIInferenceClient? client = null;
        try
        {
            using var server = StartServer(paths.Resources);
            await server.WaitIdleAsync();
            Task<AIWorkerHealth>? pending = null;
            await server.WaitPost(() =>
            {
                client = new UdsAIInferenceClient(paths.Socket, new(Enabled: true, TimeoutSeconds: 2));
                pending = client.CheckHealthAsync(lifetime.Token);
            });
            Assert.That(await pending!, Is.EqualTo(AIWorkerHealth.Ready));
            await server.WaitAssertion(() => Assert.That(server.IsAlive, Is.True));
        }
        finally
        {
            client?.Dispose();
            lifetime.Cancel();
            try { await workerTask; }
            catch (OperationCanceledException) { }
        }
    }

    [Test]
    public async Task ServerThreadContinuesWhileWorkerRequestIsPending()
    {
        var paths = Paths();
        using var worker = new MockWorker(paths.Socket, MockWorkerFault.StallHeaders);
        using var lifetime = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        using var requestCancellation = new CancellationTokenSource();
        var workerTask = worker.RunAsync(lifetime.Token);
        UdsAIInferenceClient? client = null;
        try
        {
            using var server = StartServer(paths.Resources);
            await server.WaitIdleAsync();
            Task<AIInferenceResult>? pending = null;
            await server.WaitPost(() =>
            {
                client = new UdsAIInferenceClient(paths.Socket, new(Enabled: true, TimeoutSeconds: 5));
                pending = client.InferAsync(new(Guid.NewGuid(), "synthetic server-thread spike"), requestCancellation.Token);
            });
            await worker.RequestReceived.WaitAsync(TimeSpan.FromSeconds(3));
            await server.WaitAssertion(() =>
            {
                Assert.That(server.IsAlive, Is.True);
                Assert.That(pending!.IsCompleted, Is.False, "The server must process posted actions while IPC awaits its worker.");
            });
            requestCancellation.Cancel();
            var result = await pending!.WaitAsync(TimeSpan.FromSeconds(3));
            Assert.That(result.Status, Is.EqualTo(AIInferenceStatus.Cancelled));
            Assert.That(result.UntrustedText, Is.Null);
            await server.WaitAssertion(() => Assert.That(server.IsAlive, Is.True));
        }
        finally
        {
            requestCancellation.Cancel();
            client?.Dispose();
            lifetime.Cancel();
            try { await workerTask; }
            catch (OperationCanceledException) { }
        }
    }
}
