using AsiBackbone.Core.Emissions;

namespace AsiBackbone.OpenTelemetry;

/// <summary>
/// Provides host-owned options for the OpenTelemetry governance emitter.
/// </summary>
public sealed class OpenTelemetryGovernanceEmitterOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether activity events and activity tags should be emitted.
    /// </summary>
    public bool EmitActivityEvents { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether low-cardinality counters and histograms should be emitted.
    /// </summary>
    public bool EmitMetrics { get; set; } = true;

    /// <summary>
    /// Gets or sets the provider name returned through provider-neutral emission results.
    /// </summary>
    public string ProviderName { get; set; } = OpenTelemetryGovernanceInstrumentation.ProviderName;

    /// <summary>
    /// Gets or sets the activity name used when the envelope does not include an operation name.
    /// </summary>
    public string DefaultActivityName { get; set; } = OpenTelemetryGovernanceInstrumentation.DefaultActivityName;

    /// <summary>
    /// Gets or sets an optional hook invoked before diagnostics are emitted.
    /// </summary>
    /// <remarks>
    /// This hook is intended for tests and host-owned validation seams. It should not emit protected content.
    /// </remarks>
    public Func<GovernanceEmissionEnvelope, CancellationToken, ValueTask>? BeforeEmitAsync { get; set; }

    /// <summary>
    /// Validates the configured options.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when an option value is invalid.</exception>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(ProviderName))
        {
            throw new InvalidOperationException("OpenTelemetry governance provider name is required.");
        }

        if (string.IsNullOrWhiteSpace(DefaultActivityName))
        {
            throw new InvalidOperationException("OpenTelemetry governance default activity name is required.");
        }
    }
}
