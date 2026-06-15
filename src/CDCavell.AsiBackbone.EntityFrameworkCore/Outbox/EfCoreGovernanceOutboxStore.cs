using System.Collections.ObjectModel;
using System.Text.Json;
using CDCavell.AsiBackbone.Core.Emissions;
using CDCavell.AsiBackbone.Core.Entities;
using CDCavell.AsiBackbone.Core.Outbox;
using CDCavell.AsiBackbone.EntityFrameworkCore.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CDCavell.AsiBackbone.EntityFrameworkCore.Outbox;

/// <summary>
/// Entity Framework Core-backed governance outbox store that persists provider-neutral emission envelopes through a host-owned <see cref="DbContext" />.
/// </summary>
/// <remarks>
/// This store provides durable local storage only. Provider delivery, telemetry export, SIEM routing, and cloud emission remain downstream and optional.
/// </remarks>
public sealed class EfCoreGovernanceOutboxStore : IAsiBackboneGovernanceOutboxStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly DbContext dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="EfCoreGovernanceOutboxStore" /> class.
    /// </summary>
    /// <param name="dbContext">The host-owned database context.</param>
    public EfCoreGovernanceOutboxStore(DbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);

        this.dbContext = dbContext;
    }

    /// <inheritdoc />
    public async ValueTask<GovernanceOutboxEntry> EnqueueAsync(
        GovernanceEmissionEnvelope envelope,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        cancellationToken.ThrowIfCancellationRequested();

        GovernanceOutboxEntry entry = GovernanceOutboxEntry.Create(envelope);

        _ = await dbContext
            .Set<AsiBackboneGovernanceOutboxEntryEntity>()
            .AddAsync(ToEntity(entry), cancellationToken)
            .ConfigureAwait(false);

        _ = await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return entry;
    }

    /// <inheritdoc />
    public async ValueTask<GovernanceOutboxEntry> SaveAsync(
        GovernanceOutboxEntry entry,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);
        cancellationToken.ThrowIfCancellationRequested();

        AsiBackboneGovernanceOutboxEntryEntity persistedEntity = ToEntity(entry);
        AsiBackboneGovernanceOutboxEntryEntity? existingEntity = await dbContext
            .Set<AsiBackboneGovernanceOutboxEntryEntity>()
            .SingleOrDefaultAsync(entity => entity.OutboxEntryId == entry.OutboxEntryId, cancellationToken)
            .ConfigureAwait(false);

        if (existingEntity is null)
        {
            _ = await dbContext
                .Set<AsiBackboneGovernanceOutboxEntryEntity>()
                .AddAsync(persistedEntity, cancellationToken)
                .ConfigureAwait(false);
        }
        else
        {
            persistedEntity.Id = existingEntity.Id;
            persistedEntity.ConcurrencyStamp = AsiBackboneEntity.NewConcurrencyStamp();
            dbContext.Entry(existingEntity).CurrentValues.SetValues(persistedEntity);
        }

        _ = await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return entry;
    }

    /// <inheritdoc />
    public async ValueTask<GovernanceOutboxEntry?> FindByOutboxEntryIdAsync(
        string outboxEntryId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outboxEntryId);

        string normalizedOutboxEntryId = outboxEntryId.Trim();

        AsiBackboneGovernanceOutboxEntryEntity? entity = await OutboxEntries()
            .Where(outboxEntry => outboxEntry.OutboxEntryId == normalizedOutboxEntryId)
            .SingleOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        return entity is null ? null : ToEntry(entity);
    }

    /// <inheritdoc />
    public async ValueTask<IReadOnlyList<GovernanceOutboxEntry>> FindPendingAsync(
        int maxCount = 100,
        CancellationToken cancellationToken = default)
    {
        int normalizedMaxCount = NormalizeMaxCount(maxCount);

        List<AsiBackboneGovernanceOutboxEntryEntity> entities = await OutboxEntries()
            .Where(outboxEntry => outboxEntry.Status == GovernanceEmissionStatus.Pending)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return [.. ToEntries(entities)
            .OrderBy(entry => entry.CreatedUtc)
            .ThenBy(entry => entry.OutboxEntryId, StringComparer.Ordinal)
            .Take(normalizedMaxCount)];
    }

    /// <inheritdoc />
    public async ValueTask<IReadOnlyList<GovernanceOutboxEntry>> FindRetryReadyAsync(
        DateTimeOffset utcNow,
        int maxCount = 100,
        CancellationToken cancellationToken = default)
    {
        int normalizedMaxCount = NormalizeMaxCount(maxCount);
        DateTimeOffset normalizedUtcNow = utcNow.ToUniversalTime();

        List<AsiBackboneGovernanceOutboxEntryEntity> entities = await OutboxEntries()
            .Where(outboxEntry =>
                outboxEntry.Status == GovernanceEmissionStatus.Deferred ||
                outboxEntry.Status == GovernanceEmissionStatus.Failed ||
                outboxEntry.Status == GovernanceEmissionStatus.RetryableFailure)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return [.. ToEntries(entities)
            .Where(entry => entry.IsRetryReady(normalizedUtcNow))
            .OrderBy(entry => entry.NextRetryUtc ?? entry.UpdatedUtc)
            .ThenBy(entry => entry.OutboxEntryId, StringComparer.Ordinal)
            .Take(normalizedMaxCount)];
    }

    /// <inheritdoc />
    public async ValueTask<GovernanceOutboxEntry> MarkDeliveredAsync(
        string outboxEntryId,
        GovernanceEmissionResult result,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outboxEntryId);
        ArgumentNullException.ThrowIfNull(result);

        GovernanceOutboxEntry entry = await RequireEntryAsync(outboxEntryId, cancellationToken).ConfigureAwait(false);
        GovernanceOutboxEntry updatedEntry = entry.MarkDelivered(result);

        return await SaveAsync(updatedEntry, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask<GovernanceOutboxEntry> MarkFailedAsync(
        string outboxEntryId,
        GovernanceEmissionError governanceEmissionError,
        DateTimeOffset? nextRetryUtc = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outboxEntryId);
        ArgumentNullException.ThrowIfNull(governanceEmissionError);

        GovernanceOutboxEntry entry = await RequireEntryAsync(outboxEntryId, cancellationToken).ConfigureAwait(false);
        GovernanceOutboxEntry updatedEntry = entry.MarkFailed(governanceEmissionError, nextRetryUtc);

        return await SaveAsync(updatedEntry, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask<GovernanceOutboxEntry> MarkDeadLetteredAsync(
        string outboxEntryId,
        GovernanceEmissionError governanceEmissionError,
        string? deadLetterReason = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outboxEntryId);
        ArgumentNullException.ThrowIfNull(governanceEmissionError);

        GovernanceOutboxEntry entry = await RequireEntryAsync(outboxEntryId, cancellationToken).ConfigureAwait(false);
        GovernanceOutboxEntry updatedEntry = entry.MarkDeadLettered(governanceEmissionError, deadLetterReason);

        return await SaveAsync(updatedEntry, cancellationToken).ConfigureAwait(false);
    }

    private async ValueTask<GovernanceOutboxEntry> RequireEntryAsync(
        string outboxEntryId,
        CancellationToken cancellationToken)
    {
        GovernanceOutboxEntry? entry = await FindByOutboxEntryIdAsync(outboxEntryId, cancellationToken).ConfigureAwait(false);

        return entry ?? throw new InvalidOperationException($"Outbox entry '{outboxEntryId.Trim()}' was not found.");
    }

    private IQueryable<AsiBackboneGovernanceOutboxEntryEntity> OutboxEntries()
    {
        return dbContext.Set<AsiBackboneGovernanceOutboxEntryEntity>().AsNoTracking();
    }

    private static AsiBackboneGovernanceOutboxEntryEntity ToEntity(GovernanceOutboxEntry entry)
    {
        GovernanceEmissionEnvelope envelope = entry.Envelope;
        GovernanceEmissionPayload? payload = envelope.Payload;
        GovernanceEmissionError? lastError = entry.LastError;

        return new AsiBackboneGovernanceOutboxEntryEntity
        {
            OutboxEntryId = entry.OutboxEntryId,
            Status = entry.Status,
            CreatedUtc = entry.CreatedUtc,
            UpdatedUtc = entry.UpdatedUtc,
            DeliveredUtc = entry.Status is GovernanceEmissionStatus.Delivered ? entry.UpdatedUtc : null,
            RetryCount = entry.RetryCount,
            MaxRetryCount = entry.MaxRetryCount,
            NextRetryUtc = entry.NextRetryUtc,
            ProviderName = entry.ProviderName,
            ProviderRecordId = entry.ProviderRecordId,
            DeadLetterReason = entry.DeadLetterReason,
            LastErrorCode = lastError?.Code,
            LastErrorMessage = lastError?.Message,
            LastErrorIsRetryable = lastError?.IsRetryable,
            LastErrorProviderName = lastError?.ProviderName,
            LastErrorProviderErrorCode = lastError?.ProviderErrorCode,
            MetadataJson = JsonSerializer.Serialize(entry.Metadata, JsonOptions),
            EnvelopeId = envelope.EnvelopeId,
            EnvelopeSchemaVersion = envelope.SchemaVersion,
            EnvelopeEventType = envelope.EventType,
            EnvelopeEventId = envelope.EventId,
            EnvelopeOccurredUtc = envelope.OccurredUtc,
            EnvelopeCreatedUtc = envelope.CreatedUtc,
            EnvelopeCorrelationId = envelope.CorrelationId,
            EnvelopeAuditResidueId = envelope.AuditResidueId,
            EnvelopeLifecycleStage = envelope.LifecycleStage,
            EnvelopeLifecycleStageSequence = envelope.LifecycleStageSequence,
            EnvelopePolicyVersion = envelope.PolicyVersion,
            EnvelopePolicyHash = envelope.PolicyHash,
            EnvelopeTraceId = envelope.TraceId,
            EnvelopeSpanId = envelope.SpanId,
            EnvelopeParentSpanId = envelope.ParentSpanId,
            EnvelopeOperationName = envelope.OperationName,
            EnvelopeOutcome = envelope.Outcome,
            EnvelopeActorId = envelope.ActorId,
            EnvelopeEmitterStatus = envelope.EmitterStatus,
            EnvelopeEmitterProvider = envelope.EmitterProvider,
            EnvelopeOutboxSequence = envelope.OutboxSequence,
            EnvelopeGatewayExecutionId = envelope.GatewayExecutionId,
            EnvelopeDecisionStage = envelope.DecisionStage,
            EnvelopeMetadataJson = JsonSerializer.Serialize(envelope.Metadata, JsonOptions),
            EnvelopePayloadType = payload?.PayloadType,
            EnvelopePayloadSchemaVersion = payload?.SchemaVersion,
            EnvelopePayloadContentType = payload?.ContentType,
            EnvelopePayloadContentHash = payload?.ContentHash,
            EnvelopePayloadSizeBytes = payload?.SizeBytes,
            EnvelopePayloadMetadataJson = JsonSerializer.Serialize(payload?.Metadata ?? EmptyMetadata(), JsonOptions)
        };
    }

    private static GovernanceOutboxEntry[] ToEntries(IEnumerable<AsiBackboneGovernanceOutboxEntryEntity> entities)
    {
        return [.. entities.Select(ToEntry)];
    }

    private static GovernanceOutboxEntry ToEntry(AsiBackboneGovernanceOutboxEntryEntity entity)
    {
        GovernanceEmissionPayload? payload = string.IsNullOrWhiteSpace(entity.EnvelopePayloadType)
            ? null
            : GovernanceEmissionPayload.Create(
                entity.EnvelopePayloadType,
                entity.EnvelopePayloadSchemaVersion,
                entity.EnvelopePayloadContentType,
                entity.EnvelopePayloadContentHash,
                entity.EnvelopePayloadSizeBytes,
                DeserializeMetadata(entity.EnvelopePayloadMetadataJson));

        GovernanceEmissionEnvelope envelope = GovernanceEmissionEnvelope.Create(
            entity.EnvelopeEventType,
            entity.EnvelopeEventId,
            entity.EnvelopeOccurredUtc,
            entity.EnvelopeId,
            entity.EnvelopeCreatedUtc,
            entity.EnvelopeSchemaVersion,
            entity.EnvelopeCorrelationId,
            entity.EnvelopeAuditResidueId,
            entity.EnvelopeLifecycleStage,
            entity.EnvelopePolicyVersion,
            entity.EnvelopePolicyHash,
            entity.EnvelopeTraceId,
            entity.EnvelopeSpanId,
            entity.EnvelopeParentSpanId,
            entity.EnvelopeOperationName,
            entity.EnvelopeOutcome,
            entity.EnvelopeActorId,
            entity.EnvelopeEmitterStatus,
            entity.EnvelopeEmitterProvider,
            entity.EnvelopeOutboxSequence,
            entity.EnvelopeGatewayExecutionId,
            entity.EnvelopeDecisionStage,
            payload,
            DeserializeMetadata(entity.EnvelopeMetadataJson));

        GovernanceEmissionError? lastError = string.IsNullOrWhiteSpace(entity.LastErrorCode) || string.IsNullOrWhiteSpace(entity.LastErrorMessage)
            ? null
            : GovernanceEmissionError.Create(
                entity.LastErrorCode,
                entity.LastErrorMessage,
                entity.LastErrorIsRetryable ?? false,
                entity.LastErrorProviderName,
                entity.LastErrorProviderErrorCode);

        return GovernanceOutboxEntry.Restore(
            envelope,
            entity.Status,
            entity.OutboxEntryId,
            entity.CreatedUtc,
            entity.UpdatedUtc,
            entity.RetryCount,
            entity.MaxRetryCount,
            entity.NextRetryUtc,
            lastError,
            entity.ProviderName,
            entity.ProviderRecordId,
            entity.DeadLetterReason,
            DeserializeMetadata(entity.MetadataJson));
    }

    private static ReadOnlyDictionary<string, string> DeserializeMetadata(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return EmptyMetadata();
        }

        Dictionary<string, string>? metadata = JsonSerializer.Deserialize<Dictionary<string, string>>(json, JsonOptions);

        return metadata is null || metadata.Count == 0
            ? EmptyMetadata()
            : new ReadOnlyDictionary<string, string>(new Dictionary<string, string>(metadata, StringComparer.Ordinal));
    }

    private static ReadOnlyDictionary<string, string> EmptyMetadata()
    {
        return new ReadOnlyDictionary<string, string>(new Dictionary<string, string>(StringComparer.Ordinal));
    }

    private static int NormalizeMaxCount(int maxCount)
    {
        return maxCount <= 0
            ? throw new ArgumentOutOfRangeException(nameof(maxCount), maxCount, "Maximum count must be greater than zero.")
            : maxCount;
    }
}
