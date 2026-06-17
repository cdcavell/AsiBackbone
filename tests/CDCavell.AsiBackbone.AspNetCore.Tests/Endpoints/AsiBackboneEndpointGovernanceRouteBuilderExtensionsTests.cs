using System.Reflection;
using CDCavell.AsiBackbone.AspNetCore.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CDCavell.AsiBackbone.AspNetCore.Tests.Endpoints;

public sealed class AsiBackboneEndpointGovernanceRouteBuilderExtensionsTests
{
    [Fact]
    public void RequireGovernancePolicy_RouteHandlerBuilder_AddsPolicyMetadataAndReturnsSameBuilder()
    {
        var app = WebApplication.Create();

        RouteHandlerBuilder routeBuilder = app.MapGet(
            "/governed",
            static () => Results.Ok());

        RouteHandlerBuilder returned = routeBuilder.RequireGovernancePolicy<TestPolicy>();

        Assert.Same(routeBuilder, returned);

        Endpoint endpoint = Assert.Single(
            app.Services.GetRequiredService<EndpointDataSource>().Endpoints);

        RequireGovernancePolicyAttribute metadata =
            Assert.Single(endpoint.Metadata.OfType<RequireGovernancePolicyAttribute>());

        Assert.Equal(typeof(TestPolicy), metadata.PolicyType);
    }

    [Fact]
    public void RequireGovernancePolicy_EndpointConventionBuilder_AddsPolicyMetadataAndReturnsSameBuilder()
    {
        var builder = new CapturingEndpointConventionBuilder();

        CapturingEndpointConventionBuilder returned =
            builder.RequireGovernancePolicy(typeof(TestPolicy));

        Assert.Same(builder, returned);

        EndpointBuilder endpointBuilder = CreateEndpointBuilder();
        Action<EndpointBuilder> convention = Assert.Single(builder.Conventions);
        convention(endpointBuilder);

        RequireGovernancePolicyAttribute metadata =
            Assert.Single(endpointBuilder.Metadata.OfType<RequireGovernancePolicyAttribute>());

        Assert.Equal(typeof(TestPolicy), metadata.PolicyType);
    }

    [Fact]
    public void RequireLiabilityHandshake_AddsMetadataAndReturnsSameBuilder()
    {
        var builder = new CapturingEndpointConventionBuilder();

        CapturingEndpointConventionBuilder returned = builder.RequireLiabilityHandshake();

        Assert.Same(builder, returned);

        EndpointBuilder endpointBuilder = CreateEndpointBuilder();
        Action<EndpointBuilder> convention = Assert.Single(builder.Conventions);
        convention(endpointBuilder);

        _ = Assert.Single(endpointBuilder.Metadata.OfType<RequireLiabilityHandshakeAttribute>());
    }

    [Fact]
    public void RequireCapabilityGrant_AddsMetadataAndReturnsSameBuilder()
    {
        var builder = new CapturingEndpointConventionBuilder();

        CapturingEndpointConventionBuilder returned = builder.RequireCapabilityGrant(" payments.approve ");

        Assert.Same(builder, returned);

        EndpointBuilder endpointBuilder = CreateEndpointBuilder();
        Action<EndpointBuilder> convention = Assert.Single(builder.Conventions);
        convention(endpointBuilder);

        RequireCapabilityGrantAttribute metadata =
            Assert.Single(endpointBuilder.Metadata.OfType<RequireCapabilityGrantAttribute>());

        Assert.Equal("payments.approve", metadata.Scope);
    }

    [Fact]
    public void EmitGovernanceAudit_AddsMetadataAndReturnsSameBuilder()
    {
        var builder = new CapturingEndpointConventionBuilder();

        CapturingEndpointConventionBuilder returned = builder.EmitGovernanceAudit();

        Assert.Same(builder, returned);

        EndpointBuilder endpointBuilder = CreateEndpointBuilder();
        Action<EndpointBuilder> convention = Assert.Single(builder.Conventions);
        convention(endpointBuilder);

        _ = Assert.Single(endpointBuilder.Metadata.OfType<EmitGovernanceAuditAttribute>());
    }

    [Fact]
    public void RequireGovernancePolicy_ThrowsWhenPolicyTypeIsNull()
    {
        var builder = new CapturingEndpointConventionBuilder();

        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(
            () => builder.RequireGovernancePolicy(null!));

        Assert.Equal("policyType", exception.ParamName);
    }

    [Fact]
    public void MetadataExtensions_ThrowWhenBuilderIsNull()
    {
        CapturingEndpointConventionBuilder? builder = null;

        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(
            () => builder!.RequireLiabilityHandshake());

        Assert.Equal("builder", exception.ParamName);
    }

    [Fact]
    public void AddEndpointMetadata_ThrowsWhenMetadataIsNull()
    {
        MethodInfo method = typeof(AsiBackboneEndpointGovernanceRouteBuilderExtensions)
            .GetMethod("AddEndpointMetadata", BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(typeof(CapturingEndpointConventionBuilder));

        TargetInvocationException exception = Assert.Throws<TargetInvocationException>(
            () => method.Invoke(
                null,
                [new CapturingEndpointConventionBuilder(), null!]));

        ArgumentNullException innerException =
            Assert.IsType<ArgumentNullException>(exception.InnerException);

        Assert.Equal("metadata", innerException.ParamName);
    }

    private static EndpointBuilder CreateEndpointBuilder()
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

    private sealed class TestPolicy
    {
    }
}
