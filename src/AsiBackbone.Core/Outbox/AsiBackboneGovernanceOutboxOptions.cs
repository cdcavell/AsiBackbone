namespace AsiBackbone.Core.Outbox;

/// <summary>
/// Provides provider-neutral retry timing options for governance outbox drain processing.
/// </summary>
/// <remarks>
/// These options control default retry timestamps when a downstream emitter does not provide its own retry-after value.
/// </remarks>
public sealed class AsiBackboneGovernanceOutboxOptions
{
    /// <summary>
    /// Gets or sets the default delay applied after a transient emission failure or unexpected emitter exception.
    /// </summary>
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Gets or sets the default delay applied when an emitter returns a pending or deferred result without a retry-after timestamp.
    /// </summary>
    public TimeSpan DeferredDelay { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Validates the configured outbox timing options.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when a delay is negative.</exception>
    public void Validate()
    {
        ValidateDelay(RetryDelay, nameof(RetryDelay));
        ValidateDelay(DeferredDelay, nameof(DeferredDelay));
    }

    private static void ValidateDelay(TimeSpan delay, string propertyName)
    {
        if (delay < TimeSpan.Zero)
        {
            throw new InvalidOperationException($"{propertyName} must be greater than or equal to TimeSpan.Zero.");
        }
    }
}
