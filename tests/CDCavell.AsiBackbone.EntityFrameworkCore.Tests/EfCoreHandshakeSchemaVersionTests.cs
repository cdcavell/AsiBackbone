using CDCavell.AsiBackbone.Core.Actors;
using CDCavell.AsiBackbone.Core.Handshakes;
using CDCavell.AsiBackbone.Core.Serialization;
using CDCavell.AsiBackbone.EntityFrameworkCore.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CDCavell.AsiBackbone.EntityFrameworkCore.Tests;

/// <summary>
/// Integration tests for handshake schema version persistence.
/// </summary>
public sealed class EfCoreHandshakeSchemaVersionTests
{
    /// <summary>
    /// Verifies that EF Core handshake request persistence round-trips the schema version.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Fact]
    public async Task HandshakeRequestRoundTripsSchemaVersion()
    {
        await using HostOwnedHandshakeDbContext context = CreateContext();

        context.HandshakeRequests.Add(new AsiBackboneHandshakeRequestEntity
        {
            HandshakeId = "handshake-123",
            SchemaVersion = "1.1-test",
            ActorId = "actor-123",
            ActorType = AsiBackboneActorType.Human,
            ActorDisplayName = "Test Actor",
            OperationName = "document.approve",
            ReasonCode = "ack.required",
            Message = "Acknowledgment is required.",
            RequiredAcknowledgmentCode = "ACK-001",
            RequiredAcknowledgmentText = "I understand this action is consequential.",
            RiskLevel = LiabilityHandshakeRiskLevel.High,
            RiskCategory = "administrative",
            CorrelationId = "correlation-123",
            TraceId = "trace-456",
            PolicyVersion = "v1",
            PolicyHash = "hash-abc"
        });

        _ = await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        AsiBackboneHandshakeRequestEntity found = await context.HandshakeRequests
            .AsNoTracking()
            .SingleAsync(request => request.HandshakeId == "handshake-123", TestContext.Current.CancellationToken);

        Assert.Equal("1.1-test", found.SchemaVersion);
    }

    /// <summary>
    /// Verifies that EF Core handshake acknowledgment persistence round-trips the schema version.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Fact]
    public async Task HandshakeAcknowledgmentRoundTripsSchemaVersion()
    {
        await using HostOwnedHandshakeDbContext context = CreateContext();

        context.HandshakeAcknowledgments.Add(new AsiBackboneHandshakeAcknowledgmentEntity
        {
            AcknowledgmentId = "ack-123",
            SchemaVersion = "1.1-test",
            HandshakeId = "handshake-123",
            ActorId = "actor-123",
            ActorType = AsiBackboneActorType.Human,
            ActorDisplayName = "Test Actor",
            AcknowledgmentCode = "ACK-001",
            Acknowledged = true,
            OccurredUtc = new DateTimeOffset(2026, 6, 1, 9, 0, 0, TimeSpan.Zero),
            CorrelationId = "correlation-123",
            TraceId = "trace-456"
        });

        _ = await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        AsiBackboneHandshakeAcknowledgmentEntity found = await context.HandshakeAcknowledgments
            .AsNoTracking()
            .SingleAsync(acknowledgment => acknowledgment.AcknowledgmentId == "ack-123", TestContext.Current.CancellationToken);

        Assert.Equal("1.1-test", found.SchemaVersion);
    }

    /// <summary>
    /// Verifies that EF Core handshake entities default to the initial stable schema version.
    /// </summary>
    [Fact]
    public void HandshakeEntitiesDefaultToStableSchemaVersion()
    {
        Assert.Equal(
            AsiBackboneSchemaVersions.StableArtifactsV1,
            new AsiBackboneHandshakeRequestEntity().SchemaVersion);

        Assert.Equal(
            AsiBackboneSchemaVersions.StableArtifactsV1,
            new AsiBackboneHandshakeAcknowledgmentEntity().SchemaVersion);
    }

    private static HostOwnedHandshakeDbContext CreateContext()
    {
        DbContextOptions<HostOwnedHandshakeDbContext> options = new DbContextOptionsBuilder<HostOwnedHandshakeDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new HostOwnedHandshakeDbContext(options);
    }

    private sealed class HostOwnedHandshakeDbContext(DbContextOptions<HostOwnedHandshakeDbContext> options)
        : DbContext(options)
    {
        public DbSet<AsiBackboneHandshakeRequestEntity> HandshakeRequests =>
            Set<AsiBackboneHandshakeRequestEntity>();

        public DbSet<AsiBackboneHandshakeAcknowledgmentEntity> HandshakeAcknowledgments =>
            Set<AsiBackboneHandshakeAcknowledgmentEntity>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            _ = modelBuilder.ApplyAsiBackboneConfigurations();
        }
    }
}
