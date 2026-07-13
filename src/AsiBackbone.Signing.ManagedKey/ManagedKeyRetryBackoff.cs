namespace AsiBackbone.Signing.ManagedKey;

/// <summary>
/// Calculates bounded exponential retry delays with full jitter.
/// </summary>
internal static class ManagedKeyRetryBackoff
{
    internal const string StrategyName = "exponential-full-jitter";

    /// <summary>
    /// Calculates a retry delay for the one-based retry attempt.
    /// </summary>
    internal static TimeSpan CalculateDelay(
        TimeSpan baseDelay,
        TimeSpan maxDelay,
        int retryAttempt,
        double jitterSample,
        TimeSpan? providerRetryAfter = null)
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

        if (providerRetryAfter < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(providerRetryAfter));
        }

        long upperBoundTicks = CalculateUpperBoundTicks(baseDelay.Ticks, maxDelay.Ticks, retryAttempt);
        long jitteredTicks = jitterSample >= 1d
            ? upperBoundTicks
            : (long)(upperBoundTicks * jitterSample);
        long providerMinimumTicks = providerRetryAfter is null
            ? 0
            : Math.Min(providerRetryAfter.Value.Ticks, maxDelay.Ticks);

        return TimeSpan.FromTicks(Math.Max(jitteredTicks, providerMinimumTicks));
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
