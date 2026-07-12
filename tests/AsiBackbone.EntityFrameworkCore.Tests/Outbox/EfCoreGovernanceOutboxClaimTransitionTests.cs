using AsiBackbone.Core.Emissions;
using AsiBackbone.Core.Outbox;
using AsiBackbone.EntityFrameworkCore.Outbox;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AsiBackbone.EntityFrameworkCore.Tests;

/// <summary>
/// Focused coverage for claim-owned EF Core outbox transitions, stale claims, terminal no-ops, and defensive guards.
/// </summary>
public sealed class EfCoreGovernanceOutboxClaimTransitionTests
{
    /// <summary>
    /// Verifies a matching claim can complete delivery, merge provider metadata, and clear lease state durably.
    /// </summary>
    [Fact]
    public async Task MarkClaimDeliveredPersistsProviderResultAndClearsClaimMetadata()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using SqliteConnection connection = await EfCoreGovernanceOutboxTestHost.OpenConnectionAsync(cancellationToken);
        DbContextOptions<GovernanceOutboxTestDbContext> options = EfCoreGovernanceOutboxTestHost.CreateOptions(connection);
        await EfCoreGovernanceOutboxTestHost.EnsureCreatedAsync(options, cancellationToken);

        await using GovernanceOutboxTestDbContext context = new(options);
        var store = new EfCoreGovernanceOutboxStore(context);
        GovernanceOutboxClaim claim = await SaveAndClaimAsync(
            context,
            store,
            "claim-delivered",
            new DateTimeOffset(2026, 7, 12, 12, 0, 0, TimeSpan.Zero),
            cancellationToken);
        GovernanceEmissionResult result = GovernanceEmissionResult.Delivered(
            "provider-delivered",
            "provider-record-delivered",
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["delivery.state"] = "accepted"
            });

        GovernanceOutboxEntry updated = await store.MarkClaimDeliveredAsync(claim, result, cancellationToken);
        context.ChangeTracker.Clear();
        GovernanceOutboxEntry? persisted = await store.FindByOutboxEntryIdAsync(claim.OutboxEntryId, cancellationToken);

        Assert.Equal(GovernanceEmissionStatus.Delivered, updated.Status);
        Assert.Equal("provider-delivered", updated.ProviderName);
        Assert.Equal("provider-record-delivered", updated.ProviderRecordId);
        Assert.Equal("accepted", updated.Metadata["delivery.state"]);
        Assert.Equal(1, updated.ClaimAttemptCount);
        AssertClaimCleared(updated);
        Assert.NotNull(persisted);
        Assert.Equal(updated.Status, persisted.Status);
        Assert.Equal(updated.ProviderName, persisted.ProviderName);
        Assert.Equal(updated.ProviderRecordId, persisted.ProviderRecordId);
        Assert.Equal("accepted", persisted.Metadata["delivery.state"]);
        AssertClaimCleared(persisted);
    }

    /// <summary>
    /// Verifies a matching claim can persist both ordinary and retryable failure states while clearing lease state.
    /// </summary>
    /// <param name="isRetryable">Whether the provider error permits retry.</param>
    /// <param name="expectedStatus">The expected durable status.</param>
    [Theory]
    [InlineData(false, GovernanceEmissionStatus.Failed)]
    [InlineData(true, GovernanceEmissionStatus.RetryableFailure)]
    public async Task MarkClaimFailedPersistsFailureKindAndClearsClaimMetadata(
        bool isRetryable,
        GovernanceEmissionStatus expectedStatus)
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using SqliteConnection connection = await EfCoreGovernanceOutboxTestHost.OpenConnectionAsync(cancellationToken);
        DbContextOptions<GovernanceOutboxTestDbContext> options = EfCoreGovernanceOutboxTestHost.CreateOptions(connection);
        await EfCoreGovernanceOutboxTestHost.EnsureCreatedAsync(options, cancellationToken);

        await using GovernanceOutboxTestDbContext context = new(options);
        var store = new EfCoreGovernanceOutboxStore(context);
        GovernanceOutboxClaim claim = await SaveAndClaimAsync(
            context,
            store,
            $"claim-failed-{isRetryable}",
            new DateTimeOffset(2026, 7, 12, 12, 10, 0, TimeSpan.Zero),
            cancellationToken);
        DateTimeOffset nextRetryUtc = new(2026, 7, 12, 12, 15, 0, TimeSpan.Zero);
        GovernanceEmissionError error = GovernanceEmissionError.Create(
            isRetryable ? "provider.retryable" : "provider.failed",
            isRetryable ? "Provider requested retry." : "Provider rejected the emission.",
            isRetryable,
            "provider-failed",
            isRetryable ? "429" : "400");

        GovernanceOutboxEntry updated = await store.MarkClaimFailedAsync(
            claim,
            error,
            nextRetryUtc,
            cancellationToken);
        context.ChangeTracker.Clear();
        GovernanceOutboxEntry? persisted = await store.FindByOutboxEntryIdAsync(claim.OutboxEntryId, cancellationToken);

        Assert.Equal(expectedStatus, updated.Status);
        Assert.Equal(1, updated.RetryCount);
        Assert.Equal(nextRetryUtc, updated.NextRetryUtc);
        Assert.Equal(error.Code, updated.LastError?.Code);
        Assert.Equal(error.Message, updated.LastError?.Message);
        Assert.Equal(error.ProviderErrorCode, updated.LastError?.ProviderErrorCode);
        Assert.Equal("provider-failed", updated.ProviderName);
        AssertClaimCleared(updated);
        Assert.NotNull(persisted);
        Assert.Equal(updated.Status, persisted.Status);
        Assert.Equal(updated.RetryCount, persisted.RetryCount);
        Assert.Equal(updated.NextRetryUtc, persisted.NextRetryUtc);
        Assert.Equal(updated.LastError?.Code, persisted.LastError?.Code);
        AssertClaimCleared(persisted);
    }

    /// <summary>
    /// Verifies a matching claim can dead-letter with either an explicit reason or the provider error message fallback.
    /// </summary>
    /// <param name="deadLetterReason">The optional explicit terminal reason.</param>
    [Theory]
    [InlineData(null)]
    [InlineData("operator-confirmed-terminal")]
    public async Task MarkClaimDeadLetteredPersistsReasonAndClearsClaimMetadata(string? deadLetterReason)
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using SqliteConnection connection = await EfCoreGovernanceOutboxTestHost.OpenConnectionAsync(cancellationToken);
        DbContextOptions<GovernanceOutboxTestDbContext> options = EfCoreGovernanceOutboxTestHost.CreateOptions(connection);
        await EfCoreGovernanceOutboxTestHost.EnsureCreatedAsync(options, cancellationToken);

        await using GovernanceOutboxTestDbContext context = new(options);
        var store = new EfCoreGovernanceOutboxStore(context);
        GovernanceOutboxClaim claim = await SaveAndClaimAsync(
            context,
            store,
            $"claim-dead-letter-{deadLetterReason ?? "fallback"}",
            new DateTimeOffset(2026, 7, 12, 12, 20, 0, TimeSpan.Zero),
            cancellationToken);
        GovernanceEmissionError error = GovernanceEmissionError.Create(
            "provider.terminal",
            "Provider reported a terminal failure.",
            isRetryable: false,
            providerName: "provider-terminal",
            providerErrorCode: "terminal-001");

        GovernanceOutboxEntry updated = await store.MarkClaimDeadLetteredAsync(
            claim,
            error,
            deadLetterReason,
            cancellationToken);
        context.ChangeTracker.Clear();
        GovernanceOutboxEntry? persisted = await store.FindByOutboxEntryIdAsync(claim.OutboxEntryId, cancellationToken);

        string expectedReason = deadLetterReason ?? error.Message;
        Assert.Equal(GovernanceEmissionStatus.DeadLettered, updated.Status);
        Assert.Equal(expectedReason, updated.DeadLetterReason);
        Assert.Equal(error.Code, updated.LastError?.Code);
        Assert.Equal("provider-terminal", updated.ProviderName);
        AssertClaimCleared(updated);
        Assert.NotNull(persisted);
        Assert.Equal(expectedReason, persisted.DeadLetterReason);
        Assert.Equal(error.ProviderErrorCode, persisted.LastError?.ProviderErrorCode);
        AssertClaimCleared(persisted);
    }

    /// <summary>
    /// Verifies <see cref="EfCoreGovernanceOutboxStore.SaveClaimAsync" /> persists a claim-owned deferred transition.
    /// </summary>
    [Fact]
    public async Task SaveClaimPersistsDeferredTransitionAndClearsClaimMetadata()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using SqliteConnection connection = await EfCoreGovernanceOutboxTestHost.OpenConnectionAsync(cancellationToken);
        DbContextOptions<GovernanceOutboxTestDbContext> options = EfCoreGovernanceOutboxTestHost.CreateOptions(connection);
        await EfCoreGovernanceOutboxTestHost.EnsureCreatedAsync(options, cancellationToken);

        await using GovernanceOutboxTestDbContext context = new(options);
        var store = new EfCoreGovernanceOutboxStore(context);
        GovernanceOutboxClaim claim = await SaveAndClaimAsync(
            context,
            store,
            "claim-save-deferred",
            new DateTimeOffset(2026, 7, 12, 12, 30, 0, TimeSpan.Zero),
            cancellationToken);
        DateTimeOffset nextRetryUtc = new(2026, 7, 12, 12, 45, 0, TimeSpan.Zero);
        GovernanceEmissionError error = GovernanceEmissionError.Create(
            "provider.deferred",
            "Provider deferred processing.",
            isRetryable: true,
            providerName: "provider-deferred");
        GovernanceOutboxEntry deferred = claim.Entry.MarkDeferred(error, nextRetryUtc);

        GovernanceOutboxEntry updated = await store.SaveClaimAsync(claim, deferred, cancellationToken);
        context.ChangeTracker.Clear();
        GovernanceOutboxEntry? persisted = await store.FindByOutboxEntryIdAsync(claim.OutboxEntryId, cancellationToken);

        Assert.Equal(GovernanceEmissionStatus.Deferred, updated.Status);
        Assert.Equal(nextRetryUtc, updated.NextRetryUtc);
        Assert.Equal(error.Code, updated.LastError?.Code);
        Assert.Equal(0, updated.RetryCount);
        AssertClaimCleared(updated);
        Assert.NotNull(persisted);
        Assert.Equal(GovernanceEmissionStatus.Deferred, persisted.Status);
        Assert.Equal(nextRetryUtc, persisted.NextRetryUtc);
        Assert.Equal(error.Code, persisted.LastError?.Code);
        AssertClaimCleared(persisted);
    }

    /// <summary>
    /// Verifies an expired claim that has been superseded cannot overwrite the current lease or provider state.
    /// </summary>
    [Fact]
    public async Task SupersededExpiredClaimDoesNotOverwriteCurrentClaim()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using SqliteConnection connection = await EfCoreGovernanceOutboxTestHost.OpenConnectionAsync(cancellationToken);
        DbContextOptions<GovernanceOutboxTestDbContext> options = EfCoreGovernanceOutboxTestHost.CreateOptions(connection);
        await EfCoreGovernanceOutboxTestHost.EnsureCreatedAsync(options, cancellationToken);

        await using GovernanceOutboxTestDbContext context = new(options);
        var store = new EfCoreGovernanceOutboxStore(context);
        DateTimeOffset firstClaimUtc = new(2026, 7, 12, 13, 0, 0, TimeSpan.Zero);
        GovernanceOutboxClaim firstClaim = await SaveAndClaimAsync(
            context,
            store,
            "claim-superseded",
            firstClaimUtc,
            cancellationToken,
            leaseDuration: TimeSpan.FromMinutes(1));
        context.ChangeTracker.Clear();

        GovernanceOutboxClaim currentClaim = Assert.Single(await store.ClaimPendingAsync(
            GovernanceOutboxClaimRequest.Create(
                "worker-current",
                firstClaimUtc.AddMinutes(2),
                TimeSpan.FromMinutes(5),
                maxCount: 1),
            cancellationToken));
        context.ChangeTracker.Clear();

        GovernanceOutboxEntry result = await store.MarkClaimDeliveredAsync(
            firstClaim,
            GovernanceEmissionResult.Delivered("stale-provider", "stale-record"),
            cancellationToken);
        GovernanceOutboxEntry? persisted = await store.FindByOutboxEntryIdAsync(firstClaim.OutboxEntryId, cancellationToken);

        Assert.Equal(GovernanceEmissionStatus.Pending, result.Status);
        Assert.Equal(currentClaim.WorkerId, result.ClaimOwner);
        Assert.Equal(currentClaim.ClaimToken, result.ClaimToken);
        Assert.Equal(2, result.ClaimAttemptCount);
        Assert.Null(result.ProviderName);
        Assert.Null(result.ProviderRecordId);
        Assert.NotNull(persisted);
        Assert.Equal(currentClaim.WorkerId, persisted.ClaimOwner);
        Assert.Equal(currentClaim.ClaimToken, persisted.ClaimToken);
        Assert.Equal(GovernanceEmissionStatus.Pending, persisted.Status);
    }

    /// <summary>
    /// Verifies mismatched claim owners and tokens cannot replace the current claimed row.
    /// </summary>
    /// <param name="mismatchOwner">Whether the claim owner is changed.</param>
    /// <param name="mismatchToken">Whether the claim token is changed.</param>
    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task MismatchedClaimIdentityDoesNotOverwriteCurrentClaim(bool mismatchOwner, bool mismatchToken)
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using SqliteConnection connection = await EfCoreGovernanceOutboxTestHost.OpenConnectionAsync(cancellationToken);
        DbContextOptions<GovernanceOutboxTestDbContext> options = EfCoreGovernanceOutboxTestHost.CreateOptions(connection);
        await EfCoreGovernanceOutboxTestHost.EnsureCreatedAsync(options, cancellationToken);

        await using GovernanceOutboxTestDbContext context = new(options);
        var store = new EfCoreGovernanceOutboxStore(context);
        GovernanceOutboxClaim currentClaim = await SaveAndClaimAsync(
            context,
            store,
            $"claim-mismatch-{mismatchOwner}-{mismatchToken}",
            new DateTimeOffset(2026, 7, 12, 13, 10, 0, TimeSpan.Zero),
            cancellationToken);
        GovernanceOutboxClaim mismatchedClaim = GovernanceOutboxClaim.Create(
            currentClaim.Entry,
            mismatchOwner ? "worker-other" : currentClaim.WorkerId,
            mismatchToken ? "claim-token-other" : currentClaim.ClaimToken,
            currentClaim.ClaimedUtc,
            currentClaim.ClaimExpiresUtc);
        GovernanceEmissionError error = GovernanceEmissionError.Create(
            "provider.should-not-persist",
            "This stale update must not persist.",
            providerName: "provider-stale");
        context.ChangeTracker.Clear();

        GovernanceOutboxEntry result = await store.MarkClaimFailedAsync(
            mismatchedClaim,
            error,
            cancellationToken: cancellationToken);
        GovernanceOutboxEntry? persisted = await store.FindByOutboxEntryIdAsync(currentClaim.OutboxEntryId, cancellationToken);

        Assert.Equal(GovernanceEmissionStatus.Pending, result.Status);
        Assert.Equal(currentClaim.WorkerId, result.ClaimOwner);
        Assert.Equal(currentClaim.ClaimToken, result.ClaimToken);
        Assert.Equal(0, result.RetryCount);
        Assert.Null(result.LastError);
        Assert.NotNull(persisted);
        Assert.Equal(currentClaim.WorkerId, persisted.ClaimOwner);
        Assert.Equal(currentClaim.ClaimToken, persisted.ClaimToken);
        Assert.Null(persisted.LastError);
    }

    /// <summary>
    /// Verifies delivered and dead-lettered rows remain terminal when an earlier claim attempts another transition.
    /// </summary>
    [Fact]
    public async Task ClaimUpdatesDoNotReplaceDeliveredOrDeadLetteredTerminalState()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using SqliteConnection connection = await EfCoreGovernanceOutboxTestHost.OpenConnectionAsync(cancellationToken);
        DbContextOptions<GovernanceOutboxTestDbContext> options = EfCoreGovernanceOutboxTestHost.CreateOptions(connection);
        await EfCoreGovernanceOutboxTestHost.EnsureCreatedAsync(options, cancellationToken);

        await using GovernanceOutboxTestDbContext context = new(options);
        var store = new EfCoreGovernanceOutboxStore(context);
        GovernanceOutboxClaim deliveredClaim = await SaveAndClaimAsync(
            context,
            store,
            "claim-terminal-delivered",
            new DateTimeOffset(2026, 7, 12, 13, 20, 0, TimeSpan.Zero),
            cancellationToken);
        GovernanceOutboxEntry delivered = await store.MarkClaimDeliveredAsync(
            deliveredClaim,
            GovernanceEmissionResult.Delivered("provider-terminal", "record-terminal"),
            cancellationToken);
        context.ChangeTracker.Clear();

        GovernanceOutboxEntry deliveredNoOp = await store.MarkClaimFailedAsync(
            deliveredClaim,
            GovernanceEmissionError.Create("provider.late", "Late failure.", providerName: "provider-late"),
            cancellationToken: cancellationToken);

        GovernanceOutboxClaim deadLetterClaim = await SaveAndClaimAsync(
            context,
            store,
            "claim-terminal-dead-letter",
            new DateTimeOffset(2026, 7, 12, 13, 30, 0, TimeSpan.Zero),
            cancellationToken);
        GovernanceEmissionError terminalError = GovernanceEmissionError.Create(
            "provider.dead",
            "Permanent failure.",
            providerName: "provider-dead");
        GovernanceOutboxEntry deadLettered = await store.MarkClaimDeadLetteredAsync(
            deadLetterClaim,
            terminalError,
            "durable-terminal-reason",
            cancellationToken);
        context.ChangeTracker.Clear();

        GovernanceOutboxEntry deadLetterNoOp = await store.MarkClaimDeliveredAsync(
            deadLetterClaim,
            GovernanceEmissionResult.Delivered("provider-late-success", "late-record"),
            cancellationToken);

        Assert.Equal(GovernanceEmissionStatus.Delivered, delivered.Status);
        Assert.Equal(GovernanceEmissionStatus.Delivered, deliveredNoOp.Status);
        Assert.Equal("provider-terminal", deliveredNoOp.ProviderName);
        Assert.Equal("record-terminal", deliveredNoOp.ProviderRecordId);
        Assert.Null(deliveredNoOp.LastError);
        Assert.Equal(GovernanceEmissionStatus.DeadLettered, deadLettered.Status);
        Assert.Equal(GovernanceEmissionStatus.DeadLettered, deadLetterNoOp.Status);
        Assert.Equal("durable-terminal-reason", deadLetterNoOp.DeadLetterReason);
        Assert.Equal(terminalError.Code, deadLetterNoOp.LastError?.Code);
        Assert.Null(deadLetterNoOp.ProviderRecordId);
    }

    /// <summary>
    /// Verifies public claimed-transition methods reject null dependencies and mismatched stable identities.
    /// </summary>
    [Fact]
    public async Task ClaimedTransitionGuardsRejectNullArgumentsAndIdentityMismatch()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using SqliteConnection connection = await EfCoreGovernanceOutboxTestHost.OpenConnectionAsync(cancellationToken);
        DbContextOptions<GovernanceOutboxTestDbContext> options = EfCoreGovernanceOutboxTestHost.CreateOptions(connection);
        await EfCoreGovernanceOutboxTestHost.EnsureCreatedAsync(options, cancellationToken);

        await using GovernanceOutboxTestDbContext context = new(options);
        var store = new EfCoreGovernanceOutboxStore(context);
        DateTimeOffset claimedUtc = new(2026, 7, 12, 14, 0, 0, TimeSpan.Zero);
        GovernanceOutboxEntry claimedEntry = GovernanceOutboxEntry.Create(
                EfCoreGovernanceOutboxTestHost.CreateEnvelope("guard-claim"),
                "guard-claim-entry",
                claimedUtc.AddMinutes(-1))
            .MarkClaimed("worker-guard", "token-guard", claimedUtc, TimeSpan.FromMinutes(5));
        GovernanceOutboxClaim claim = GovernanceOutboxClaim.Create(
            claimedEntry,
            "worker-guard",
            "token-guard",
            claimedUtc,
            claimedUtc.AddMinutes(5));
        GovernanceOutboxEntry differentEntry = GovernanceOutboxEntry.Create(
            EfCoreGovernanceOutboxTestHost.CreateEnvelope("guard-other"),
            "guard-other-entry",
            claimedUtc);
        GovernanceEmissionResult result = GovernanceEmissionResult.Delivered("provider-guard", "record-guard");
        GovernanceEmissionError error = GovernanceEmissionError.Create("provider.guard", "Guard failure.");

        _ = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await store.MarkClaimDeliveredAsync(null!, result, cancellationToken));
        _ = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await store.MarkClaimDeliveredAsync(claim, null!, cancellationToken));
        _ = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await store.MarkClaimFailedAsync(null!, error, cancellationToken: cancellationToken));
        _ = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await store.MarkClaimFailedAsync(claim, null!, cancellationToken: cancellationToken));
        _ = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await store.MarkClaimDeadLetteredAsync(null!, error, cancellationToken: cancellationToken));
        _ = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await store.MarkClaimDeadLetteredAsync(claim, null!, cancellationToken: cancellationToken));
        _ = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await store.SaveClaimAsync(null!, claimedEntry, cancellationToken));
        _ = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await store.SaveClaimAsync(claim, null!, cancellationToken));

        ArgumentException mismatchException = await Assert.ThrowsAsync<ArgumentException>(async () =>
            await store.SaveClaimAsync(claim, differentEntry, cancellationToken));
        Assert.Equal("entry", mismatchException.ParamName);
    }

    /// <summary>
    /// Verifies a claimed transition for a missing durable row reports the documented not-found behavior.
    /// </summary>
    [Fact]
    public async Task SaveClaimForMissingEntryThrowsDocumentedNotFoundException()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using SqliteConnection connection = await EfCoreGovernanceOutboxTestHost.OpenConnectionAsync(cancellationToken);
        DbContextOptions<GovernanceOutboxTestDbContext> options = EfCoreGovernanceOutboxTestHost.CreateOptions(connection);
        await EfCoreGovernanceOutboxTestHost.EnsureCreatedAsync(options, cancellationToken);

        await using GovernanceOutboxTestDbContext context = new(options);
        var store = new EfCoreGovernanceOutboxStore(context);
        DateTimeOffset claimedUtc = new(2026, 7, 12, 14, 10, 0, TimeSpan.Zero);
        GovernanceOutboxEntry claimedEntry = GovernanceOutboxEntry.Create(
                EfCoreGovernanceOutboxTestHost.CreateEnvelope("missing-claim"),
                "missing-claim-entry",
                claimedUtc.AddMinutes(-1))
            .MarkClaimed("worker-missing", "token-missing", claimedUtc, TimeSpan.FromMinutes(5));
        GovernanceOutboxClaim claim = GovernanceOutboxClaim.Create(
            claimedEntry,
            "worker-missing",
            "token-missing",
            claimedUtc,
            claimedUtc.AddMinutes(5));

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await store.SaveClaimAsync(claim, claimedEntry.MarkDeferred(), cancellationToken));

        Assert.Equal("Outbox entry 'missing-claim-entry' was not found.", exception.Message);
    }

    /// <summary>
    /// Verifies pre-cancellation prevents a matching claimed transition from reaching persistence.
    /// </summary>
    [Fact]
    public async Task ClaimedTransitionObservesCancellationBeforePersistence()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using SqliteConnection connection = await EfCoreGovernanceOutboxTestHost.OpenConnectionAsync(cancellationToken);
        DbContextOptions<GovernanceOutboxTestDbContext> options = EfCoreGovernanceOutboxTestHost.CreateOptions(connection);
        await EfCoreGovernanceOutboxTestHost.EnsureCreatedAsync(options, cancellationToken);

        await using GovernanceOutboxTestDbContext context = new(options);
        var store = new EfCoreGovernanceOutboxStore(context);
        GovernanceOutboxClaim claim = await SaveAndClaimAsync(
            context,
            store,
            "claim-cancelled",
            new DateTimeOffset(2026, 7, 12, 14, 20, 0, TimeSpan.Zero),
            cancellationToken);
        context.ChangeTracker.Clear();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await store.MarkClaimDeliveredAsync(
                claim,
                GovernanceEmissionResult.Delivered("provider-cancelled", "record-cancelled"),
                cancellation.Token));

        GovernanceOutboxEntry? persisted = await store.FindByOutboxEntryIdAsync(claim.OutboxEntryId, cancellationToken);
        Assert.NotNull(persisted);
        Assert.Equal(GovernanceEmissionStatus.Pending, persisted.Status);
        Assert.Equal(claim.WorkerId, persisted.ClaimOwner);
        Assert.Equal(claim.ClaimToken, persisted.ClaimToken);
    }

    private static async Task<GovernanceOutboxClaim> SaveAndClaimAsync(
        GovernanceOutboxTestDbContext context,
        EfCoreGovernanceOutboxStore store,
        string outboxEntryId,
        DateTimeOffset claimedUtc,
        CancellationToken cancellationToken,
        TimeSpan? leaseDuration = null)
    {
        var entry = GovernanceOutboxEntry.Create(
            EfCoreGovernanceOutboxTestHost.CreateEnvelope(
                $"event-{outboxEntryId}",
                metadata: new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["envelope.source"] = "claim-transition-test"
                }),
            outboxEntryId,
            claimedUtc.AddMinutes(-5),
            metadata: new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["entry.source"] = "claim-transition-test"
            });
        _ = await store.SaveAsync(entry, cancellationToken);
        context.ChangeTracker.Clear();

        return Assert.Single(await store.ClaimPendingAsync(
            GovernanceOutboxClaimRequest.Create(
                "worker-original",
                claimedUtc,
                leaseDuration ?? TimeSpan.FromMinutes(5),
                maxCount: 1),
            cancellationToken));
    }

    private static void AssertClaimCleared(GovernanceOutboxEntry entry)
    {
        Assert.False(entry.HasClaim);
        Assert.Null(entry.ClaimOwner);
        Assert.Null(entry.ClaimToken);
        Assert.Null(entry.ClaimedUtc);
        Assert.Null(entry.ClaimExpiresUtc);
    }
}
