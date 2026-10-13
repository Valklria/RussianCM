using Content.Server.CMU14.AdminAI;
using Content.Tools.AdminAI.Spike;
using NUnit.Framework;

namespace Content.Tools.AdminAI.Tests;

[TestFixture]
public sealed class UdsSpikeTests
{
    [Test]
    public async Task DisabledClientDoesNotConnectToSocket()
    {
        using var client = new UdsSpikeClient("/run/rucmu-ai/nonexistent.sock", new());
        Assert.That(await client.CheckHealthAsync(CancellationToken.None), Is.EqualTo(AIWorkerHealth.Disabled));
        Assert.That((await client.InferAsync(new(Guid.NewGuid(), "synthetic"), CancellationToken.None)).Status,
            Is.EqualTo(AIInferenceStatus.Disabled));
    }

    [Test]
    public async Task MissingWorkerAndCancelledRequestFailWithoutDraft()
    {
        using var client = new UdsSpikeClient("/run/rucmu-ai/nonexistent.sock", new(Enabled: true, TimeoutSeconds: 1));
        var result = await client.InferAsync(new(Guid.NewGuid(), "synthetic"), CancellationToken.None);
        Assert.That(result.Status, Is.EqualTo(AIInferenceStatus.Unavailable));
        Assert.That(result.UntrustedText, Is.Null);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        result = await client.InferAsync(new(Guid.NewGuid(), "synthetic"), cancellation.Token);
        Assert.That(result.Status, Is.EqualTo(AIInferenceStatus.Cancelled));
    }

    [Test, Category("LocalIPC")]
    public async Task ActualHttpOverUnixSocketRoundTrip()
    {
        var path = Environment.GetEnvironmentVariable("RUCMU_AI_TEST_SOCKET");
        if (string.IsNullOrWhiteSpace(path))
            Assert.Ignore("Set RUCMU_AI_TEST_SOCKET to a short, writable absolute .sock path. IPC gate remains unverified.");

        using var worker = new MockWorker(path!);
        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var workerTask = worker.RunAsync(cancellation.Token);
        try
        {
            using var client = new UdsSpikeClient(path!, new(Enabled: true, TimeoutSeconds: 2));
            Assert.That(await client.CheckHealthAsync(cancellation.Token), Is.EqualTo(AIWorkerHealth.Ready));
            var result = await client.InferAsync(new(Guid.NewGuid(), "synthetic probe"), cancellation.Token);
            Assert.That(result.Status, Is.EqualTo(AIInferenceStatus.Success));
            Assert.That(result.UntrustedText, Does.StartWith("[MOCK WORKER: NO MODEL]"));
        }
        finally
        {
            cancellation.Cancel();
            try { await workerTask; }
            catch (OperationCanceledException) { }
        }
    }
}
