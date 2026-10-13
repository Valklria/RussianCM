using Content.Server.GameTicking;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.CMU14.AdminAI;

/// <summary>Only the current round identifier; no round-control or game-action API is exposed to the coordinator.</summary>
public interface IAdminAIRoundContext { int CurrentRoundId { get; } }
public sealed partial class AdminAIRoundContext : IAdminAIRoundContext
{
    [Dependency] private IEntitySystemManager _systems = default!;
    public int CurrentRoundId => GameTicker.GetRoundId(_systems);
}
