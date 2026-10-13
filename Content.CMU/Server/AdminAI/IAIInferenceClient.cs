using System;
using System.Threading;
using System.Threading.Tasks;

namespace Content.Server.CMU14.AdminAI;

/// <summary>
/// Local inference boundary. No game actions, sessions, database handles or tools are exposed here.
/// All successful output is untrusted data, never a command or a player-facing message.
/// </summary>
public interface IAIInferenceClient
{
    Task<AIWorkerHealth> CheckHealthAsync(CancellationToken cancellationToken);
    Task<AIInferenceResult> InferAsync(AIInferenceRequest request, CancellationToken cancellationToken);
}

public sealed record AIInferenceRequest(Guid CorrelationId, string Input);

public enum AIInferenceStatus : byte
{
    Disabled,
    Unavailable,
    TimedOut,
    Cancelled,
    InvalidResponse,
    Success,
}

public enum AIWorkerHealth : byte
{
    Disabled,
    Unavailable,
    Ready,
}

public sealed record AIInferenceResult(Guid CorrelationId, AIInferenceStatus Status, string? UntrustedText = null);
