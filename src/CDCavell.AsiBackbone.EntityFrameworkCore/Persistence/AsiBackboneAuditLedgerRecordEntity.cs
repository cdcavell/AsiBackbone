using CDCavell.AsiBackbone.Core.Actors;
using CDCavell.AsiBackbone.Core.Entities;

namespace CDCavell.AsiBackbone.EntityFrameworkCore.Persistence;

/// <summary>
/// Represents the Entity Framework Core persistence shape for an AsiBackbone audit ledger record.
/// </summary>
public sealed class AsiBackboneAuditLedgerRecordEntity : AsiBackboneEntity
{
    /// <summary>
    /// Gets or sets the stable audit ledger record identifier.
    /// </summary>
    public string RecordId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the stable audit event identifier.
    /// </summary>
    public string EventId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the UTC timestamp when the audited event occurred.
    /// </summary>
    public DateTimeOffset OccurredUtc { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when the ledger record was created by the host or storage adapter.
    /// </summary>
    public DateTimeOffset RecordedUtc { get; set; }

    /// <summary>
    /// Gets or sets the stable actor identifier associated with the event.
    /// </summary>
    public string ActorId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the actor type associated with the event.
    /// </summary>
    public AsiBackboneActorType ActorType { get; set; }

    /// <summary>
    /// Gets or sets the optional display name or label associated with the actor.
    /// </summary>
    public string? ActorDisplayName { get; set; }

    /// <summary>
    /// Gets or sets the operation name associated with the audited event.
    /// </summary>
    public string OperationName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the governance, constraint, or host-defined outcome associated with the event.
    /// </summary>
    public string Outcome { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the serialized machine-readable reason codes associated with the event.
    /// </summary>
    public string ReasonCodesJson { get; set; } = "[]";

    /// <summary>
    /// Gets or sets the correlation identifier associated with the event, when supplied by the host.
    /// </summary>
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Gets or sets the trace identifier associated with the event, when supplied by the host.
    /// </summary>
    public string? TraceId { get; set; }

    /// <summary>
    /// Gets or sets the policy version associated with the event, when supplied by the host.
    /// </summary>
    public string? PolicyVersion { get; set; }

    /// <summary>
    /// Gets or sets the policy hash associated with the event, when supplied by the host.
    /// </summary>
    public string? PolicyHash { get; set; }

    /// <summary>
    /// Gets or sets the related responsibility or liability handshake identifier, when available.
    /// </summary>
    public string? HandshakeId { get; set; }

    /// <summary>
    /// Gets or sets the related acknowledgment identifier, when available.
    /// </summary>
    public string? AcknowledgmentId { get; set; }

    /// <summary>
    /// Gets or sets the related capability token identifier, when available.
    /// </summary>
    public string? CapabilityTokenId { get; set; }

    /// <summary>
    /// Gets or sets the previous ledger record hash, when supplied by a host or signing package.
    /// </summary>
    public string? PreviousRecordHash { get; set; }

    /// <summary>
    /// Gets or sets this ledger record hash, when supplied by a host or signing package.
    /// </summary>
    public string? RecordHash { get; set; }

    /// <summary>
    /// Gets or sets the signature key identifier, when supplied by a signing package or host.
    /// </summary>
    public string? SignatureKeyId { get; set; }

    /// <summary>
    /// Gets or sets the signature algorithm, when supplied by a signing package or host.
    /// </summary>
    public string? SignatureAlgorithm { get; set; }

    /// <summary>
    /// Gets or sets the signature value, when supplied by a signing package or host.
    /// </summary>
    public string? SignatureValue { get; set; }

    /// <summary>
    /// Gets or sets serialized framework-neutral audit metadata supplied by the host.
    /// </summary>
    public string MetadataJson { get; set; } = "{}";
}
