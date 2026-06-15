using System.Collections.ObjectModel;
using System.Text.Json;
using CDCavell.AsiBackbone.Core.Audit;
using CDCavell.AsiBackbone.EntityFrameworkCore.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CDCavell.AsiBackbone.EntityFrameworkCore.Audit;

/// <summary>
/// Entity Framework Core-backed audit residue lifecycle store that persists records through a host-owned <see cref="DbContext" />.
/// </summary>
/// <remarks>
/// This store appends provider-neutral lifecycle events and intentionally relies on the host application to expose the ASI Backbone entities from its own <see cref="DbContext" /> and migrations.
/// </remarks>
public sealed class EfCoreAuditResidueLifecycleStore : IAsiBackboneAuditResidueLifecycleStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly DbContext dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="EfCoreAuditResidueLifecycleStore" /> class.
    /// </summary>
    /// <param name="dbContext">The host-owned database context.</param>
    public EfCoreAuditResidueLifecycleStore(DbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);

        this.dbContext = dbContext;
    }

    /// <inheritdoc />
    public async ValueTask<AuditResidueLifecycleEvent> AppendAsync(
        AuditResidueLifecycleEvent lifecycleEvent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(lifecycleEvent);
        cancellationToken.ThrowIfCancellationRequested();

        _ = await dbContext
            .Set<AsiBackboneAuditResidueLifecycleEventEntity>()
            .AddAsync(ToEntity(lifecycleEvent), cancellationToken)
            .ConfigureAwait(false);

        _ = await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return lifecycleEvent;
    }

    /// <inheritdoc />
    public async ValueTask<AuditResidueLifecycleEvent?> FindByEventIdAsync(
        string eventId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventId);

        string normalizedEventId = eventId.Trim();

        AsiBackboneAuditResidueLifecycleEventEntity? entity = await LifecycleEvents()
            .Where(lifecycleEvent => lifecycleEvent.EventId == normalizedEventId)
            .SingleOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        return entity is null ? null : ToLifecycleEvent(entity);
    }

    /// <inheritdoc />
    public async ValueTask<IReadOnlyList<AuditResidueLifecycleEvent>> FindByCorrelationIdAsync(
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);

        string normalizedCorrelationId = correlationId.Trim();

        List<AsiBackboneAuditResidueLifecycleEventEntity> entities = await LifecycleEvents()
            .Where(lifecycleEvent => lifecycleEvent.CorrelationId == normalizedCorrelationId)
            .OrderBy(lifecycleEvent => lifecycleEvent.OccurredUtc)
            .ThenBy(lifecycleEvent => lifecycleEvent.EventId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return ToLifecycleEvents(entities);
    }

    /// <inheritdoc />
    public async ValueTask<IReadOnlyList<AuditResidueLifecycleEvent>> FindByAuditResidueIdAsync(
        string auditResidueId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(auditResidueId);

        string normalizedAuditResidueId = auditResidueId.Trim();

        List<AsiBackboneAuditResidueLifecycleEventEntity> entities = await LifecycleEvents()
            .Where(lifecycleEvent => lifecycleEvent.AuditResidueId == normalizedAuditResidueId)
            .OrderBy(lifecycleEvent => lifecycleEvent.OccurredUtc)
            .ThenBy(lifecycleEvent => lifecycleEvent.EventId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return ToLifecycleEvents(entities);
    }

    private IQueryable<AsiBackboneAuditResidueLifecycleEventEntity> LifecycleEvents()
    {
        return dbContext.Set<AsiBackboneAuditResidueLifecycleEventEntity>().AsNoTracking();
    }

    private static AsiBackboneAuditResidueLifecycleEventEntity ToEntity(AuditResidueLifecycleEvent lifecycleEvent)
    {
        return new AsiBackboneAuditResidueLifecycleEventEntity
        {
            EventId = lifecycleEvent.EventId,
            Stage = lifecycleEvent.Stage,
            StageSequence = lifecycleEvent.StageSequence,
            OccurredUtc = lifecycleEvent.OccurredUtc,
            CorrelationId = lifecycleEvent.CorrelationId,
            AuditResidueId = lifecycleEvent.AuditResidueId,
            TraceId = lifecycleEvent.TraceId,
            OperationName = lifecycleEvent.OperationName,
            Outcome = lifecycleEvent.Outcome,
            MetadataJson = JsonSerializer.Serialize(lifecycleEvent.Metadata, JsonOptions)
        };
    }

    private static AuditResidueLifecycleEvent[] ToLifecycleEvents(IEnumerable<AsiBackboneAuditResidueLifecycleEventEntity> entities)
    {
        return [.. entities.Select(ToLifecycleEvent)];
    }

    private static AuditResidueLifecycleEvent ToLifecycleEvent(AsiBackboneAuditResidueLifecycleEventEntity entity)
    {
        return AuditResidueLifecycleEvent.Create(
            entity.Stage,
            entity.CorrelationId,
            entity.AuditResidueId,
            entity.EventId,
            entity.OccurredUtc,
            entity.TraceId,
            entity.OperationName,
            entity.Outcome,
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
}
