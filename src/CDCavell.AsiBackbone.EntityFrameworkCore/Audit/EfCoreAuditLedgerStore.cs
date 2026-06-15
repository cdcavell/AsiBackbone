using System.Collections.ObjectModel;
using System.Text.Json;
using CDCavell.AsiBackbone.Core.Actors;
using CDCavell.AsiBackbone.Core.Audit;
using CDCavell.AsiBackbone.Core.Results;
using CDCavell.AsiBackbone.EntityFrameworkCore.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CDCavell.AsiBackbone.EntityFrameworkCore.Audit;

/// <summary>
/// Entity Framework Core-backed audit ledger store that persists records through a host-owned <see cref="DbContext" />.
/// </summary>
/// <remarks>
/// This store is append-oriented and intentionally relies on the host application to expose the ASI Backbone entities from
/// its own <see cref="DbContext" /> and migrations. It does not create a package-owned context or select a database provider.
/// </remarks>
public sealed class EfCoreAuditLedgerStore : IAsiBackboneAuditLedgerStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly DbContext dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="EfCoreAuditLedgerStore" /> class.
    /// </summary>
    /// <param name="dbContext">The host-owned database context.</param>
    public EfCoreAuditLedgerStore(DbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);

        this.dbContext = dbContext;
    }

    /// <inheritdoc />
    public async ValueTask<OperationResult<AuditLedgerRecord>> AppendAsync(
        AuditLedgerRecord record,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(record);
        cancellationToken.ThrowIfCancellationRequested();

        AsiBackboneAuditLedgerRecordEntity entity = ToEntity(record);

        _ = await dbContext
            .Set<AsiBackboneAuditLedgerRecordEntity>()
            .AddAsync(entity, cancellationToken)
            .ConfigureAwait(false);

        foreach (AsiBackboneAuditLedgerReasonCodeEntity reasonCode in ToReasonCodeEntities(entity.Id, record.ReasonCodes))
        {
            _ = await dbContext
                .Set<AsiBackboneAuditLedgerReasonCodeEntity>()
                .AddAsync(reasonCode, cancellationToken)
                .ConfigureAwait(false);
        }

        foreach (AsiBackboneAuditLedgerMetadataEntity metadata in ToMetadataEntities(entity.Id, record.Metadata))
        {
            _ = await dbContext
                .Set<AsiBackboneAuditLedgerMetadataEntity>()
                .AddAsync(metadata, cancellationToken)
                .ConfigureAwait(false);
        }

        try
        {
            _ = await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (DbUpdateException ex)
        {
            dbContext.ChangeTracker.Clear();

            return OperationResult.Failure<AuditLedgerRecord>(
                "asi_backbone.audit_ledger.append_failed",
                ex.Message);
        }

        return OperationResult.Success(record);
    }

    /// <inheritdoc />
    public async ValueTask<AuditLedgerRecord?> FindByRecordIdAsync(
        string recordId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(recordId);

        string normalizedRecordId = recordId.Trim();

        AsiBackboneAuditLedgerRecordEntity? entity = await LedgerRecords()
            .Where(record => record.RecordId == normalizedRecordId)
            .SingleOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        return entity is null ? null : ToRecord(entity);
    }

    /// <inheritdoc />
    public async ValueTask<IReadOnlyList<AuditLedgerRecord>> FindByCorrelationIdAsync(
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);

        string normalizedCorrelationId = correlationId.Trim();

        List<AsiBackboneAuditLedgerRecordEntity> entities = await LedgerRecords()
            .Where(record => record.CorrelationId == normalizedCorrelationId)
            .OrderBy(record => record.RecordedUtc)
            .ThenBy(record => record.RecordId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return ToRecords(entities);
    }

    /// <inheritdoc />
    public async ValueTask<IReadOnlyList<AuditLedgerRecord>> FindByTraceIdAsync(
        string traceId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(traceId);

        string normalizedTraceId = traceId.Trim();

        List<AsiBackboneAuditLedgerRecordEntity> entities = await LedgerRecords()
            .Where(record => record.TraceId == normalizedTraceId)
            .OrderBy(record => record.RecordedUtc)
            .ThenBy(record => record.RecordId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return ToRecords(entities);
    }

    /// <inheritdoc />
    public async ValueTask<IReadOnlyList<AuditLedgerRecord>> FindByActorIdAsync(
        string actorId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(actorId);

        string normalizedActorId = actorId.Trim();

        List<AsiBackboneAuditLedgerRecordEntity> entities = await LedgerRecords()
            .Where(record => record.ActorId == normalizedActorId)
            .OrderBy(record => record.RecordedUtc)
            .ThenBy(record => record.RecordId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return ToRecords(entities);
    }

    /// <inheritdoc />
    public async ValueTask<IReadOnlyList<AuditLedgerRecord>> FindByRecordedUtcRangeAsync(
        DateTimeOffset recordedFromUtc,
        DateTimeOffset recordedToUtc,
        CancellationToken cancellationToken = default)
    {
        DateTimeOffset normalizedFromUtc = recordedFromUtc.ToUniversalTime();
        DateTimeOffset normalizedToUtc = recordedToUtc.ToUniversalTime();

        if (normalizedFromUtc > normalizedToUtc)
        {
            throw new ArgumentException(
                "The recorded UTC range start must be less than or equal to the range end.",
                nameof(recordedFromUtc));
        }

        List<AsiBackboneAuditLedgerRecordEntity> entities = await LedgerRecords()
            .Where(record => record.RecordedUtc >= normalizedFromUtc && record.RecordedUtc <= normalizedToUtc)
            .OrderBy(record => record.RecordedUtc)
            .ThenBy(record => record.RecordId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return ToRecords(entities);
    }

    private IQueryable<AsiBackboneAuditLedgerRecordEntity> LedgerRecords()
    {
        return dbContext.Set<AsiBackboneAuditLedgerRecordEntity>().AsNoTracking();
    }

    private static AsiBackboneAuditLedgerRecordEntity ToEntity(AuditLedgerRecord record)
    {
        return new AsiBackboneAuditLedgerRecordEntity
        {
            RecordId = record.RecordId,
            SchemaVersion = record.SchemaVersion,
            EventId = record.EventId,
            AuditResidueId = record.AuditResidueId,
            OccurredUtc = record.OccurredUtc,
            RecordedUtc = record.RecordedUtc,
            ActorId = record.ActorId,
            ActorType = record.ActorType,
            ActorDisplayName = record.ActorDisplayName,
            OperationName = record.OperationName,
            Outcome = record.Outcome,
            ReasonCodesJson = JsonSerializer.Serialize(record.ReasonCodes, JsonOptions),
            CorrelationId = record.CorrelationId,
            TraceId = record.TraceId,
            SpanId = record.SpanId,
            ParentSpanId = record.ParentSpanId,
            DecisionLatencyMs = record.DecisionLatencyMs,
            ConstraintSetHash = record.ConstraintSetHash,
            ConstraintCount = record.ConstraintCount,
            RiskScore = record.RiskScore,
            PolicyScope = record.PolicyScope,
            TenantHash = record.TenantHash,
            OrganizationHash = record.OrganizationHash,
            EmitterStatus = record.EmitterStatus,
            EmitterProvider = record.EmitterProvider,
            OutboxSequence = record.OutboxSequence,
            GatewayExecutionId = record.GatewayExecutionId,
            DecisionStage = record.DecisionStage,
            PolicyVersion = record.PolicyVersion,
            PolicyHash = record.PolicyHash,
            HandshakeId = record.HandshakeId,
            AcknowledgmentId = record.AcknowledgmentId,
            CapabilityTokenId = record.CapabilityTokenId,
            PreviousRecordHash = record.PreviousRecordHash,
            RecordHash = record.RecordHash,
            SignatureKeyId = record.SignatureKeyId,
            SignatureAlgorithm = record.SignatureAlgorithm,
            SignatureValue = record.SignatureValue,
            MetadataJson = JsonSerializer.Serialize(record.Metadata, JsonOptions)
        };
    }

    private static AsiBackboneAuditLedgerReasonCodeEntity[] ToReasonCodeEntities(
        Guid auditLedgerRecordId,
        IReadOnlyList<string> reasonCodes)
    {
        return [.. reasonCodes
            .Select((reasonCode, index) => new AsiBackboneAuditLedgerReasonCodeEntity
            {
                AuditLedgerRecordId = auditLedgerRecordId,
                Sequence = index,
                ReasonCode = reasonCode
            })];
    }

    private static AsiBackboneAuditLedgerMetadataEntity[] ToMetadataEntities(
        Guid auditLedgerRecordId,
        IReadOnlyDictionary<string, string> metadata)
    {
        return [.. metadata
            .Select(item => new AsiBackboneAuditLedgerMetadataEntity
            {
                AuditLedgerRecordId = auditLedgerRecordId,
                MetadataKey = item.Key,
                MetadataValue = item.Value
            })];
    }

    private static AuditLedgerRecord[] ToRecords(IEnumerable<AsiBackboneAuditLedgerRecordEntity> entities)
    {
        return [.. entities.Select(ToRecord)];
    }

    private static AuditLedgerRecord ToRecord(AsiBackboneAuditLedgerRecordEntity entity)
    {
        string[] reasonCodes = DeserializeReasonCodes(entity.ReasonCodesJson);
        ReadOnlyDictionary<string, string> metadata = DeserializeMetadata(entity.MetadataJson);

        var residue = new EntityAuditResidue(
            entity.EventId,
            entity.AuditResidueId,
            entity.SchemaVersion,
            entity.OccurredUtc,
            entity.ActorId,
            entity.ActorType,
            entity.ActorDisplayName,
            entity.OperationName,
            entity.Outcome,
            Array.AsReadOnly(reasonCodes),
            entity.CorrelationId,
            entity.TraceId,
            entity.SpanId,
            entity.ParentSpanId,
            entity.DecisionLatencyMs,
            entity.ConstraintSetHash,
            entity.ConstraintCount,
            entity.RiskScore,
            entity.PolicyScope,
            entity.TenantHash,
            entity.OrganizationHash,
            entity.EmitterStatus,
            entity.EmitterProvider,
            entity.OutboxSequence,
            entity.GatewayExecutionId,
            entity.DecisionStage,
            entity.PolicyVersion,
            entity.PolicyHash,
            metadata);

        return AuditLedgerRecord.FromResidue(
            residue,
            entity.RecordId,
            entity.RecordedUtc,
            entity.HandshakeId,
            entity.AcknowledgmentId,
            entity.CapabilityTokenId,
            entity.PreviousRecordHash,
            entity.RecordHash,
            entity.SignatureKeyId,
            entity.SignatureAlgorithm,
            entity.SignatureValue,
            schemaVersion: entity.SchemaVersion);
    }

    private static string[] DeserializeReasonCodes(string? json)
    {
        return string.IsNullOrWhiteSpace(json)
            ? []
            : JsonSerializer.Deserialize<string[]>(json, JsonOptions) ?? [];
    }

    private static ReadOnlyDictionary<string, string> DeserializeMetadata(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new ReadOnlyDictionary<string, string>(new Dictionary<string, string>(StringComparer.Ordinal));
        }

        Dictionary<string, string>? metadata = JsonSerializer.Deserialize<Dictionary<string, string>>(json, JsonOptions);

        return metadata is null || metadata.Count == 0
            ? new ReadOnlyDictionary<string, string>(new Dictionary<string, string>(StringComparer.Ordinal))
            : new ReadOnlyDictionary<string, string>(new Dictionary<string, string>(metadata, StringComparer.Ordinal));
    }

    private sealed class EntityAuditResidue(
        string eventId,
        string? auditResidueId,
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
        IReadOnlyDictionary<string, string> metadata) : IAsiBackboneAuditResidue
    {
        public string EventId { get; } = eventId;

        public string? AuditResidueId { get; } = auditResidueId;

        public string SchemaVersion { get; } = schemaVersion;

        public DateTimeOffset OccurredUtc { get; } = occurredUtc;

        public string ActorId { get; } = actorId;

        public AsiBackboneActorType ActorType { get; } = actorType;

        public string? ActorDisplayName { get; } = actorDisplayName;

        public string OperationName { get; } = operationName;

        public string Outcome { get; } = outcome;

        public IReadOnlyList<string> ReasonCodes { get; } = reasonCodes;

        public string? CorrelationId { get; } = correlationId;

        public string? TraceId { get; } = traceId;

        public string? SpanId { get; } = spanId;

        public string? ParentSpanId { get; } = parentSpanId;

        public long? DecisionLatencyMs { get; } = decisionLatencyMs;

        public string? ConstraintSetHash { get; } = constraintSetHash;

        public int? ConstraintCount { get; } = constraintCount;

        public double? RiskScore { get; } = riskScore;

        public string? PolicyScope { get; } = policyScope;

        public string? TenantHash { get; } = tenantHash;

        public string? OrganizationHash { get; } = organizationHash;

        public string? EmitterStatus { get; } = emitterStatus;

        public string? EmitterProvider { get; } = emitterProvider;

        public long? OutboxSequence { get; } = outboxSequence;

        public string? GatewayExecutionId { get; } = gatewayExecutionId;

        public string? DecisionStage { get; } = decisionStage;

        public string? PolicyVersion { get; } = policyVersion;

        public string? PolicyHash { get; } = policyHash;

        public IReadOnlyDictionary<string, string> Metadata { get; } = metadata;
    }
}
