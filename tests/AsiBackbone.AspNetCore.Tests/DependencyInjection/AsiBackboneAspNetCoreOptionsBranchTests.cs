using AsiBackbone.AspNetCore.DependencyInjection;
using Xunit;

namespace AsiBackbone.AspNetCore.Tests.DependencyInjection;

/// <summary>
/// Tests for the <see cref="AsiBackboneAspNetCoreOptions"/> class, focusing on the behavior of its properties and validation logic.
/// </summary>
public sealed class AsiBackboneAspNetCoreOptionsBranchTests
{
    /// <summary>
    /// Tests that the <see cref="AsiBackboneAspNetCoreOptions.IncludeEndpointMetadata"/> property correctly reflects the state of the <see cref="AsiBackboneAspNetCoreOptions.IncludeEndpointDisplayName"/> and <see cref="AsiBackboneAspNetCoreOptions.IncludeRoutePattern"/> properties.
    /// </summary>
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

    /// <summary>
    /// Tests that setting the <see cref="AsiBackboneAspNetCoreOptions.IncludeEndpointMetadata"/> property updates both the <see cref="AsiBackboneAspNetCoreOptions.IncludeEndpointDisplayName"/> and <see cref="AsiBackboneAspNetCoreOptions.IncludeRoutePattern"/> properties accordingly.
    /// </summary>
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

    /// <summary>
    /// Tests that the <see cref="AsiBackboneAspNetCoreOptions.CorrelationIdHeaderName"/> property returns an empty string when no header names are configured.
    /// </summary>
    [Fact]
    public void CorrelationIdHeaderNameReturnsEmptyWhenNoHeaderNamesAreConfigured()
    {
        var options = new AsiBackboneAspNetCoreOptions
        {
            CorrelationIdHeaderNames = [],
        };

        Assert.Equal(string.Empty, options.CorrelationIdHeaderName);
    }

    /// <summary>
    /// Tests that setting the <see cref="AsiBackboneAspNetCoreOptions.CorrelationIdHeaderName"/> property replaces any previously configured header names in the <see cref="AsiBackboneAspNetCoreOptions.CorrelationIdHeaderNames"/> collection.
    /// </summary>
    [Fact]
    public void CorrelationIdHeaderNameSetterReplacesConfiguredHeaderNames()
    {
        var options = new AsiBackboneAspNetCoreOptions
        {
            CorrelationIdHeaderNames = ["X-First", "X-Second"],
            CorrelationIdHeaderName = "X-Replacement"
        };

        Assert.Equal("X-Replacement", options.CorrelationIdHeaderName);
        Assert.Equal(["X-Replacement"], options.CorrelationIdHeaderNames);
    }

    /// <summary>
    /// Tests that the <see cref="AsiBackboneAspNetCoreOptions.Validate"/> method accepts header names when at least one configured header is not blank.
    /// </summary>
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

    /// <summary>
    /// Tests that the <see cref="AsiBackboneAspNetCoreOptions.Validate"/> method throws an <see cref="InvalidOperationException"/> when the <see cref="AsiBackboneAspNetCoreOptions.CorrelationIdHeaderNames"/> property is set to null.
    /// </summary>
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

    /// <summary>
    /// Tests that the <see cref="AsiBackboneAspNetCoreOptions.Validate"/> method throws an <see cref="InvalidOperationException"/> when the <see cref="AsiBackboneAspNetCoreOptions.CorrelationIdHeaderNames"/> property is set to an empty collection.
    /// </summary>
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

    /// <summary>
    /// Tests that the <see cref="AsiBackboneAspNetCoreOptions.Validate"/> method throws an <see cref="InvalidOperationException"/> when the <see cref="AsiBackboneAspNetCoreOptions.CorrelationIdHeaderNames"/> property is set to a collection containing only whitespace.
    /// </summary>
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
