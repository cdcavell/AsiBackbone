namespace CDCavell.AsiBackbone.AspNetCore.DependencyInjection;

/// <summary>
/// Provides configuration options for ASP.NET Core host integration.
/// </summary>
public sealed class AsiBackboneAspNetCoreOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether HTTP route values may be included by later request-context adapters.
    /// </summary>
    public bool IncludeRouteValues { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether endpoint metadata may be inspected by later request-context adapters.
    /// </summary>
    public bool IncludeEndpointMetadata { get; set; } = true;

    /// <summary>
    /// Gets or sets the HTTP header name preferred for correlation identifier propagation.
    /// </summary>
    public string CorrelationIdHeaderName { get; set; } = "X-Correlation-ID";

    /// <summary>
    /// Validates the options.
    /// </summary>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(CorrelationIdHeaderName))
        {
            throw new InvalidOperationException("CorrelationIdHeaderName must be configured.");
        }
    }
}
