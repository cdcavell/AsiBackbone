using AsiBackbone.Core.Emissions;
using AsiBackbone.Core.Outbox;
using AsiBackbone.EntityFrameworkCore.Outbox;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit;

namespace AsiBackbone.EntityFrameworkCore.Tests.Outbox;

/// <summary>
/// Verifies the caller-owned concurrency contract for non-claim EF Core outbox mutations.
/// </summary>
public sealed class EfCoreGovernanceOutboxNonClaimConcurrencyTests
{
    /// <summary>
    /// Verifies that <see cref="EfCoreGovernanceOutboxStore.SaveAsync" /> preserves and propagates the original EF Core concurrency exception.
    /// </summary>
    [Fact]
    public async Task SaveAsyncPropagatesOriginalConcurrencyException()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using SqliteConnection keeperConnection = await EfCoreGovernanceOutboxTestHost.OpenSharedMemoryKeeperConnectionAsync(cancellationToken);
        DbContextOptions<GovernanceOutboxTestDbContext> options = EfCoreGovernanceOutboxTestHost.CreateOptions(keeperConnection.ConnectionString);
        await EfCoreGovernanceOutboxTestHost.EnsureCreatedAsync(options, cancellationToken);

        string outboxEntryId;
        await using (GovernanceOutboxTestDbContext seedContext = new(options))
        {
            var seedStore = new EfCoreGovernanceOutboxStore(seedContext);
            GovernanceOutboxEntry seeded = await seedStore.EnqueueAsync(
                EfCoreGovernanceOutboxTestHost.CreateEnvelope("non-claim-save"),
                cancellationToken);
            outboxEntryId = seeded.OutboxEntryId;
        }

        await using GovernanceOutboxTestDbContext staleContext = new(options);
        await using GovernanceOutboxTestDbContext winningContext = new(options);
        var staleStore = new EfCoreGovernanceOutboxStore(staleContext);
        var winningStore = new EfCoreGovernanceOutboxStore(winningContext);

        GovernanceOutboxEntry staleEntry = Assert.IsType<GovernanceOutboxEntry>(
            await staleStore.FindByOutboxEntryIdAsync(outboxEntryId, cancellationToken));
        _ = await staleContext.GovernanceOutboxEntries.SingleAsync(
            item => item.OutboxEntryId == outboxEntryId,
            cancellationToken);

        _ = await winningStore.MarkFailedAsync(
            outboxEntryId,
            GovernanceEmissionError.Create("winner.failed", "The concurrent writer won."),
            cancellationToken: cancellationToken);

        GovernanceOutboxEntry staleUpdate = staleEntry.MarkDelivered(
            GovernanceEmissionResult.Delivered("stale-provider", "stale-record"));

        DbUpdateConcurrencyException exception = await Assert.ThrowsAsync<DbUpdateConcurrencyException>(async () =>
            await staleStore.SaveAsync(staleUpdate, cancellationToken));

