using System.Collections.ObjectModel;
using CDCavell.AsiBackbone.Core.Actors;
using CDCavell.AsiBackbone.Core.Serialization;

namespace CDCavell.AsiBackbone.Core.Audit;

/// <summary>
/// Represents a persistence-ready audit ledger record captured from AsiBackbone audit residue.
/// </summary>
public sealed class AuditLedgerRecord : IAsiBackboneAuditResidue
{
    private static readonly ReadOnlyCollection<string> EmptyReasonCodes =
        Array.AsReadOnly(Array.Empty<string>());

    private static readonly IReadOnlyDictionary<string, string> EmptyMetadata =
        new ReadOnlyDictionary<string, string>(
            new Dictionary<string, string>(StringComparer.Ordinal));

    private AuditLedgerRecord(
        string recordId,
        string? schemaVersion,
        string eventId,
        string? auditResidueId,
        DateTimeOffset occurredUtc,
        DateTimeOffset recordedUtc,
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
        string? handshakeId,
        string? acknowledgmentId,
        string? capabilityTokenId,
        string? previousRecordHash,
        string? recordHash,
        string? signatureKeyId,
        string? signatureAlgorithm,
        string? signatureValue,
        IReadOnlyDictionary<string, string> metadata)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(recordId);
        ArgumentException.ThrowIfNullOrWhiteSpace(eventId);
        ArgumentException.ThrowIfNullOrWhiteSpace(actorId);
        ArgumentException.ThrowIfNullOrWhiteSpace(operationName);
        ArgumentException.ThrowIfNullOrWhiteSpace(outcome);

