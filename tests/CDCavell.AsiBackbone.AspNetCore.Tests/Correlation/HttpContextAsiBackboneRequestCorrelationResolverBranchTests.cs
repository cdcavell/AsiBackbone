using CDCavell.AsiBackbone.AspNetCore.Correlation;
using CDCavell.AsiBackbone.AspNetCore.DependencyInjection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.Options;
using Xunit;

namespace CDCavell.AsiBackbone.AspNetCore.Tests.Correlation;

public sealed class HttpContextAsiBackboneRequestCorrelationResolverBranchTests
{
    [Fact]
    public void ResolveRequestCorrelationSkipsBlankHeaderNamesAndBlankHeaderValues()
    {
        DefaultHttpContext httpContext = new()
        {
            TraceIdentifier = "trace-blank-header",
        };
        httpContext.Request.Headers["X-Blank"] = "   ";
        httpContext.Request.Headers["X-Valid"] = " correlation-valid ";

        HttpContextAsiBackboneRequestCorrelationResolver resolver = CreateResolver(
            httpContext,
            options => options.CorrelationIdHeaderNames = [" ", "X-Blank", "X-Valid"]);

        AsiBackboneHttpRequestCorrelation correlation = resolver.ResolveRequestCorrelation();

        Assert.Equal("correlation-valid", correlation.CorrelationId);
    }

    [Fact]
    public void ResolveRequestCorrelationReturnsNullCorrelationIdWhenTraceFallbackDisabled()
    {
        DefaultHttpContext httpContext = new()
        {
            TraceIdentifier = "trace-no-fallback",
        };

        HttpContextAsiBackboneRequestCorrelationResolver resolver = CreateResolver(
            httpContext,
            options => options.UseHttpContextTraceIdentifierAsCorrelationId = false);

        AsiBackboneHttpRequestCorrelation correlation = resolver.ResolveRequestCorrelation();

        Assert.Null(correlation.CorrelationId);
        Assert.Equal("trace-no-fallback", correlation.TraceId);
    }

    [Fact]
    public void ResolveRequestCorrelationOmitsRequestMethodWhenBlank()
    {
        DefaultHttpContext httpContext = new()
        {
            TraceIdentifier = "trace-no-method",
        };
        httpContext.Request.Method = " ";

        HttpContextAsiBackboneRequestCorrelationResolver resolver = CreateResolver(httpContext);

        AsiBackboneHttpRequestCorrelation correlation = resolver.ResolveRequestCorrelation();

        Assert.False(correlation.Metadata.ContainsKey(AsiBackboneHttpRequestMetadataKeys.Method));
    }

    [Fact]
    public void ResolveRequestCorrelationOmitsRouteMetadataWhenEndpointIsNotRouteEndpoint()
    {
        DefaultHttpContext httpContext = new()
        {
            TraceIdentifier = "trace-non-route-endpoint",
        };
        httpContext.SetEndpoint(new Endpoint(_ => Task.CompletedTask, EndpointMetadataCollection.Empty, "Plain endpoint"));

        HttpContextAsiBackboneRequestCorrelationResolver resolver = CreateResolver(httpContext);

        AsiBackboneHttpRequestCorrelation correlation = resolver.ResolveRequestCorrelation();

        Assert.Equal("Plain endpoint", correlation.Metadata[AsiBackboneHttpRequestMetadataKeys.EndpointDisplayName]);
        Assert.False(correlation.Metadata.ContainsKey(AsiBackboneHttpRequestMetadataKeys.RoutePattern));
    }

    [Fact]
    public void ResolveRequestCorrelationOmitsEndpointDisplayNameWhenBlank()
    {
        DefaultHttpContext httpContext = new()
        {
            TraceIdentifier = "trace-blank-endpoint-name",
        };
        httpContext.SetEndpoint(CreateRouteEndpoint("/items/{id}", " "));

        HttpContextAsiBackboneRequestCorrelationResolver resolver = CreateResolver(httpContext);

        AsiBackboneHttpRequestCorrelation correlation = resolver.ResolveRequestCorrelation();

        Assert.False(correlation.Metadata.ContainsKey(AsiBackboneHttpRequestMetadataKeys.EndpointDisplayName));
        Assert.Equal("/items/{id}", correlation.Metadata[AsiBackboneHttpRequestMetadataKeys.RoutePattern]);
    }

    [Fact]
    public void ResolveRequestCorrelationSkipsNullAndBlankRouteValues()
    {
        DefaultHttpContext httpContext = new()
        {
            TraceIdentifier = "trace-route-values",
        };
        httpContext.Request.RouteValues["id"] = "42";
        httpContext.Request.RouteValues["optional"] = null;
        httpContext.Request.RouteValues[" "] = "blank-key";

        HttpContextAsiBackboneRequestCorrelationResolver resolver = CreateResolver(httpContext);

        AsiBackboneHttpRequestCorrelation correlation = resolver.ResolveRequestCorrelation();

        Assert.Equal("42", correlation.Metadata[$"{AsiBackboneHttpRequestMetadataKeys.RouteValuePrefix}id"]);
        Assert.DoesNotContain(correlation.Metadata.Keys, key => key.Contains("optional", StringComparison.Ordinal));
        Assert.DoesNotContain(correlation.Metadata.Values, value => value == "blank-key");
    }

    [Fact]
    public void ResolveRequestCorrelationHonorsMetadataExclusionOptions()
    {
        DefaultHttpContext httpContext = new()
        {
            TraceIdentifier = "trace-exclusions",
        };
        httpContext.Request.Method = HttpMethods.Post;
        httpContext.Request.Path = "/items/42";
        httpContext.Request.RouteValues["id"] = "42";
        httpContext.SetEndpoint(CreateRouteEndpoint("/items/{id}", "Items endpoint"));

        HttpContextAsiBackboneRequestCorrelationResolver resolver = CreateResolver(
            httpContext,
            options =>
            {
                options.IncludeRequestMethod = false;
                options.IncludeEndpointMetadata = false;
                options.IncludeRouteValues = false;
            });

        AsiBackboneHttpRequestCorrelation correlation = resolver.ResolveRequestCorrelation();

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
        static Task RequestDelegate(HttpContext _)
        {
            return Task.CompletedTask;
        }

        Endpoint endpoint = new RouteEndpointBuilder(RequestDelegate, RoutePatternFactory.Parse(pattern), order: 0)
        {
            DisplayName = displayName,
        }.Build();

        return (RouteEndpoint)endpoint;
    }
}
