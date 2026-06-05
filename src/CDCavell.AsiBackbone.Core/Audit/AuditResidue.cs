using System.Collections.ObjectModel;
using CDCavell.AsiBackbone.Core.Actors;
using CDCavell.AsiBackbone.Core.Constraints;
using CDCavell.AsiBackbone.Core.Decisions;

namespace CDCavell.AsiBackbone.Core.Audit;

/// <summary>
/// Represents the framework-neutral audit residue produced by an AsiBackbone operation.
/// </summary>
public sealed class AuditResidue : IAsiBackboneAuditResidue
{
    private static readonly ReadOnlyCollection<string> EmptyReasonCodes =
        Array.AsReadOnly(Array.Empty<string>());

    private static readonly IReadOnlyDictionary<string, string> EmptyMetadata =
        new Dictionary<string, string>(StringComparer.Ordinal);

    private AuditResidue(
        string eventId,
        DateTimeOffset occurredUtc,
        string actorId,
        AsiBackboneActorType actorType,
        string? actorDisplayName,
        string operationName,
        string outcome,
        IReadOnlyList<string> reasonCodes,
        string? correlationId,
        string? traceId,
        string? policyVersion,
        string? policyHash,
        IReadOnlyDictionary<string, string> metadata)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventId);
        ArgumentException.ThrowIfNullOrWhiteSpace(actorId);
        ArgumentException.ThrowIfNullOrWhiteSpace(operationName);
        ArgumentException.ThrowIfNullOrWhiteSpace(outcome);

        EventId = eventId.Trim();
        OccurredUtc = occurredUtc.ToUniversalTime();
        ActorId = actorId.Trim();
        ActorType = actorType;
        ActorDisplayName = NormalizeOptional(actorDisplayName);
        OperationName = operationName.Trim();
        Outcome = outcome.Trim();
        ReasonCodes = reasonCodes;
        CorrelationId = NormalizeOptional(correlationId);
        TraceId = NormalizeOptional(traceId);
        PolicyVersion = NormalizeOptional(policyVersion);
        PolicyHash = NormalizeOptional(policyHash);
        Metadata = metadata;
    }

    /// <inheritdoc />
    public string EventId { get; }

    /// <inheritdoc />
    public DateTimeOffset OccurredUtc { get; }

    /// <inheritdoc />
    public string ActorId { get; }

    /// <inheritdoc />
    public AsiBackboneActorType ActorType { get; }

    /// <inheritdoc />
    public string? ActorDisplayName { get; }

    /// <inheritdoc />
    public string OperationName { get; }

    /// <inheritdoc />
    public string Outcome { get; }

    /// <inheritdoc />
    public IReadOnlyList<string> ReasonCodes { get; }

    /// <inheritdoc />
    public string? CorrelationId { get; }

    /// <inheritdoc />
    public string? TraceId { get; }

    /// <inheritdoc />
    public string? PolicyVersion { get; }

    /// <inheritdoc />
    public string? PolicyHash { get; }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, string> Metadata { get; }

    /// <summary>
    /// Gets a value indicating whether this audit residue contains reason codes.
    /// </summary>
    public bool HasReasonCodes => ReasonCodes.Count > 0;

    /// <summary>
    /// Gets a value indicating whether this audit residue contains metadata.
    /// </summary>
    public bool HasMetadata => Metadata.Count > 0;

    /// <summary>
    /// Creates audit residue from a host-defined operation outcome.
    /// </summary>
    /// <param name="actor">The actor associated with the operation.</param>
    /// <param name="operationName">The operation name.</param>
    /// <param name="outcome">The governance, constraint, or host-defined outcome.</param>
    /// <param name="reasonCodes">Optional machine-readable reason codes.</param>
    /// <param name="eventId">Optional audit event identifier. When omitted, a new identifier is generated.</param>
    /// <param name="occurredUtc">Optional event timestamp. When omitted, the current UTC timestamp is used.</param>
    /// <param name="correlationId">Optional correlation identifier.</param>
    /// <param name="traceId">Optional trace identifier.</param>
    /// <param name="policyVersion">Optional policy version.</param>
    /// <param name="policyHash">Optional policy hash.</param>
    /// <param name="metadata">Optional host-provided audit metadata.</param>
    /// <returns>An audit residue value.</returns>
    public static AuditResidue Create(
        IAsiBackboneActorContext actor,
        string operationName,
        string outcome,
        IEnumerable<string>? reasonCodes = null,
        string? eventId = null,
        DateTimeOffset? occurredUtc = null,
        string? correlationId = null,
        string? traceId = null,
        string? policyVersion = null,
        string? policyHash = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        ArgumentNullException.ThrowIfNull(actor);

        return new AuditResidue(
            NormalizeEventId(eventId),
            occurredUtc ?? DateTimeOffset.UtcNow,
            actor.ActorId,
            actor.ActorType,
            actor.DisplayName,
            operationName,
            outcome,
            NormalizeReasonCodes(reasonCodes),
            correlationId,
            traceId,
            policyVersion,
            policyHash,
            NormalizeMetadata(metadata));
    }

    /// <summary>
    /// Creates audit residue from a governance decision.
    /// </summary>
    /// <param name="actor">The actor associated with the operation.</param>
    /// <param name="operationName">The operation name.</param>
    /// <param name="decision">The governance decision to audit.</param>
    /// <param name="eventId">Optional audit event identifier. When omitted, a new identifier is generated.</param>
    /// <param name="occurredUtc">Optional event timestamp. When omitted, the current UTC timestamp is used.</param>
    /// <param name="metadata">Optional host-provided audit metadata.</param>
    /// <returns>An audit residue value.</returns>
    public static AuditResidue FromDecision(
        IAsiBackboneActorContext actor,
        string operationName,
        GovernanceDecision decision,
        string? eventId = null,
        DateTimeOffset? occurredUtc = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        ArgumentNullException.ThrowIfNull(decision);

        return Create(
            actor,
            operationName,
            decision.Outcome.ToString(),
            decision.ReasonCodes,
            eventId,
            occurredUtc,
            decision.CorrelationId,
            decision.TraceId,
            decision.PolicyVersion,
            decision.PolicyHash,
            metadata);
    }

    /// <summary>
    /// Creates audit residue from a constraint evaluation result.
    /// </summary>
    /// <param name="actor">The actor associated with the operation.</param>
    /// <param name="operationName">The operation name.</param>
    /// <param name="constraintResult">The constraint evaluation result to audit.</param>
    /// <param name="eventId">Optional audit event identifier. When omitted, a new identifier is generated.</param>
    /// <param name="occurredUtc">Optional event timestamp. When omitted, the current UTC timestamp is used.</param>
    /// <param name="correlationId">Optional correlation identifier.</param>
    /// <param name="traceId">Optional trace identifier.</param>
    /// <param name="policyVersion">Optional policy version.</param>
    /// <param name="policyHash">Optional policy hash.</param>
    /// <param name="metadata">Optional host-provided audit metadata.</param>
    /// <returns>An audit residue value.</returns>
    public static AuditResidue FromConstraint(
        IAsiBackboneActorContext actor,
        string operationName,
        ConstraintEvaluationResult constraintResult,
        string? eventId = null,
        DateTimeOffset? occurredUtc = null,
        string? correlationId = null,
        string? traceId = null,
        string? policyVersion = null,
        string? policyHash = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        ArgumentNullException.ThrowIfNull(constraintResult);

        return Create(
            actor,
            operationName,
            constraintResult.Outcome.ToString(),
            constraintResult.ReasonCodes,
            eventId,
            occurredUtc,
            correlationId,
            traceId,
            policyVersion,
            policyHash,
            metadata);
    }

    private static string NormalizeEventId(string? eventId)
    {
        return string.IsNullOrWhiteSpace(eventId)
            ? Guid.NewGuid().ToString("N")
            : eventId.Trim();
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    private static ReadOnlyCollection<string> NormalizeReasonCodes(IEnumerable<string>? reasonCodes)
    {
        string[] normalizedReasonCodes = reasonCodes?
            .Where(reasonCode => !string.IsNullOrWhiteSpace(reasonCode))
            .Select(reasonCode => reasonCode.Trim())
            .ToArray() ?? [];

        return normalizedReasonCodes.Length == 0
            ? EmptyReasonCodes
            : Array.AsReadOnly(normalizedReasonCodes);
    }

    private static IReadOnlyDictionary<string, string> NormalizeMetadata(
        IReadOnlyDictionary<string, string>? metadata)
    {
        if (metadata is null || metadata.Count == 0)
        {
            return EmptyMetadata;
        }

        Dictionary<string, string> normalizedMetadata = new(StringComparer.Ordinal);

        foreach (KeyValuePair<string, string> item in metadata)
        {
            if (string.IsNullOrWhiteSpace(item.Key))
            {
                continue;
            }

            normalizedMetadata[item.Key.Trim()] = item.Value?.Trim() ?? string.Empty;
        }

        return normalizedMetadata.Count == 0
            ? EmptyMetadata
            : normalizedMetadata;
    }
}
