using AsiBackbone.Core.Emissions;

namespace AsiBackbone.Core.Outbox;

/// <summary>
/// Defines an opt-in claim-capable governance outbox store for coordinated multi-worker emission.
/// </summary>
/// <remarks>
/// This contract is additive to <see cref="IAsiBackboneGovernanceOutboxStore" />. Hosts opt in when scaled workers need to claim a lease before provider emission. Claim support reduces duplicate selection risk for cooperating workers, but it does not create exactly-once provider delivery. Claim-aware implementations may