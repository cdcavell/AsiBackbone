using System.Data.Common;
using AsiBackbone.Core.Emissions;
using AsiBackbone.Core.Outbox;
using AsiBackbone.EntityFrameworkCore.Outbox;
using AsiBackbone.EntityFrameworkCore.Persistence;
using BenchmarkDotNet.Attributes;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace AsiBackbone.Benchmarks.BenchmarkDotNet;

/// <summary>
/// Measures the portable EF Core governance outbox claim path across realistic batch sizes and injected command latency.
/// </summary>
/// <remarks>
/// The benchmark uses an in-memory SQLite database so provider execution remains deterministic while a command interceptor
/// injects latency at every measured database command. The benchmark verifies the uncontended baseline command shape of
/// one candidate query plus one read and one optimistic-concurrency update per claimed row.
/// </remarks>
[MemoryDiagnoser]
[RankColumn]
[InvocationCount(1)]
[UnrollFactor(1)]
public class EfCoreOutboxClaimBenchmarks
{
    private static readonly DateTimeOffset ClaimUtc = new(2026, 7, 13, 12, 0, 0, TimeSpan.Zero);
    private readonly CommandLatencyInterceptor commandInterceptor = new();
    private SqliteConnection? connection;
    private DbContextOptions<BenchmarkGovernanceDbContext>? options;
    private int seedSequence;

    /// <summary>
    /// Gets or sets the number of pending entries requested by the claim operation.
    /// </summary>
    [Params(1, 10, 50, 100)]
    public int BatchSize { get; set; }

    /// <summary>
    /// Gets or sets the artificial latency applied to each measured database command.
    /// </summary>
    [Params(0, 1, 5)]
    public int SimulatedLatencyMilliseconds { get; set; }

    /// <summary>
    /// Creates the shared in-memory SQLite database used by the benchmark case.
    /// </summary>
    [GlobalSetup]
    public async Task GlobalSetupAsync()
    {
        connection = new SqliteConnection("Filename=:memory:");
        await connection.OpenAsync().ConfigureAwait(false);

        options = new DbContextOptionsBuilder<BenchmarkGovernanceDbContext>()
            .UseSqlite(connection)
            .AddInterceptors(commandInterceptor)
            .Options;

        await using var context = new BenchmarkGovernanceDbContext(options);
        _ = await context.Database.EnsureCreatedAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Rebuilds an uncontended pending batch before each measured invocation.
    /// </summary>
    [IterationSetup]
    public void IterationSetup()
    {
        commandInterceptor.Stop();

        using var context = new BenchmarkGovernanceDbContext(RequireOptions());
        _ = context.GovernanceOutboxEntries.ExecuteDelete();

        var store = new EfCoreGovernanceOutboxStore(context);
        int currentSeed = Interlocked.Increment(ref seedSequence);
        DateTimeOffset createdUtc = ClaimUtc.AddHours(-1);

        for (int index = 0; index < BatchSize; index++)
        {
            string suffix = $"{currentSeed:D6}-{index:D4}";
            GovernanceEmissionEnvelope envelope = GovernanceEmissionEnvelope.Create(
                GovernanceEmissionEventType.Outbox,
                $"benchmark-event-{suffix}",
                createdUtc.AddTicks(index),
                envelopeId: $"benchmark-envelope-{suffix}",
                correlationId: "efcore-outbox-claim-benchmark",
                emitterStatus: GovernanceEmissionStatus.Pending.ToString(),
                emitterProvider: "efcore-outbox");
            GovernanceOutboxEntry entry = GovernanceOutboxEntry.Create(
                envelope,
                $"benchmark-entry-{suffix}",
                createdUtc.AddTicks(index));

            _ = store.SaveAsync(entry).AsTask().GetAwaiter().GetResult();
        }

        context.ChangeTracker.Clear();
        commandInterceptor.Start(SimulatedLatencyMilliseconds);
    }

    /// <summary>
    /// Claims one ordered pending batch through the portable per-row optimistic-concurrency implementation.
    /// </summary>
    /// <returns>The number of claims won by the benchmark worker.</returns>
    [Benchmark(Description = "efcore_outbox.claim_pending_portable")]
    public async Task<int> ClaimPendingAsync()
    {
        await using var context = new BenchmarkGovernanceDbContext(RequireOptions());
        var store = new EfCoreGovernanceOutboxStore(context);

        IReadOnlyList<GovernanceOutboxClaim> claims = await store.ClaimPendingAsync(
            GovernanceOutboxClaimRequest.Create(
                "benchmark-worker",
                ClaimUtc,
                TimeSpan.FromMinutes(5),
                BatchSize))
            .ConfigureAwait(false);

        int expectedCommandCount = 1 + (2 * BatchSize);
        int observedCommandCount = commandInterceptor.CommandCount;

        if (claims.Count != BatchSize)
        {
            throw new InvalidOperationException(
                $"Expected {BatchSize} claims but observed {claims.Count}.");
        }

        if (observedCommandCount != expectedCommandCount)
        {
            throw new InvalidOperationException(
                $"Expected {expectedCommandCount} measured database commands but observed {observedCommandCount}.");
        }

        return claims.Count;
    }

    /// <summary>
    /// Disposes the shared SQLite connection after all benchmark cases complete.
    /// </summary>
    [GlobalCleanup]
    public void GlobalCleanup()
    {
        commandInterceptor.Stop();
        connection?.Dispose();
        connection = null;
        options = null;
    }

    private DbContextOptions<BenchmarkGovernanceDbContext> RequireOptions()
    {
        return options ?? throw new InvalidOperationException("The benchmark database has not been initialized.");
    }

    private sealed class BenchmarkGovernanceDbContext(DbContextOptions<BenchmarkGovernanceDbContext> contextOptions)
        : DbContext(contextOptions)
    {
        public DbSet<AsiBackboneGovernanceOutboxEntryEntity> GovernanceOutboxEntries =>
            Set<AsiBackboneGovernanceOutboxEntryEntity>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            _ = modelBuilder.ApplyAsiBackboneConfigurations();
        }
    }

    private sealed class CommandLatencyInterceptor : DbCommandInterceptor
    {
        private int commandCount;
        private int delayMilliseconds;
        private int enabled;

        public int CommandCount => Volatile.Read(ref commandCount);

        public void Start(int milliseconds)
        {
            if (milliseconds < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(milliseconds));
            }

            delayMilliseconds = milliseconds;
            Interlocked.Exchange(ref commandCount, 0);
            Volatile.Write(ref enabled, 1);
        }

        public void Stop()
        {
            Volatile.Write(ref enabled, 0);
        }

        public override async ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            _ = command;
            _ = eventData;
            await RecordCommandAsync(cancellationToken).ConfigureAwait(false);
            return result;
        }

        public override async ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            _ = command;
            _ = eventData;
            await RecordCommandAsync(cancellationToken).ConfigureAwait(false);
            return result;
        }

        public override async ValueTask<InterceptionResult<object>> ScalarExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<object> result,
            CancellationToken cancellationToken = default)
        {
            _ = command;
            _ = eventData;
            await RecordCommandAsync(cancellationToken).ConfigureAwait(false);
            return result;
        }

        private async ValueTask RecordCommandAsync(CancellationToken cancellationToken)
        {
            if (Volatile.Read(ref enabled) == 0)
            {
                return;
            }

            _ = Interlocked.Increment(ref commandCount);
            int milliseconds = delayMilliseconds;
            if (milliseconds > 0)
            {
                await Task.Delay(milliseconds, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
