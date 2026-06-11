namespace CDCavell.AsiBackbone.AspNetCore.Correlation;

/// <summary>
/// Resolves framework-neutral request correlation data from the current ASP.NET Core HTTP request.
/// </summary>
public interface IAsiBackboneHttpRequestCorrelationResolver
{
    /// <summary>
    /// Resolves correlation identifiers and safe request metadata for the current HTTP request.
    /// </summary>
    /// <returns>The resolved request correlation data.</returns>
    AsiBackboneHttpRequestCorrelation ResolveRequestCorrelation();
}
