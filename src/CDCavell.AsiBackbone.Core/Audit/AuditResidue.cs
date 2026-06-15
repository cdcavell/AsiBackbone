using System.Collections.ObjectModel;
using CDCavell.AsiBackbone.Core.Actors;
using CDCavell.AsiBackbone.Core.Constraints;
using CDCavell.AsiBackbone.Core.Decisions;
using CDCavell.AsiBackbone.Core.Serialization;

namespace CDCavell.AsiBackbone.Core.Audit;

/// <summary>
/// Represents the framework-neutral audit residue produced by an AsiBackbone operation.
/// </summary>
public sealed class AuditResidue : IAsiBackboneAuditResidue
{
    private static readonly ReadOnlyCollection<string> EmptyReasonCodes =
        Array.AsReadOnly(Array.Empty<string>());

    private static readonly IReadOnlyDictionary<string, string> EmptyMetadata =
        new ReadOnlyDictionary<string, string>(
            new Dictionary<string, string>(StringComparer.Ordinal));

    private AuditResidue(
        string eventId,
        string auditResidueId,
        string schemaVersion,
        DateTimeOffset occurredUtc,
        string actorId,
        AsiBackboneActorType actorType,
        string? actorDisplayName,
        string operationName,
        string outcome,
        IReadOnlyList<string> reasonCodes,
        string? correlationId,
        string? traceId,
        string? spanId,
        string? parentSpanId,
        long? decisionLatencyMs,
        string? constraintSetHash,
        int? constraintCount,
        double? riskScore,
        string? policyScope,
        string? tenantHash,
        string? organizationHash,
        string? emitterStatus,
        string? emitterProvider,
        long? outboxSequence,
        string? gatewayExecutionId,
        string? decisionStage,
        string? policyVersion,
        string? policyHash,
        IReadOnlyDictionary<string, string> metadata)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventId);
        ArgumentException.ThrowIfNullOrWhiteSpace(auditResidueId);
        ArgumentException.ThrowIfNullOrWhiteSpace(actorId);
        ArgumentException.ThrowIfNullOrWhiteSpace(operationName);
        ArgumentException.ThrowIfNullOrWhiteSpace(outcome);

        EventId = eventId.Trim();
        AuditResidueId = auditResidueId.Trim();
        SchemaVersion = AsiBackboneSchemaVersions.Normalize(schemaVersion);
        OccurredUtc = occurredUtc.ToUniversalTime();
        ActorId = actorId.Trim();
        ActorType = actorType;
        ActorDisplayName = NormalizeOptional(actorDisplayName);
        OperationName = operationName.Trim();
        Outcome = outcome.Trim();
        ReasonCodes = reasonCodes;
        CorrelationId = NormalizeOptional(correlationId);
        TraceId = NormalizeOptional(traceId);
        SpanId = NormalizeOptional(spanId);
        ParentSpanId = NormalizeOptional(parentSpanId);
        DecisionLatencyMs = NormalizeNonNegative(decisionLatencyMs, nameof(decisionLatencyMs));
        ConstraintSetHash = NormalizeOptional(constraintSetHash);
        ConstraintCount = NormalizeNonNegative(constraintCount, nameof(constraintCount));
        RiskScore = NormalizeRiskScore(riskScore);
        PolicyScope = NormalizeOptional(policyScope);
        TenantHash = NormalizeOptional(tenantHash);
        OrganizationHash = NormalizeOptional(organizationHash);
        EmitterStatus = NormalizeOptional(emitterStatus);
        EmitterProvider = NormalizeOptional(emitterProvider);
        OutboxSequence = NormalizeNonNegative(outboxSequence, nameof(outboxSequence));
        GatewayExecutionId = NormalizeOptional(gatewayExecutionId);
        DecisionStage = NormalizeOptional(decisionStage);
        PolicyVersion = NormalizeOptional(policyVersion);
        PolicyHash = NormalizeOptional(policyHash);
        Metadata = metadata;
    }

    /// <inheritdoc />
    public string EventId { get; }

    /// <inheritdoc />
    public string AuditResidueId { get; }

    /// <inheritdoc />
    public string SchemaVersion { get; }

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
    public string? SpanId { get; }

    /// <inheritdoc />
    public string? ParentSpanId { get; }

    /// <inheritdoc />
    public long? DecisionLatencyMs { get; }

    /// <inheritdoc />
    public string? ConstraintSetHash { get; }

    /// <inheritdoc />
    public int? ConstraintCount { get; }

    /// <inheritdoc />
    public double? RiskScore { get; }

    /// <inheritdoc />
    public string? PolicyScope { get; }

    /// <inheritdoc />
    public string? TenantHash { get; }

    /// <inheritdoc />
    public string? OrganizationHash { get; }

    /// <inheritdoc />
    public string? EmitterStatus { get; }

    /// <inheritdoc />
    public string? EmitterProvider { get; }

    /// <inheritdoc />
    public long? OutboxSequence { get; }

    /// <inheritdoc />
    public string? GatewayExecutionId { get; }

    /// <inheritdoc />
    public string? DecisionStage { get; }

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
    /// <param name="auditResidueId">Optional audit residue identifier. When omitted, the normalized event identifier is used.</param>
    /// <param name="spanId">Optional span identifier.</param>
    /// <param name="parentSpanId">Optional parent span identifier.</param>
    /// <param name="decisionLatencyMs">Optional decision latency in milliseconds.</param>
    /// <param name="constraintSetHash">Optional evaluated constraint-set hash.</param>
    /// <param name="constraintCount">Optional evaluated constraint count.</param>
    /// <param name="riskScore">Optional host-defined risk score.</param>
    /// <param name="policyScope">Optional host-defined policy scope.</param>
    /// <param name="tenantHash">Optional privacy-preserving tenant hash.</param>
    /// <param name="organizationHash">Optional privacy-preserving organization hash.</param>
    /// <param name="emitterStatus">Optional provider-neutral emitter status.</param>
    /// <param name="emitterProvider">Optional provider-neutral emitter provider name.</param>
    /// <param name="outboxSequence">Optional outbox sequence.</param>
    /// <param name="gatewayExecutionId">Optional gateway execution identifier.</param>
    /// <param name="decisionStage">Optional provider-neutral decision stage.</param>
    /// <param name="schemaVersion">Optional schema version. When omitted, the stable artifact schema version is used.</param>
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
        IReadOnlyDictionary<string, string>? metadata = null,
        string? auditResidueId = null,
        string? spanId = null,
        string? parentSpanId = null,
        long? decisionLatencyMs = null,
        string? constraintSetHash = null,
        int? constraintCount = null,
        double? riskScore = null,
        string? policyScope = null,
        string? tenantHash = null,
        string? organizationHash = null,
        string? emitterStatus = null,
        string? emitterProvider = null,
        long? outboxSequence = null,
        string? gatewayExecutionId = null,
        string? decisionStage = null,
        string? schemaVersion = null)
    {
        ArgumentNullException.ThrowIfNull(actor);

        string normalizedEventId = NormalizeIdentifier(eventId);

        return new AuditResidue(
            normalizedEventId,
            NormalizeAuditResidueId(auditResidueId, normalizedEventId),
            schemaVersion ?? AsiBackboneSchemaVersions.StableArtifactsV1,
            occurredUtc ?? DateTimeOffset.UtcNow,
            actor.ActorId,
            actor.ActorType,
            actor.DisplayName,
            operationName,
            outcome,
            NormalizeReasonCodes(reasonCodes),
            correlationId,
            traceId,
            spanId,
            parentSpanId,
            decisionLatencyMs,
            constraintSetHash,
            constraintCount,
            riskScore,
            policyScope,
            tenantHash,
            organizationHash,
            emitterStatus,
            emitterProvider,
            outboxSequence,
            gatewayExecutionId,
            decisionStage,
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
    /// <param name="auditResidueId">Optional audit residue identifier. When omitted, the normalized event identifier is used.</param>
    /// <param name="spanId">Optional span identifier.</param>
    /// <param name="parentSpanId">Optional parent span identifier.</param>
    /// <param name="decisionLatencyMs">Optional decision latency in milliseconds.</param>
    /// <param name="constraintSetHash">Optional evaluated constraint-set hash.</param>
    /// <param name="constraintCount">Optional evaluated constraint count.</param>
    /// <param name="riskScore">Optional host-defined risk score.</param>
    /// <param name="policyScope">Optional host-defined policy scope.</param>
    /// <param name="tenantHash">Optional privacy-preserving tenant hash.</param>
    /// <param name="organizationHash">Optional privacy-preserving organization hash.</param>
    /// <param name="emitterStatus">Optional provider-neutral emitter status.</param>
    /// <param name="emitterProvider">Optional provider-neutral emitter provider name.</param>
    /// <param name="outboxSequence">Optional outbox sequence.</param>
    /// <param name="gatewayExecutionId">Optional gateway execution identifier.</param>
    /// <param name="decisionStage">Optional provider-neutral decision stage.</param>
    /// <param name="schemaVersion">Optional schema version. When omitted, the stable artifact schema version is used.</param>
    /// <returns>An audit residue value.</returns>
    public static AuditResidue FromDecision(
        IAsiBackboneActorContext actor,
        string operationName,
        GovernanceDecision decision,
        string? eventId = null,
        DateTimeOffset? occurredUtc = null,
        IReadOnlyDictionary<string, string>? metadata = null,
        string? auditResidueId = null,
        string? spanId = null,
        string? parentSpanId = null,
        long? decisionLatencyMs = null,
        string? constraintSetHash = null,
        int? constraintCount = null,
        double? riskScore = null,
        string? policyScope = null,
        string? tenantHash = null,
        string? organizationHash = null,
        string? emitterStatus = null,
        string? emitterProvider = null,
        long? outboxSequence = null,
        string? gatewayExecutionId = null,
        string? decisionStage = null,
        string? schemaVersion = null)
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
            metadata,
            auditResidueId,
            spanId,
            parentSpanId,
            decisionLatencyMs,
            constraintSetHash,
            constraintCount,
            riskScore,
            policyScope,
            tenantHash,
            organizationHash,
            emitterStatus,
            emitterProvider,
            outboxSequence,
            gatewayExecutionId,
            decisionStage,
            schemaVersion);
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
    /// <param name="auditResidueId">Optional audit residue identifier. When omitted, the normalized event identifier is used.</param>
    /// <param name="spanId">Optional span identifier.</param>
    /// <param name="parentSpanId">Optional parent span identifier.</param>
    /// <param name="decisionLatencyMs">Optional decision latency in milliseconds.</param>
    /// <param name="constraintSetHash">Optional evaluated constraint-set hash.</param>
    /// <param name="constraintCount">Optional evaluated constraint count.</param>
    /// <param name="riskScore">Optional host-defined risk score.</param>
    /// <param name="policyScope">Optional host-defined policy scope.</param>
    /// <param name="tenantHash">Optional privacy-preserving tenant hash.</param>
    /// <param name="organizationHash">Optional privacy-preserving organization hash.</param>
    /// <param name="emitterStatus">Optional provider-neutral emitter status.</param>
    /// <param name="emitterProvider">Optional provider-neutral emitter provider name.</param>
    /// <param name="outboxSequence">Optional outbox sequence.</param>
    /// <param name="gatewayExecutionId">Optional gateway execution identifier.</param>
    /// <param name="decisionStage">Optional provider-neutral decision stage.</param>
    /// <param name="schemaVersion">Optional schema version. When omitted, the stable artifact schema version is used.</param>
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
        IReadOnlyDictionary<string, string>? metadata = null,
        string? auditResidueId = null,
        string? spanId = null,
        string? parentSpanId = null,
        long? decisionLatencyMs = null,
        string? constraintSetHash = null,
        int? constraintCount = null,
        double? riskScore = null,
        string? policyScope = null,
        string? tenantHash = null,
        string? organizationHash = null,
        string? emitterStatus = null,
        string? emitterProvider = null,
        long? outboxSequence = null,
        string? gatewayExecutionId = null,
        string? decisionStage = null,
        string? schemaVersion = null)
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
            metadata,
            auditResidueId,
            spanId,
            parentSpanId,
            decisionLatencyMs,
            constraintSetHash,
            constraintCount,
            riskScore,
            policyScope,
            tenantHash,
            organizationHash,
            emitterStatus,
            emitterProvider,
            outboxSequence,
            gatewayExecutionId,
            decisionStage,
            schemaVersion);
    }

    private static string NormalizeIdentifier(string? identifier)
    {
        return string.IsNullOrWhiteSpace(identifier)
            ? Guid.NewGuid().ToString("N")
            : identifier.Trim();
    }

    private static string NormalizeAuditResidueId(string? auditResidueId, string eventId)
    {
        return string.IsNullOrWhiteSpace(auditResidueId)
            ? eventId
            : auditResidueId.Trim();
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    private static long? NormalizeNonNegative(long? value, string parameterName)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(parameterName, value, "Value must be greater than or equal to zero.");
        }

        return value;
    }

    private static int? NormalizeNonNegative(int? value, string parameterName)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(parameterName, value, "Value must be greater than or equal to zero.");
        }

        return value;
    }

    private static double? NormalizeRiskScore(double? riskScore)
    {
        if (riskScore is null)
        {
            return null;
        }

        if (double.IsNaN(riskScore.Value) || double.IsInfinity(riskScore.Value) || riskScore.Value < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(riskScore), riskScore, "Risk score must be a finite value greater than or equal to zero.");
        }

        return riskScore;
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
            : new ReadOnlyDictionary<string, string>(normalizedMetadata);
    }
}
