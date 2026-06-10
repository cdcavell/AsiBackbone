using CDCavell.AsiBackbone.Core.Actors;
using CDCavell.AsiBackbone.Core.Audit;
using CDCavell.AsiBackbone.Core.Results;
using CDCavell.AsiBackbone.EntityFrameworkCore.Audit;
using CDCavell.AsiBackbone.EntityFrameworkCore.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CDCavell.AsiBackbone.EntityFrameworkCore.Tests;

/// <summary>
/// Integration tests for <see cref="EfCoreAuditLedgerStore" /> using a host-owned EF Core context.
/// </summary>
public sealed class EfCoreAuditLedgerStoreTests
{
    /// <summary>
    /// Verifies that the EF Core audit ledger store appends and rehydrates the full audit ledger record shape.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Fact]
    public async Task AppendAsyncPersistsRecordInHostOwnedDbContext()
    {
        await using HostOwnedAuditDbContext context = CreateContext();
        var store = new EfCoreAuditLedgerStore(context);
        AuditLedgerRecord record = CreateRecord(
            "record-123",
            "correlation-123",
            "trace-123",
            "actor-123",
            new DateTimeOffset(2026, 6, 1, 10, 0, 0, TimeSpan.Zero),
            ["allowed", "policy.current"],
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["tenant"] = "sample",
                ["source"] = "test"
            });

        OperationResult<AuditLedgerRecord> result = await store.AppendAsync(record, TestContext.Current.CancellationToken);
        AuditLedgerRecord? found = await store.FindByRecordIdAsync("record-123", TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        Assert.NotNull(found);
        Assert.Equal(record.RecordId, found.RecordId);
        Assert.Equal(record.EventId, found.EventId);
        Assert.Equal(record.OccurredUtc, found.OccurredUtc);
        Assert.Equal(record.RecordedUtc, found.RecordedUtc);
        Assert.Equal(record.ActorId, found.ActorId);
        Assert.Equal(record.ActorType, found.ActorType);
        Assert.Equal(record.ActorDisplayName, found.ActorDisplayName);
        Assert.Equal(record.OperationName, found.OperationName);
        Assert.Equal(record.Outcome, found.Outcome);
        Assert.Equal(record.ReasonCodes, found.ReasonCodes);
        Assert.Equal(record.CorrelationId, found.CorrelationId);
        Assert.Equal(record.TraceId, found.TraceId);
        Assert.Equal(record.PolicyVersion, found.PolicyVersion);
        Assert.Equal(record.PolicyHash, found.PolicyHash);
        Assert.Equal(record.HandshakeId, found.HandshakeId);
        Assert.Equal(record.AcknowledgmentId, found.AcknowledgmentId);
        Assert.Equal(record.CapabilityTokenId, found.CapabilityTokenId);
        Assert.Equal(record.PreviousRecordHash, found.PreviousRecordHash);
        Assert.Equal(record.RecordHash, found.RecordHash);
        Assert.Equal(record.SignatureKeyId, found.SignatureKeyId);
        Assert.Equal(record.SignatureAlgorithm, found.SignatureAlgorithm);
        Assert.Equal(record.SignatureValue, found.SignatureValue);
        Assert.Equal(record.Metadata, found.Metadata);

        Assert.Equal(1, await context.AuditLedgerRecords.CountAsync(TestContext.Current.CancellationToken));
        Assert.Equal(2, await context.AuditLedgerReasonCodes.CountAsync(TestContext.Current.CancellationToken));
        Assert.Equal(2, await context.AuditLedgerMetadata.CountAsync(TestContext.Current.CancellationToken));
    }

    /// <summary>
    /// Verifies that audit ledger records can be queried by correlation ID, trace ID, actor ID, and recorded UTC range.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Fact]
    public async Task QueryMethodsReturnExpectedRecords()
    {
        await using HostOwnedAuditDbContext context = CreateContext();
        var store = new EfCoreAuditLedgerStore(context);
        AuditLedgerRecord firstRecord = CreateRecord(
            "record-1",
            "correlation-shared",
            "trace-shared",
            "actor-shared",
            new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero));
        AuditLedgerRecord secondRecord = CreateRecord(
            "record-2",
            "correlation-shared",
            "trace-shared",
            "actor-shared",
            new DateTimeOffset(2026, 6, 2, 0, 0, 0, TimeSpan.Zero));
        AuditLedgerRecord thirdRecord = CreateRecord(
            "record-3",
            "correlation-other",
            "trace-other",
            "actor-other",
            new DateTimeOffset(2026, 6, 3, 0, 0, 0, TimeSpan.Zero));

