namespace CDCavell.AsiBackbone.AspNetCore.Outbox;

/// <summary>
/// Provides host-owned scheduling options for the governance outbox drain worker.
/// </summary>
public sealed class AsiBackboneGovernanceOutboxDrainWorkerOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether the hosted drain worker should run.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum number of pending or retry-ready entries attempted per drain pass.
    /// </summary>
    public int BatchSize { get; set; } = 100;

    /// <summary>
    /// Gets or sets the interval between drain passes when the worker is enabled.
    /// </summary>
    public TimeSpan PollingInterval { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets or sets the delay after an unexpected worker-level failure before the next drain pass is attempted.
    /// </summary>
    public TimeSpan FailureDelay { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets or sets the retry clock used when finding retry-ready entries.
    /// </summary>
    public Func<DateTimeOffset> RetryClock { get; set; } = static () => DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets a value indicating whether a final drain pass should be attempted during host shutdown.
    /// </summary>
    public bool DrainOnShutdown { get; set; }

    /// <summary>
    /// Gets or sets the maximum amount of time allowed for an optional shutdown drain pass.
    /// </summary>
    public TimeSpan ShutdownDrainTimeout { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Validates the configured worker options.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when a required worker option is invalid.</exception>
    public void Validate()
    {
        if (BatchSize <= 0)
        {
            throw new InvalidOperationException("Governance outbox drain batch size must be greater than zero.");
        }

        if (PollingInterval <= TimeSpan.Zero)
        {
            throw new InvalidOperationException("Governance outbox drain polling interval must be greater than zero.");
        }

        if (FailureDelay <= TimeSpan.Zero)
        {
            throw new InvalidOperationException("Governance outbox drain failure delay must be greater than zero.");
        }

        if (ShutdownDrainTimeout <= TimeSpan.Zero)
        {
            throw new InvalidOperationException("Governance outbox drain shutdown timeout must be greater than zero.");
        }

        if (RetryClock is null)
        {
            throw new InvalidOperationException("Governance outbox drain retry clock must be configured.");
        }
    }
}