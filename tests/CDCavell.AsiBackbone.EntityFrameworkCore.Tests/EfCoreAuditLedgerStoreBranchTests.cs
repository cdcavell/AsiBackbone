using CDCavell.AsiBackbone.Core.Actors;
using CDCavell.AsiBackbone.Core.Audit;
using CDCavell.AsiBackbone.Core.Results;
using CDCavell.AsiBackbone.EntityFrameworkCore.Audit;
using CDCavell.AsiBackbone.EntityFrameworkCore.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CDCavell.AsiBackbone.EntityFrameworkCore.Tests;

public sealed class EfCoreAuditLedgerStoreBranchTests
{
    [Fact]
    public async Task FindByRecordIdAsyncReturnsNullWhenRecordDoesNotExist()
    {
        await using HostOwnedAuditDbContext context = CreateInMemoryContext();
        var store = new EfCoreAuditLedgerStore(context);

        AuditLedgerRecord? found = await store.FindByRecordIdAsync(
            "missing-record",
            TestContext.Current.CancellationToken);

        Assert.Null(found);
    }

    [Fact]
    public async Task FindByRecordedUtcRangeAsyncRejectsInvertedRange()
    {
        await using HostOwnedAuditDbContext context = CreateInMemoryContext();
        var store = new EfCoreAuditLedgerStore(context);

        ArgumentException exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            store.FindByRecordedUtcRangeAsync(
                new DateTimeOffset(2026, 6, 2, 0, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero),
                TestContext.Current.CancellationToken).AsTask());

        Assert.Contains("recorded UTC range", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AppendAsyncReturnsFailureWhenProviderRejectsDuplicateRecordId()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync(TestContext.Current.CancellationToken);

        DbContextOptions<HostOwnedAuditDbContext> options = new DbContextOptionsBuilder<HostOwnedAuditDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var context = new HostOwnedAuditDbContext(options);
        _ = await context.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        var store = new EfCoreAuditLedgerStore(context);
        AuditLedgerRecord firstRecord = CreateRecord("duplicate-record", "event-1");
        AuditLedgerRecord secondRecord = CreateRecord("duplicate-record", "event-2");

        OperationResult<AuditLedgerRecord> firstResult = await store.AppendAsync(
            firstRecord,
            TestContext.Current.CancellationToken);
        OperationResult<AuditLedgerRecord> secondResult = await store.AppendAsync(
            secondRecord,
            TestContext.Current.CancellationToken);

        Assert.True(firstResult.Succeeded);
        Assert.False(secondResult.Succeeded);
        Assert.Contains("asi_backbone.audit_ledger.append_failed", secondResult.ReasonCodes);
    }

    [Fact]
    public async Task FindByRecordIdAsyncHandlesEmptyJsonPayloadsAsEmptyCollections()
    {
        await using HostOwnedAuditDbContext context = CreateInMemoryContext();
        var store = new EfCoreAuditLedgerStore(context);

        _ = context.AuditLedgerRecords.Add(new AsiBackboneAuditLedgerRecordEntity
        {
            RecordId = "empty-json-record",
            EventId = "empty-json-event",
            OccurredUtc = new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero),
            RecordedUtc = new DateTimeOffset(2026, 6, 1, 1, 0, 0, TimeSpan.Zero),
            ActorId = "empty-json-actor",
            ActorType = AsiBackboneActorType.Service,
            OperationName = "empty-json.operation",
            Outcome = "Allowed",
            ReasonCodesJson = " ",
            MetadataJson = " ",
        });
        _ = await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        AuditLedgerRecord? found = await store.FindByRecordIdAsync(
            "empty-json-record",
            TestContext.Current.CancellationToken);

        Assert.NotNull(found);
        Assert.Empty(found.ReasonCodes);
        Assert.Empty(found.Metadata);
    }

    private static HostOwnedAuditDbContext CreateInMemoryContext()
    {
        DbContextOptions<HostOwnedAuditDbContext> options = new DbContextOptionsBuilder<HostOwnedAuditDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new HostOwnedAuditDbContext(options);
    }

    private static AuditLedgerRecord CreateRecord(string recordId, string eventId)
    {
        var actor = AsiBackboneActorContext.Human("actor-branch", "Branch Actor");
        var residue = AuditResidue.Create(
            actor,
            "branch.operation",
            "Allowed",
            ["branch.reason"],
            eventId: eventId,
            occurredUtc: new DateTimeOffset(2026, 6, 1, 9, 0, 0, TimeSpan.Zero),
            correlationId: "branch-correlation",
            traceId: "branch-trace",
            policyVersion: "branch-version",
            policyHash: "branch-hash",
            metadata: new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["branch"] = "true",
            });

        return AuditLedgerRecord.FromResidue(
            residue,
            recordId: recordId,
            recordedUtc: new DateTimeOffset(2026, 6, 1, 10, 0, 0, TimeSpan.Zero));
    }

    private sealed class HostOwnedAuditDbContext(DbContextOptions<HostOwnedAuditDbContext> options)
        : DbContext(options)
    {
        public DbSet<AsiBackboneAuditLedgerRecordEntity> AuditLedgerRecords =>
            Set<AsiBackboneAuditLedgerRecordEntity>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            _ = modelBuilder.ApplyAsiBackboneConfigurations();
        }
    }
}