        Assert.NotEmpty(exception.Entries);
    }

    /// <summary>
    /// Verifies that <see cref="EfCoreGovernanceOutboxStore.MarkDeliveredAsync" /> does not hide, translate, or retry a lost non-claim write.
    /// </summary>
    [Fact]
    public async Task MarkDeliveredAsyncPropagatesConcurrencyExceptionWithoutHiddenRetry()
    {
        await AssertMutationPropagatesConcurrencyAsync(
            (store, outboxEntryId, cancellationToken) => store.MarkDeliveredAsync(
                outboxEntryId,
                GovernanceEmissionResult.Delivered("losing-provider", "losing-record"),
                cancellationToken));
    }

    /// <summary>
    /// Verifies that <see cref="EfCoreGovernanceOutboxStore.MarkFailedAsync" /> does not hide, translate, or retry a lost non-claim write.
    /// </summary>
    [Fact]
    public async Task MarkFailedAsyncPropagatesConcurrencyExceptionWithoutHiddenRetry()
    {
        await AssertMutationPropagatesConcurrencyAsync(
            (store, outboxEntryId, cancellationToken) => store.MarkFailedAsync(
                outboxEntryId,
                GovernanceEmissionError.Create("loser.failed", "The losing writer attempted failure."),
                cancellationToken: cancellationToken));
    }

    private static async Task AssertMutationPropagatesConcurrencyAsync(
        Func<EfCoreGovernanceOutboxStore, string, CancellationToken, ValueTask<GovernanceOutboxEntry>> mutation)
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using SqliteConnection keeperConnection = await EfCoreGovernanceOutboxTestHost.OpenSharedMemoryKeeperConnectionAsync(cancellationToken);
        DbContextOptions<GovernanceOutboxTestDbContext> durableOptions = EfCoreGovernanceOutboxTestHost.CreateOptions(keeperConnection.ConnectionString);
        await EfCoreGovernanceOutboxTestHost.EnsureCreatedAsync(durableOptions, cancellationToken);

        var interceptor = new OneShotSaveChangesInterceptor();
        DbContextOptions<GovernanceOutboxTestDbContext> staleOptions = EfCoreGovernanceOutboxTestHost.CreateOptions(
            keeperConnection.ConnectionString,
            interceptor);

        string outboxEntryId;
        await using (GovernanceOutboxTestDbContext seedContext = new(durableOptions))
        {
            var seedStore = new EfCoreGovernanceOutboxStore(seedContext);
            GovernanceOutboxEntry seeded = await seedStore.EnqueueAsync(
                EfCoreGovernanceOutboxTestHost.CreateEnvelope($"non-claim-{Guid.NewGuid():N}"),
                cancellationToken);
            outboxEntryId = seeded.OutboxEntryId;
        }

        await using GovernanceOutboxTestDbContext staleContext = new(staleOptions);
        var staleStore = new EfCoreGovernanceOutboxStore(staleContext);
        interceptor.Arm(async callbackCancellationToken =>
        {
            await using GovernanceOutboxTestDbContext winningContext = new(durableOptions);
            var winningStore = new EfCoreGovernanceOutboxStore(winningContext);
            _ = await winningStore.MarkDeadLetteredAsync(
                outboxEntryId,
                GovernanceEmissionError.Create("winner.deadletter", "The concurrent writer completed first."),
                "Concurrent writer won.",
                callbackCancellationToken);
        });

        DbUpdateConcurrencyException exception = await Assert.ThrowsAsync<DbUpdateConcurrencyException>(async () =>
            await mutation(staleStore, outboxEntryId, cancellationToken));

        Assert.NotEmpty(exception.Entries);

        await using GovernanceOutboxTestDbContext verificationContext = new(durableOptions);
        var verificationStore = new EfCoreGovernanceOutboxStore(verificationContext);
        GovernanceOutboxEntry durableWinner = Assert.IsType<GovernanceOutboxEntry>(
            await verificationStore.FindByOutboxEntryIdAsync(outboxEntryId, cancellationToken));

        Assert.True(durableWinner.IsDeadLettered);
        Assert.Equal("winner.deadletter", durableWinner.LastError?.Code);
    }

    private sealed class OneShotSaveChangesInterceptor : SaveChangesInterceptor
    {
        private Func<CancellationToken, Task>? beforeSaveAsync;

        public void Arm(Func<CancellationToken, Task> callback)
        {
            ArgumentNullException.ThrowIfNull(callback);

            if (Interlocked.CompareExchange(ref beforeSaveAsync, callback, null) is not null)
            {
                throw new InvalidOperationException("The save interceptor is already armed.");
            }
        }

        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            _ = eventData;
            Func<CancellationToken, Task>? callback = Interlocked.Exchange(ref beforeSaveAsync, null);
            if (callback is not null)
            {
                await callback(cancellationToken).ConfigureAwait(false);
            }

            return result;
        }
    }
}
