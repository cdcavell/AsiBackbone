namespace AsiBackbone.Signing.ManagedKey;

/// <summary>
/// Calculates bounded exponential retry delays with jitter.
/// </summary>
internal static class ManagedKeyRetryBackoff
{
    internal const string StrategyName = "exponential-jitter";

    /// <summary>
    /// Calculates a monotonically increasing retry delay for the one-based retry attempt.
    /// </summary>
    internal static TimeSpan CalculateDelay(
        TimeSpan baseDelay,
        TimeSpan maxDelay,
        int retryAttempt,
        double jitterSample,
        TimeSpan previousDelay)
    {
        if (baseDelay < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(baseDelay));
        }

        if (maxDelay < TimeSpan.Zero || maxDelay < baseDelay)
        {
            throw new ArgumentOutOfRangeException(nameof(maxDelay));
        }

        if (retryAttempt <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(retryAttempt));
        }

        if (double.IsNaN(jitterSample) || jitterSample < 0d || jitterSample > 1d)
        {
            throw new ArgumentOutOfRangeException(nameof(jitterSample));
        }

        if (previousDelay < TimeSpan.Zero || previousDelay > maxDelay)
        {
            throw new ArgumentOutOfRangeException(nameof(previousDelay));
        }

        if (baseDelay == TimeSpan.Zero)
        {
            return TimeSpan.Zero;
        }

        long upperBoundTicks = CalculateUpperBoundTicks(baseDelay.Ticks, maxDelay.Ticks, retryAttempt);
        long exponentialLowerBoundTicks = upperBoundTicks / 2;
        long monotonicLowerBoundTicks = previousDelay.Ticks < upperBoundTicks
            ? previousDelay.Ticks + 1
            : upperBoundTicks;
        long lowerBoundTicks = Math.Min(
            upperBoundTicks,
            Math.Max(exponentialLowerBoundTicks, monotonicLowerBoundTicks));
        long jitterRangeTicks = upperBoundTicks - lowerBoundTicks;
        long jitteredTicks = jitterSample >= 1d
            ? upperBoundTicks
            : lowerBoundTicks + (long)(jitterRangeTicks * jitterSample);

        return TimeSpan.FromTicks(Math.Min(jitteredTicks, maxDelay.Ticks));
    }

    private static long CalculateUpperBoundTicks(long baseDelayTicks, long maxDelayTicks, int retryAttempt)
    {
        long upperBoundTicks = Math.Min(baseDelayTicks, maxDelayTicks);

        for (int attempt = 1; attempt < retryAttempt && upperBoundTicks < maxDelayTicks; attempt++)
        {
            upperBoundTicks = upperBoundTicks > maxDelayTicks / 2
                ? maxDelayTicks
                : Math.Min(upperBoundTicks * 2, maxDelayTicks);
        }

        return upperBoundTicks;
    }
}

/// <summary>
/// Supplies a random sample used by the retry backoff calculation.
/// </summary>
internal interface IManagedKeyRetryJitterSource
{
    double NextDouble();
}

/// <summary>
/// Delays a retry while preserving cancellation.
/// </summary>
internal interface IManagedKeyRetryDelay
{
    ValueTask DelayAsync(TimeSpan delay, CancellationToken cancellationToken);
}

internal sealed class SharedRandomManagedKeyRetryJitterSource : IManagedKeyRetryJitterSource
{
    internal static SharedRandomManagedKeyRetryJitterSource Instance { get; } = new();

    private SharedRandomManagedKeyRetryJitterSource()
    {
    }

    public double NextDouble()
    {
        return Random.Shared.NextDouble();
    }
}

internal sealed class SystemManagedKeyRetryDelay : IManagedKeyRetryDelay
{
    internal static SystemManagedKeyRetryDelay Instance { get; } = new();

    private SystemManagedKeyRetryDelay()
    {
    }

    public ValueTask DelayAsync(TimeSpan delay, CancellationToken cancellationToken)
    {
        return delay <= TimeSpan.Zero
            ? ValueTask.CompletedTask
            : new ValueTask(Task.Delay(delay, cancellationToken));
    }
}
