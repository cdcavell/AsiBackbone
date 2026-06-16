using Microsoft.AspNetCore.Builder;

namespace CDCavell.AsiBackbone.AspNetCore.Endpoints;

/// <summary>
/// Provides Minimal API / endpoint route-builder helpers for adding AsiBackbone governance metadata.
/// </summary>
public static class AsiBackboneEndpointGovernanceRouteBuilderExtensions
{
    /// <summary>
    /// Adds a host-defined governance policy marker to an endpoint.
    /// </summary>
    /// <typeparam name="TBuilder">The endpoint convention builder type.</typeparam>
    /// <typeparam name="TPolicy">The host-defined policy marker or resolver type.</typeparam>
    /// <param name="builder">The endpoint convention builder.</param>
    /// <returns>The same builder so calls can be chained.</returns>
    public static TBuilder RequireGovernancePolicy<TBuilder, TPolicy>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder
    {
        return builder.RequireGovernancePolicy(typeof(TPolicy));
    }

    /// <summary>
    /// Adds a host-defined governance policy marker to an endpoint.
    /// </summary>
    /// <typeparam name="TBuilder">The endpoint convention builder type.</typeparam>
    /// <param name="builder">The endpoint convention builder.</param>
    /// <param name="policyType">The host-defined policy marker or resolver type.</param>
    /// <returns>The same builder so calls can be chained.</returns>
    public static TBuilder RequireGovernancePolicy<TBuilder>(this TBuilder builder, Type policyType)
        where TBuilder : IEndpointConventionBuilder
    {
        ArgumentNullException.ThrowIfNull(policyType);
        return AddEndpointMetadata(builder, new RequireGovernancePolicyAttribute(policyType));
    }

    /// <summary>
    /// Adds liability-handshake metadata to an endpoint.
    /// </summary>
    /// <typeparam name="TBuilder">The endpoint convention builder type.</typeparam>
    /// <param name="builder">The endpoint convention builder.</param>
    /// <returns>The same builder so calls can be chained.</returns>
    public static TBuilder RequireLiabilityHandshake<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder
    {
        return AddEndpointMetadata(builder, new RequireLiabilityHandshakeAttribute());
    }

    /// <summary>
    /// Adds a required capability-grant scope to an endpoint.
    /// </summary>
    /// <typeparam name="TBuilder">The endpoint convention builder type.</typeparam>
    /// <param name="builder">The endpoint convention builder.</param>
    /// <param name="scope">The required capability-grant scope.</param>
    /// <returns>The same builder so calls can be chained.</returns>
    public static TBuilder RequireCapabilityGrant<TBuilder>(this TBuilder builder, string scope)
        where TBuilder : IEndpointConventionBuilder
    {
        return AddEndpointMetadata(builder, new RequireCapabilityGrantAttribute(scope));
    }

    /// <summary>
    /// Adds governance-audit emission metadata to an endpoint.
    /// </summary>
    /// <typeparam name="TBuilder">The endpoint convention builder type.</typeparam>
    /// <param name="builder">The endpoint convention builder.</param>
    /// <returns>The same builder so calls can be chained.</returns>
    public static TBuilder EmitGovernanceAudit<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder
    {
        return AddEndpointMetadata(builder, new EmitGovernanceAuditAttribute());
    }

    private static TBuilder AddEndpointMetadata<TBuilder>(TBuilder builder, object metadata)
        where TBuilder : IEndpointConventionBuilder
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(metadata);

        builder.Add(endpointBuilder => endpointBuilder.Metadata.Add(metadata));
        return builder;
    }
}
