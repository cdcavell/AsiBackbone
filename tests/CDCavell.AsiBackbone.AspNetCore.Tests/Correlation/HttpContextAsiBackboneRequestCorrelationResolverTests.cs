using CDCavell.AsiBackbone.AspNetCore.Correlation;
using CDCavell.AsiBackbone.AspNetCore.DependencyInjection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.Options;
using Xunit;

namespace CDCavell.AsiBackbone.AspNetCore.Tests.Correlation;

public sealed class HttpContextAsiBackboneRequestCorrelationResolverTests
{
    [Fact]
    public void ResolveRequestCorrelationUsesConfiguredHeaderBeforeTraceIdentifier()
    {
        DefaultHttpContext httpContext = new()
        {
            TraceIdentifier = "trace-123",
        };
        httpContext.Request.Headers["X-Correlation-ID"] = "  correlation-456  ";

        HttpContextAsiBackboneRequestCorrelationResolver resolver = CreateResolver(httpContext);

        AsiBackboneHttpRequestCorrelation correlation = resolver.ResolveRequestCorrelation();

        Assert.Equal("correlation-456", correlation.CorrelationId);
        Assert.Equal("trace-123", correlation.TraceId);
    }

    [Fact]
    public void ResolveRequestCorrelationFallsBackToTraceIdentifierByDefault()
    {
        DefaultHttpContext httpContext = new()
        {
            TraceIdentifier = "trace-789",
        };

        HttpContextAsiBackboneRequestCorrelationResolver resolver = CreateResolver(httpContext);

        AsiBackboneHttpRequestCorrelation correlation = resolver.ResolveRequestCorrelation();

        Assert.Equal("trace-789", correlation.CorrelationId);
        Assert.Equal("trace-789", correlation.TraceId);
    }

    [Fact]
    public void ResolveRequestCorrelationSupportsConfiguredHeaderNames()
    {
        DefaultHttpContext httpContext = new()
        {
            TraceIdentifier = "trace-abc",
        };
        httpContext.Request.Headers["X-Tenant-Correlation"] = "tenant-correlation";

        HttpContextAsiBackboneRequestCorrelationResolver resolver = CreateResolver(
            httpContext,
            options => options.CorrelationIdHeaderNames = ["X-Tenant-Correlation"]);

        AsiBackboneHttpRequestCorrelation correlation = resolver.ResolveRequestCorrelation();

        Assert.Equal("tenant-correlation", correlation.CorrelationId);
    }

    [Fact]
    public void ResolveRequestCorrelationAddsSafeRequestMetadata()
    {
        DefaultHttpContext httpContext = new()
        {
            TraceIdentifier = "trace-route",
        };
        httpContext.Request.Method = HttpMethods.Post;
        httpContext.Request.Path = "/api/widgets/42";
        httpContext.Request.RouteValues["id"] = "42";
        httpContext.SetEndpoint(CreateRouteEndpoint("/api/widgets/{id}", "Widget endpoint"));

        HttpContextAsiBackboneRequestCorrelationResolver resolver = CreateResolver(httpContext);

        AsiBackboneHttpRequestCorrelation correlation = resolver.ResolveRequestCorrelation();

        Assert.Equal(HttpMethods.Post, correlation.Metadata[AsiBackboneHttpRequestMetadataKeys.Method]);
        Assert.Equal("/api/widgets/{id}", correlation.Metadata[AsiBackboneHttpRequestMetadataKeys.RoutePattern]);
        Assert.Equal("Widget endpoint", correlation.Metadata[AsiBackboneHttpRequestMetadataKeys.EndpointDisplayName]);
        Assert.Equal("42", correlation.Metadata[$"{AsiBackboneHttpRequestMetadataKeys.RouteValuePrefix}id"]);
        Assert.False(correlation.Metadata.ContainsKey(AsiBackboneHttpRequestMetadataKeys.Path));
    }

    [Fact]
    public void ResolveRequestCorrelationCanIncludeRequestPathWhenConfigured()
    {
        DefaultHttpContext httpContext = new()
        {
            TraceIdentifier = "trace-path",
        };
        httpContext.Request.Path = "/api/widgets/42";

        HttpContextAsiBackboneRequestCorrelationResolver resolver = CreateResolver(
            httpContext,
            options => options.IncludeRequestPath = true);

        AsiBackboneHttpRequestCorrelation correlation = resolver.ResolveRequestCorrelation();

        Assert.Equal("/api/widgets/42", correlation.Metadata[AsiBackboneHttpRequestMetadataKeys.Path]);
    }

    [Fact]
    public void ResolveRequestCorrelationExcludesSensitiveRequestDataByDefault()
    {
        DefaultHttpContext httpContext = new()
        {
            TraceIdentifier = "trace-sensitive",
        };
        httpContext.Request.Method = HttpMethods.Get;
        httpContext.Request.Path = "/api/widgets";
        httpContext.Request.QueryString = new QueryString("?access_token=secret-token");
        httpContext.Request.Headers.Authorization = "Bearer secret-token";
        httpContext.Request.Headers.Cookie = "session=secret-cookie";
        httpContext.Request.Headers["X-Api-Key"] = "secret-key";

        HttpContextAsiBackboneRequestCorrelationResolver resolver = CreateResolver(httpContext);

        AsiBackboneHttpRequestCorrelation correlation = resolver.ResolveRequestCorrelation();

        Assert.DoesNotContain(correlation.Metadata.Keys, key => key.Contains("header", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(correlation.Metadata.Keys, key => key.Contains("query", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(correlation.Metadata.Values, value => value.Contains("secret", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(correlation.Metadata.Values, value => value.Contains("Bearer", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ResolveRequestCorrelationReturnsTraceOnlyForBackgroundScenario()
    {
        HttpContextAccessor httpContextAccessor = new();
        HttpContextAsiBackboneRequestCorrelationResolver resolver = new(
            httpContextAccessor,
            Options.Create(new AsiBackboneAspNetCoreOptions()));

        AsiBackboneHttpRequestCorrelation correlation = resolver.ResolveRequestCorrelation();

        Assert.Null(correlation.CorrelationId);
        Assert.Empty(correlation.Metadata);
    }

    private static HttpContextAsiBackboneRequestCorrelationResolver CreateResolver(
        HttpContext httpContext,
        Action<AsiBackboneAspNetCoreOptions>? configure = null)
    {
        AsiBackboneAspNetCoreOptions options = new();
        configure?.Invoke(options);

        return new HttpContextAsiBackboneRequestCorrelationResolver(
            new HttpContextAccessor { HttpContext = httpContext },
            Options.Create(options));
    }

    private static RouteEndpoint CreateRouteEndpoint(string pattern, string displayName)
    {
        RequestDelegate requestDelegate = static _ => Task.CompletedTask;
        return new RouteEndpointBuilder(requestDelegate, RoutePatternFactory.Parse(pattern), order: 0)
        {
            DisplayName = displayName,
        }.Build();
    }
}