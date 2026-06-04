using CDCavell.ASIBackbone.Core.Actors;

namespace CDCavell.ASIBackbone.Core.Audit;

/// <summary>
/// Defines the framework-neutral audit residue produced by an ASIBackbone operation.
/// </summary>
public interface IAsiBackboneAuditResidue
{
    /// <summary>
    /// Gets the stable audit event identifier.
    /// </summary>
    string EventId { get; }

    /// <summary>
    /// Gets the UTC timestamp when the audited event occurred.
    /// </summary>
    DateTimeOffset OccurredUtc { get; }

    /// <summary>
    /// Gets the stable actor identifier associated with the event.
    /// </summary>
    string ActorId { get; }

    /// <summary>
    /// Gets the actor type associated with the event.
    /// </summary>
    AsiBackboneActorType ActorType { get; }

    /// <summary>
    /// Gets the optional display name or label associated with the actor.
    /// </summary>
    string? ActorDisplayName { get; }

    /// <summary>
    /// Gets the operation name associated with the audited event.
    /// </summary>
    string OperationName { get; }

    /// <summary>
    /// Gets the governance, constraint, or host-defined outcome associated with the event.
    /// </summary>
    string Outcome { get; }

    /// <summary>
    /// Gets machine-readable reason codes associated with the event.
    /// </summary>
    IReadOnlyList<string> ReasonCodes { get; }

    /// <summary>
    /// Gets the correlation identifier associated with the event, when supplied by the host.
    /// </summary>
    string? CorrelationId { get; }

    /// <summary>
    /// Gets the trace identifier associated with the event, when supplied by the host.
    /// </summary>
    string? TraceId { get; }

    /// <summary>
    /// Gets the policy version associated with the event, when supplied by the host.
    /// </summary>
    string? PolicyVersion { get; }

    /// <summary>
    /// Gets the policy hash associated with the event, when supplied by the host.
    /// </summary>
    string? PolicyHash { get; }

    /// <summary>
    /// Gets additional framework-neutral audit metadata supplied by the host.
    /// </summary>
    IReadOnlyDictionary<string, string> Metadata { get; }
}
