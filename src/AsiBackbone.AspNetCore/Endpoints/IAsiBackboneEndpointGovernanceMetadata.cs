namespace AsiBackbone.AspNetCore.Endpoints;

/// <summary>
/// Marker interface for ASP.NET Core endpoint metadata that participates in AsiBackbone endpoint governance.
/// </summary>
public interface IAsiBackboneEndpointGovernanceMetadata
{
}

/// <summary>
/// Describes a host-defined governance policy marker attached to an ASP.NET Core endpoint.
/// </summary>
public interface IAsiBackboneEndpointGovernancePolicyMetadata : IAsiBackboneEndpointGovernanceMetadata
{
    /// <summary>
    /// Gets the host-defined policy marker or resolver type associated with the endpoint.
    /// </summary>
    Type PolicyType { get; }
}

/// <summary>
/// Describes endpoint-scoped policy evaluation options attached to an ASP.NET Core endpoint.
/// </summary>
public interface IAsiBackboneEndpointPolicyEvaluationOptionsMetadata : IAsiBackboneEndpointGovernanceMetadata
{
    /// <summary>
    /// Gets an endpoint-level override for latency-optimized first-denial short-circuit behavior, when supplied.
    /// </summary>
    bool? ShortCircuitOnFirstDenial { get; }
}

/// <summary>
/// Describes whether an ASP.NET Core endpoint expects an acknowledgment challenge path when policy requires it.
/// </summary>
public interface IAsiBackboneEndpointLiabilityHandshakeMetadata : IAsiBackboneEndpointGovernanceMetadata
{
    /// <summary>
    /// Gets a value indicating whether the endpoint requires liability-handshake support.
    /// </summary>
    bool RequiresLiabilityHandshake { get; }
}

/// <summary>
/// Describes a required capability-grant scope for an ASP.NET Core endpoint.
/// </summary>
public interface IAsiBackboneEndpointCapabilityGrantMetadata : IAsiBackboneEndpointGovernanceMetadata
{
    /// <summary>
    /// Gets the required capability-grant scope.
    /// </summary>
    string Scope { get; }
}

/// <summary>
/// Describes whether governance decisions for an ASP.NET Core endpoint should be emitted to the host-owned audit path.
/// </summary>
public interface IAsiBackboneEndpointAuditEmissionMetadata : IAsiBackboneEndpointGovernanceMetadata
{
    /// <summary>
    /// Gets a value indicating whether governance audit emission is requested.
    /// </summary>
    bool EmitGovernanceAudit { get; }
}
