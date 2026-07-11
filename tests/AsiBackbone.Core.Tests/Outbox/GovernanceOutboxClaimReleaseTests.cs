using AsiBackbone.Core.Emissions;
using AsiBackbone.Core.Outbox;
using AsiBackbone.Storage.InMemory.Outbox;
using Xunit;

namespace AsiBackbone.Core.Tests.Outbox;

/// <summary>
/// Focused coverage for in-memory outbox claim release and stale-claim behavior.
/// </summary>
public sealed class GovernanceOutboxClaimReleaseTests
{
    /// <summary>
    /// Verifies a matching claim is released and that repeating the release is idempotent.
    /// </summary>
    [Fact]
    public async Task ReleaseClaimClearsLeaseAndRepeatedReleaseIsIdempotent()
    {
        var store = new InMemoryGovernanceOutboxStore();
        GovernanceOutboxEntry entry = await store.EnqueueAsync(
            CreateEnvelope("release-success"),
            TestContext.Current.CancellationToken);
        DateTimeOffset claimedUtc = new(2026, 7, 11, 1, 0, 0, TimeSpan.Zero);
        GovernanceOutboxClaim claim = Assert.Single(await store.ClaimPendingAsync(
            GovernanceOutboxClaimRequest.Create("worker-a", claimedUtc, TimeSpan.FromMinutes(5)),
            TestContext.Current.CancellationToken));

        GovernanceOutboxEntry? released = await store.ReleaseClaimAsync(
            claim,
            "test release",
            TestContext.Current.CancellationToken);
        GovernanceOutboxEntry? repeated = await store.ReleaseClaimAsync(
            claim,
            "repeat release",
            TestContext.Current.CancellationToken);

        Assert.NotNull(released);
        Assert.False(released.HasClaim);
        Assert.Null(released.ClaimOwner);
        Assert.Null(released.ClaimToken);
        Assert.Null(released.ClaimedUtc);
        Assert.Null(released.ClaimExpiresUtc);
        Assert.Equal(1, released.ClaimAttemptCount);
        Assert.Same(released, repeated);
        Assert.Equal(entry.OutboxEntryId, repeated.OutboxEntryId);
    }

