using System.Diagnostics;
using AsiBackbone.AspNetCore.DependencyInjection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace AsiBackbone.AspNetCore.Correlation;

/// <summary>
/// Resolves safe request correlation data from the current ASP.NET Core HTTP context.
/// </summary>
public sealed class HttpContextAsiBackboneRequestCorrelationResolver : IAsiBackboneHttpRequestCorrelationResolver
{
    private readonly IHttpContextAccessor httpContextAccessor;
    private readonly AsiBackboneAspNetCoreOptions options;

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpContextAsiBackboneRequestCorrelationResolver" /> class.
    /// </summary>
    /// <param name="httpContextAccessor">The ASP.NET Core HTTP context accessor.</param>
    /// <param name="options">The request correlation options.</param>
    public HttpContextAsiBackboneRequestCorrelationResolver(
        IHttpContextAccessor httpContextAccessor,
        IOptions<AsiBackboneAspNetCoreOptions> options)
    {
        this.httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        this.options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        this.options.Validate();
    }

    /// <inheritdoc />
    public AsiBackboneHttpRequestCorrelation ResolveRequestCorrelation()
    {
        HttpContext? httpContext = httpContextAccessor.HttpContext;

        return httpContext is null
            ? new AsiBackboneHttpRequestCorrelation(traceId: Activity.Current?.Id)
            : new AsiBackboneHttpRequestCorrelation(
            ResolveCorrelationId(httpContext),
            ResolveTraceId(httpContext),
            ResolveMetadata(httpContext));
    }

    private string? ResolveCorrelationId(HttpContext httpContext)
    {
        foreach (string headerName in options.CorrelationIdHeaderNames)
        {
            if (string.IsNullOrWhiteSpace(headerName))
            {
                continue;
            }

            if (httpContext.Request.Headers.TryGetValue(headerName, out StringValues values))
            {
                string? value = values.FirstOrDefault(static candidate => !string.IsNullOrWhiteSpace(candidate));
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value.Trim();
                }
            }
        }

        return options.UseHttpContextTraceIdentifierAsCorrelationId
            ? httpContext.TraceIdentifier
            : null;
    }

    private static string? ResolveTraceId(HttpContext httpContext)
    {
        return Activity.Current?.Id ?? httpContext.TraceIdentifier;
    }

    private Dictionary<string, string> ResolveMetadata(HttpContext httpContext)
    {
        Dictionary<string, string> metadata = new(StringComparer.Ordinal);

        if (options.IncludeRequestMethod && !string.IsNullOrWhiteSpace(httpContext.Request.Method))
        {
            metadata[AsiBackboneHttpRequestMetadataKeys.Method] = httpContext.Request.Method.Trim();
        }

        if (options.IncludeRequestPath && httpContext.Request.Path.HasValue)
        {
            metadata[AsiBackboneHttpRequestMetadataKeys.Path] = httpContext.Request.Path.Value;
        }

        Endpoint? endpoint = httpContext.GetEndpoint();
        var routeEndpoint = endpoint as RouteEndpoint;

        if (options.IncludeEndpointDisplayName && !string.IsNullOrWhiteSpace(endpoint?.DisplayName))
        {
            metadata[AsiBackboneHttpRequestMetadataKeys.EndpointDisplayName] = endpoint.DisplayName.Trim();
        }

        if (options.IncludeRoutePattern && !string.IsNullOrWhiteSpace(routeEndpoint?.RoutePattern.RawText))
        {
            metadata[AsiBackboneHttpRequestMetadataKeys.RoutePattern] = routeEndpoint.RoutePattern.RawText.Trim();
        }

        if (options.IncludeRouteValues)
        {
            foreach (KeyValuePair<string, object?> routeValue in httpContext.Request.RouteValues)
            {
                if (routeValue.Value is null || string.IsNullOrWhiteSpace(routeValue.Key))
                {
                    continue;
                }

                metadata[$"{AsiBackboneHttpRequestMetadataKeys.RouteValuePrefix}{routeValue.Key.Trim()}"] =
                    routeValue.Value.ToString()?.Trim() ?? string.Empty;
            }
        }

        return metadata;
    }
}
