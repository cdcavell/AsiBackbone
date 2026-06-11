using CDCavell.AsiBackbone.AspNetCore.DependencyInjection;
using Xunit;

namespace CDCavell.AsiBackbone.AspNetCore.Tests.DependencyInjection;

public sealed class AsiBackboneAspNetCoreOptionsBranchTests
{
    [Fact]
    public void IncludeEndpointMetadataGetterReflectsDisplayNameAndRoutePatternFlags()
    {
        var options = new AsiBackboneAspNetCoreOptions
        {
            IncludeEndpointDisplayName = false,
            IncludeRoutePattern = false,
        };

        Assert.False(options.IncludeEndpointMetadata);

        options.IncludeEndpointDisplayName = true;

        Assert.True(options.IncludeEndpointMetadata);

        options.IncludeEndpointDisplayName = false;
        options.IncludeRoutePattern = true;

        Assert.True(options.IncludeEndpointMetadata);
    }

    [Fact]
    public void IncludeEndpointMetadataSetterUpdatesBothEndpointFlags()
    {
        var options = new AsiBackboneAspNetCoreOptions
        {
            IncludeEndpointMetadata = false,
        };

        Assert.False(options.IncludeEndpointDisplayName);
        Assert.False(options.IncludeRoutePattern);

        options.IncludeEndpointMetadata = true;

        Assert.True(options.IncludeEndpointDisplayName);
        Assert.True(options.IncludeRoutePattern);
    }

    [Fact]
    public void CorrelationIdHeaderNameReturnsEmptyWhenNoHeaderNamesAreConfigured()
    {
        var options = new AsiBackboneAspNetCoreOptions
        {
            CorrelationIdHeaderNames = [],
        };

        Assert.Equal(string.Empty, options.CorrelationIdHeaderName);
    }

    [Fact]
    public void CorrelationIdHeaderNameSetterReplacesConfiguredHeaderNames()
    {
        var options = new AsiBackboneAspNetCoreOptions
        {
            CorrelationIdHeaderNames = ["X-First", "X-Second"],
        };

        options.CorrelationIdHeaderName = "X-Replacement";

        Assert.Equal("X-Replacement", options.CorrelationIdHeaderName);
        Assert.Equal(["X-Replacement"], options.CorrelationIdHeaderNames);
    }

    [Fact]
    public void ValidateAcceptsHeaderNamesWhenAtLeastOneConfiguredHeaderIsNotBlank()
    {
        var options = new AsiBackboneAspNetCoreOptions
        {
            CorrelationIdHeaderNames = [" ", "X-Correlation-ID"],
        };

        Exception? exception = Record.Exception(options.Validate);

        Assert.Null(exception);
    }

    [Fact]
    public void ValidateRejectsNullCorrelationHeaderNames()
    {
        var options = new AsiBackboneAspNetCoreOptions
        {
            CorrelationIdHeaderNames = null!,
        };

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(options.Validate);

        Assert.Contains("correlation identifier header name", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ValidateRejectsEmptyCorrelationHeaderNames()
    {
        var options = new AsiBackboneAspNetCoreOptions
        {
            CorrelationIdHeaderNames = [],
        };

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(options.Validate);

        Assert.Contains("correlation identifier header name", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ValidateRejectsWhitespaceOnlyCorrelationHeaderNames()
    {
        var options = new AsiBackboneAspNetCoreOptions
        {
            CorrelationIdHeaderNames = [" ", "\t"],
        };

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(options.Validate);

        Assert.Contains("correlation identifier header name", exception.Message, StringComparison.Ordinal);
    }
}
