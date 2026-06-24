using Microsoft.AspNetCore.Builder;

namespace AsiBackbone.AspNetCore.Endpoints;

/// <summary>
/// Provides ASP.NET Core application builder extensions for AsiBackbone endpoint governance.
/// </summary>
public static class AsiBackboneEndpointGovernanceApplicationBuilderExtensions
{
    /// <summary>
    /// Adds AsiBackbone endpoint governance middleware to the ASP.NET Core pipeline.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <returns>The same application builder so calls can be chained.</returns>
    public static IApplicationBuilder UseAsiBackboneEndpointGovernance(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        return app.UseMiddleware<AsiBackboneEndpointGovernanceMiddleware>();
    }
}