    /// <summary>
    /// Verifies an expired lease can still be explicitly released by its matching claim token.
    /// </summary>
    [Fact]
    public async Task ReleaseClaimAcceptsExpiredMatchingLease()
    {
        var store = new InMemoryGovernanceOutboxStore();
        _ = await store.EnqueueAsync(CreateEnvelope("release-expired"), TestContext.Current.CancellationToken);
        DateTimeOffset claimedUtc = new(2026, 7, 11, 2, 0, 0, TimeSpan.Zero);
        GovernanceOutboxClaim claim = Assert.Single(await store.ClaimPendingAsync(
            GovernanceOutboxClaimRequest.Create("worker-expired", claimedUtc, TimeSpan.FromSeconds(1)),
            TestContext.Current.CancellationToken));

        Assert.True(claim.IsExpired(claimedUtc.AddSeconds(1)));
        GovernanceOutboxEntry? released = await store.ReleaseClaimAsync(
            claim,
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(released);
        Assert.False(released.HasClaim);
        Assert.Equal(1, released.ClaimAttemptCount);
    }

    /// <summary>
    /// Verifies a stale claim cannot release a lease that has since been reacquired.
    /// </summary>
    [Fact]
    public async Task ReleaseClaimPreservesReacquiredLeaseWhenClaimIsStale()
    {
        var store = new InMemoryGovernanceOutboxStore();
        _ = await store.EnqueueAsync(CreateEnvelope("release-stale"), TestContext.Current.CancellationToken);
        DateTimeOffset firstClaimUtc = new(2026, 7, 11, 3, 0, 0, TimeSpan.Zero);
        GovernanceOutboxClaim staleClaim = Assert.Single(await store.ClaimPendingAsync(
            GovernanceOutboxClaimRequest.Create("worker-a", firstClaimUtc, TimeSpan.FromSeconds(1)),
            TestContext.Current.CancellationToken));
        GovernanceOutboxClaim currentClaim = Assert.Single(await store.ClaimPendingAsync(
            GovernanceOutboxClaimRequest.Create("worker-b", firstClaimUtc.AddSeconds(1), TimeSpan.FromMinutes(5)),
            TestContext.Current.CancellationToken));

        GovernanceOutboxEntry? result = await store.ReleaseClaimAsync(
            staleClaim,
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.True(result.HasClaim);
        Assert.Equal("worker-b", result.ClaimOwner);
        Assert.Equal(currentClaim.ClaimToken, result.ClaimToken);
        Assert.Equal(2, result.ClaimAttemptCount);
    }

    /// <summary>
    /// Verifies owner and token mismatches leave the active lease unchanged.
    /// </summary>
    [Fact]
    public async Task ReleaseClaimPreservesLeaseForMismatchedOwnerOrToken()
    {
        var store = new InMemoryGovernanceOutboxStore();
        _ = await store.EnqueueAsync(CreateEnvelope("release-mismatch"), TestContext.Current.CancellationToken);
        DateTimeOffset claimedUtc = new(2026, 7, 11, 4, 0, 0, TimeSpan.Zero);
        GovernanceOutboxClaim activeClaim = Assert.Single(await store.ClaimPendingAsync(
            GovernanceOutboxClaimRequest.Create("worker-a", claimedUtc, TimeSpan.FromMinutes(5)),
            TestContext.Current.CancellationToken));
        GovernanceOutboxClaim wrongOwner = GovernanceOutboxClaim.Create(
            activeClaim.Entry,
            "worker-b",
            activeClaim.ClaimToken,
            activeClaim.ClaimedUtc,
            activeClaim.ClaimExpiresUtc);
        GovernanceOutboxClaim wrongToken = GovernanceOutboxClaim.Create(
            activeClaim.Entry,
            activeClaim.WorkerId,
            "different-token",
            activeClaim.ClaimedUtc,
            activeClaim.ClaimExpiresUtc);

        GovernanceOutboxEntry? ownerResult = await store.ReleaseClaimAsync(
            wrongOwner,
            cancellationToken: TestContext.Current.CancellationToken);
        GovernanceOutboxEntry? tokenResult = await store.ReleaseClaimAsync(
            wrongToken,
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(ownerResult);
        Assert.Same(ownerResult, tokenResult);
        Assert.True(tokenResult.HasClaim);
        Assert.Equal(activeClaim.WorkerId, tokenResult.ClaimOwner);
        Assert.Equal(activeClaim.ClaimToken, tokenResult.ClaimToken);
    }

    /// <summary>
    /// Verifies missing and terminal entries use the documented no-op release semantics.
    /// </summary>
    [Fact]
    public async Task ReleaseClaimReturnsNullForMissingEntryAndPreservesTerminalEntry()
    {
        var store = new InMemoryGovernanceOutboxStore();
        DateTimeOffset claimedUtc = new(2026, 7, 11, 5, 0, 0, TimeSpan.Zero);
        GovernanceOutboxEntry missingEntry = GovernanceOutboxEntry.Create(
            CreateEnvelope("release-missing"),
            "missing-entry",
            claimedUtc).MarkClaimed("worker-missing", "missing-token", claimedUtc, TimeSpan.FromMinutes(5));
        GovernanceOutboxClaim missingClaim = GovernanceOutboxClaim.Create(
            missingEntry,
            "worker-missing",
            "missing-token",
            claimedUtc,
            claimedUtc.AddMinutes(5));

        Assert.Null(await store.ReleaseClaimAsync(
            missingClaim,
            cancellationToken: TestContext.Current.CancellationToken));

        _ = await store.EnqueueAsync(CreateEnvelope("release-terminal"), TestContext.Current.CancellationToken);
        GovernanceOutboxClaim terminalClaim = Assert.Single(await store.ClaimPendingAsync(
            GovernanceOutboxClaimRequest.Create("worker-terminal", claimedUtc, TimeSpan.FromMinutes(5)),
            TestContext.Current.CancellationToken));
        GovernanceOutboxEntry terminal = await store.MarkClaimDeliveredAsync(
            terminalClaim,
            GovernanceEmissionResult.Delivered("test-provider", "record-1"),
            TestContext.Current.CancellationToken);

        GovernanceOutboxEntry? result = await store.ReleaseClaimAsync(
            terminalClaim,
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Same(terminal, result);
        Assert.True(result.IsDelivered);
        Assert.False(result.HasClaim);
    }

    private static GovernanceEmissionEnvelope CreateEnvelope(string eventId)
    {
        return GovernanceEmissionEnvelope.Create(
            GovernanceEmissionEventType.Outbox,
            eventId,
            new DateTimeOffset(2026, 7, 11, 0, 0, 0, TimeSpan.Zero),
            envelopeId: $"envelope-{eventId}",
            correlationId: "claim-release-coverage",
            emitterStatus: GovernanceEmissionStatus.Pending.ToString(),
            emitterProvider: "in-memory-outbox");
    }
}
