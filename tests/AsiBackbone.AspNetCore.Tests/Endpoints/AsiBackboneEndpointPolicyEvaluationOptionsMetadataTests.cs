using AsiBackbone.AspNetCore.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Xunit;

namespace AsiBackbone.AspNetCore.Tests.Endpoints;

public sealed class AsiBackboneEndpointPolicyEvaluationOptionsMetadataTests
{
    [Fact]
    public void DescriptorReadsShortCircuitOnFirstDenialMetadataFromEndpoint()
    {
        var endpoint = new Endpoint(
            static context => Task.CompletedTask,
            new EndpointMetadataCollection(
                new RequireGovernancePolicyAttribute(typeof(SamplePolicy)),
                new ShortCircuitOnFirstDenialAttribute()),
            "sample.fast-abort");

        AsiBackboneEndpointGovernanceDescriptor descriptor = AsiBackboneEndpointGovernanceDescriptor.FromEndpoint(endpoint);
        IReadOnlyDictionary<string, string> metadata = descriptor.ToMetadata();

        Assert.True(descriptor.HasGovernanceMetadata);
        Assert.True(descriptor.ShortCircuitOnFirstDenial);
        Assert.Equal("true", metadata["endpoint.short_circuit_on_first_denial"]);
    }

    [Fact]
    public void DescriptorUsesLastShortCircuitOnFirstDenialMetadataValue()
    {
        var endpoint = new Endpoint(
            static context => Task.CompletedTask,
            new EndpointMetadataCollection(
                new ShortCircuitOnFirstDenialAttribute(),
                new ShortCircuitOnFirstDenialAttribute(enabled: false)),
            "sample.fast-abort.override");

        AsiBackboneEndpointGovernanceDescriptor descriptor = AsiBackboneEndpointGovernanceDescriptor.FromEndpoint(endpoint);
        IReadOnlyDictionary<string, string> metadata = descriptor.ToMetadata();

        Assert.True(descriptor.HasGovernanceMetadata);
        Assert.False(descriptor.ShortCircuitOnFirstDenial);
        Assert.Equal("false", metadata["endpoint.short_circuit_on_first_denial"]);
    }

    [Fact]
    public void RouteBuilderShortCircuitOnFirstDenialAddsMetadataAndReturnsSameBuilder()
    {
        var builder = new CapturingEndpointConventionBuilder();

        CapturingEndpointConventionBuilder returned = builder.ShortCircuitOnFirstDenial();

        Assert.Same(builder, returned);

        EndpointBuilder endpointBuilder = CreateEndpointBuilder();
        Action<EndpointBuilder> convention = Assert.Single(builder.Conventions);
        convention(endpointBuilder);

        ShortCircuitOnFirstDenialAttribute metadata =
            Assert.Single(endpointBuilder.Metadata.OfType<ShortCircuitOnFirstDenialAttribute>());

        Assert.True(metadata.ShortCircuitOnFirstDenial);
    }

    [Fact]
    public void RouteBuilderShortCircuitOnFirstDenialCanDisableMetadataValue()
    {
        var builder = new CapturingEndpointConventionBuilder();

        _ = builder.ShortCircuitOnFirstDenial(enabled: false);

        EndpointBuilder endpointBuilder = CreateEndpointBuilder();
        Action<EndpointBuilder> convention = Assert.Single(builder.Conventions);
        convention(endpointBuilder);

        ShortCircuitOnFirstDenialAttribute metadata =
            Assert.Single(endpointBuilder.Metadata.OfType<ShortCircuitOnFirstDenialAttribute>());

        Assert.False(metadata.ShortCircuitOnFirstDenial);
    }

    private static RouteEndpointBuilder CreateEndpointBuilder()
    {
        return new RouteEndpointBuilder(
            static _ => Task.CompletedTask,
            RoutePatternFactory.Parse("/"),
            order: 0);
    }

    private sealed class CapturingEndpointConventionBuilder : IEndpointConventionBuilder
    {
        public List<Action<EndpointBuilder>> Conventions { get; } = [];

        public void Add(Action<EndpointBuilder> convention)
        {
            ArgumentNullException.ThrowIfNull(convention);
            Conventions.Add(convention);
        }
    }

    private sealed class SamplePolicy
    {
    }
}