        RecordId = recordId.Trim();
        SchemaVersion = AsiBackboneSchemaVersions.Normalize(schemaVersion);
        EventId = eventId.Trim();
        AuditResidueId = NormalizeOptional(auditResidueId) ?? EventId;
        OccurredUtc = occurredUtc.ToUniversalTime();
        RecordedUtc = recordedUtc.ToUniversalTime();
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
        HandshakeId = NormalizeOptional(handshakeId);
        AcknowledgmentId = NormalizeOptional(acknowledgmentId);
        CapabilityTokenId = NormalizeOptional(capabilityTokenId);
        PreviousRecordHash = NormalizeOptional(previousRecordHash);
        RecordHash = NormalizeOptional(recordHash);
        SignatureKeyId = NormalizeOptional(signatureKeyId);
        SignatureAlgorithm = NormalizeOptional(signatureAlgorithm);
        SignatureValue = NormalizeOptional(signatureValue);
        Metadata = metadata;
    }

    /// <summary>
    /// Gets the stable audit ledger record identifier.
    /// </summary>
    public string RecordId { get; }

    /// <summary>
    /// Gets the schema version for the serialized audit ledger record shape.
    /// </summary>
    public string SchemaVersion { get; }

    /// <inheritdoc />
    public string EventId { get; }

    /// <inheritdoc />
    public string AuditResidueId { get; }

    /// <inheritdoc />
    public DateTimeOffset OccurredUtc { get; }

    /// <summary>
    /// Gets the UTC timestamp when the ledger record was created by the host or storage adapter.
    /// </summary>
    public DateTimeOffset RecordedUtc { get; }

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

    /// <summary>
    /// Gets the related responsibility or liability handshake identifier, when available.
    /// </summary>
    public string? HandshakeId { get; }

    /// <summary>
    /// Gets the related acknowledgment identifier, when available.
    /// </summary>
    public string? AcknowledgmentId { get; }

    /// <summary>
    /// Gets the related capability token identifier, when available.
    /// </summary>
    public string? CapabilityTokenId { get; }

    /// <summary>
    /// Gets the previous ledger record hash, when supplied by a host or signing package.
    /// </summary>
    public string? PreviousRecordHash { get; }

    /// <summary>
    /// Gets this ledger record hash, when supplied by a host or signing package.
    /// </summary>
    public string? RecordHash { get; }

    /// <summary>
    /// Gets the signature key identifier, when supplied by a signing package or host.
    /// </summary>
    public string? SignatureKeyId { get; }

    /// <summary>
    /// Gets the signature algorithm, when supplied by a signing package or host.
    /// </summary>
    public string? SignatureAlgorithm { get; }

    /// <summary>
    /// Gets the signature value, when supplied by a signing package or host.
    /// </summary>
    public string? SignatureValue { get; }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, string> Metadata { get; }

    /// <summary>
    /// Gets a value indicating whether this ledger record contains reason codes.
    /// </summary>
    public bool HasReasonCodes => ReasonCodes.Count > 0;

    /// <summary>
    /// Gets a value indicating whether this ledger record contains metadata.
    /// </summary>
    public bool HasMetadata => Metadata.Count > 0;

    /// <summary>
    /// Creates a persistent audit ledger record from audit residue.
    /// </summary>
    public static AuditLedgerRecord FromResidue(
        IAsiBackboneAuditResidue residue,
        string? recordId = null,
        DateTimeOffset? recordedUtc = null,
        string? handshakeId = null,
        string? acknowledgmentId = null,
        string? capabilityTokenId = null,
        string? previousRecordHash = null,
        string? recordHash = null,
        string? signatureKeyId = null,
        string? signatureAlgorithm = null,
        string? signatureValue = null,
        IReadOnlyDictionary<string, string>? metadata = null,
        string? schemaVersion = null)
    {
        ArgumentNullException.ThrowIfNull(residue);

        return new AuditLedgerRecord(
            NormalizeIdentifier(recordId),
            schemaVersion ?? residue.SchemaVersion,
            residue.EventId,
            residue.AuditResidueId,
            residue.OccurredUtc,
            recordedUtc ?? DateTimeOffset.UtcNow,
            residue.ActorId,
            residue.ActorType,
            residue.ActorDisplayName,
            residue.OperationName,
            residue.Outcome,
            NormalizeReasonCodes(residue.ReasonCodes),
            residue.CorrelationId,
            residue.TraceId,
            residue.SpanId,
            residue.ParentSpanId,
            residue.DecisionLatencyMs,
            residue.ConstraintSetHash,
            residue.ConstraintCount,
            residue.RiskScore,
            residue.PolicyScope,
            residue.TenantHash,
            residue.OrganizationHash,
            residue.EmitterStatus,
            residue.EmitterProvider,
            residue.OutboxSequence,
            residue.GatewayExecutionId,
            residue.DecisionStage,
            residue.PolicyVersion,
            residue.PolicyHash,
            handshakeId,
            acknowledgmentId,
            capabilityTokenId,
            previousRecordHash,
            recordHash,
            signatureKeyId,
            signatureAlgorithm,
            signatureValue,
            NormalizeMetadata(residue.Metadata, metadata));
    }

    private static string NormalizeIdentifier(string? identifier)
    {
        return string.IsNullOrWhiteSpace(identifier)
            ? Guid.NewGuid().ToString("N")
            : identifier.Trim();
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    private static long? NormalizeNonNegative(long? value, string parameterName)
    {
        return value < 0
            ? throw new ArgumentOutOfRangeException(parameterName, value, "Value must be greater than or equal to zero.")
            : value;
    }

    private static int? NormalizeNonNegative(int? value, string parameterName)
    {
        return value < 0
            ? throw new ArgumentOutOfRangeException(parameterName, value, "Value must be greater than or equal to zero.")
            : value;
    }

    private static double? NormalizeRiskScore(double? riskScore)
    {
        return riskScore is null
            ? null
            : double.IsNaN(riskScore.Value) || double.IsInfinity(riskScore.Value) || riskScore.Value < 0
            ? throw new ArgumentOutOfRangeException(nameof(riskScore), riskScore, "Risk score must be a finite value greater than or equal to zero.")
            : riskScore;
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
        params IReadOnlyDictionary<string, string>?[] metadataSets)
    {
        Dictionary<string, string> normalizedMetadata = new(StringComparer.Ordinal);

        foreach (IReadOnlyDictionary<string, string>? metadata in metadataSets)
        {
            if (metadata is null || metadata.Count == 0)
            {
                continue;
            }

            foreach (KeyValuePair<string, string> item in metadata)
            {
                if (string.IsNullOrWhiteSpace(item.Key))
                {
                    continue;
                }

                normalizedMetadata[item.Key.Trim()] = item.Value?.Trim() ?? string.Empty;
            }
        }

        return normalizedMetadata.Count == 0
            ? EmptyMetadata
            : new ReadOnlyDictionary<string, string>(normalizedMetadata);
    }
}