        _ = await store.AppendAsync(firstRecord, TestContext.Current.CancellationToken);
        _ = await store.AppendAsync(secondRecord, TestContext.Current.CancellationToken);
        _ = await store.AppendAsync(thirdRecord, TestContext.Current.CancellationToken);

        IReadOnlyList<AuditLedgerRecord> correlationMatches = await store.FindByCorrelationIdAsync(
            "correlation-shared",
            TestContext.Current.CancellationToken);
        IReadOnlyList<AuditLedgerRecord> traceMatches = await store.FindByTraceIdAsync(
            "trace-shared",
            TestContext.Current.CancellationToken);
        IReadOnlyList<AuditLedgerRecord> actorMatches = await store.FindByActorIdAsync(
            "actor-shared",
            TestContext.Current.CancellationToken);
        IReadOnlyList<AuditLedgerRecord> dateRangeMatches = await store.FindByRecordedUtcRangeAsync(
            new DateTimeOffset(2026, 6, 1, 12, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 6, 2, 12, 0, 0, TimeSpan.Zero),
            TestContext.Current.CancellationToken);

        string[] expectedSharedRecordIds = ["record-1", "record-2"];
        string[] expectedDateRangeRecordIds = ["record-2"];

        Assert.Equal(expectedSharedRecordIds, correlationMatches.Select(record => record.RecordId).ToArray());
        Assert.Equal(expectedSharedRecordIds, traceMatches.Select(record => record.RecordId).ToArray());
        Assert.Equal(expectedSharedRecordIds, actorMatches.Select(record => record.RecordId).ToArray());
        Assert.Equal(expectedDateRangeRecordIds, dateRangeMatches.Select(record => record.RecordId).ToArray());
    }

    private static HostOwnedAuditDbContext CreateContext()
    {
        DbContextOptions<HostOwnedAuditDbContext> options = new DbContextOptionsBuilder<HostOwnedAuditDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new HostOwnedAuditDbContext(options);
    }

    private static AuditLedgerRecord CreateRecord(
        string recordId,
        string correlationId,
        string traceId,
        string actorId,
        DateTimeOffset? recordedUtc = null,
        IEnumerable<string>? reasonCodes = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        var actor = AsiBackboneActorContext.Human(actorId, "Test Actor");
        var residue = AuditResidue.Create(
            actor,
            "system.sync",
            "Allowed",
            reasonCodes,
            eventId: $"event-{recordId}",
            occurredUtc: new DateTimeOffset(2026, 6, 1, 9, 0, 0, TimeSpan.Zero),
            correlationId: correlationId,
            traceId: traceId,
            policyVersion: "2026.06",
            policyHash: "policy-hash",
            metadata: metadata);

        return AuditLedgerRecord.FromResidue(
            residue,
            recordId: recordId,
            recordedUtc: recordedUtc,
            handshakeId: "handshake-123",
            acknowledgmentId: "ack-123",
            capabilityTokenId: "token-123",
            previousRecordHash: "previous-hash",
            recordHash: "record-hash",
            signatureKeyId: "key-123",
            signatureAlgorithm: "HS256",
            signatureValue: "signature-value");
    }

    private sealed class HostOwnedAuditDbContext(DbContextOptions<HostOwnedAuditDbContext> options)
        : DbContext(options)
    {
        public DbSet<AsiBackboneAuditLedgerRecordEntity> AuditLedgerRecords =>
            Set<AsiBackboneAuditLedgerRecordEntity>();

        public DbSet<AsiBackboneAuditLedgerReasonCodeEntity> AuditLedgerReasonCodes =>
            Set<AsiBackboneAuditLedgerReasonCodeEntity>();

        public DbSet<AsiBackboneAuditLedgerMetadataEntity> AuditLedgerMetadata =>
            Set<AsiBackboneAuditLedgerMetadataEntity>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            _ = modelBuilder.ApplyAsiBackboneConfigurations();
        }
    }
}
