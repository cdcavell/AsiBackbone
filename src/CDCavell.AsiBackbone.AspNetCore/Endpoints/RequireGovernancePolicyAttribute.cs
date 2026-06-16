namespace CDCavell.AsiBackbone.AspNetCore.Endpoints;

/// <summary>
/// Marks an ASP.NET Core endpoint as requiring host-defined AsiBackbone governance policy evaluation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="RequireGovernancePolicyAttribute" /> class.
/// </remarks>
/// <param name="policyType">The host-defined policy marker or resolver type associated with the endpoint.</param>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public sealed class RequireGovernancePolicyAttribute(Type policyType) : Attribute, IAsiBackboneEndpointGovernancePolicyMetadata
{

    /// <inheritdoc />
    public Type PolicyType { get; } = policyType ?? throw new ArgumentNullException(nameof(policyType));
}

/// <summary>
/// Marks an ASP.NET Core endpoint as requiring liability-handshake support when a governance decision requires acknowledgment.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class RequireLiabilityHandshakeAttribute : Attribute, IAsiBackboneEndpointLiabilityHandshakeMetadata
{
    /// <inheritdoc />
    public bool RequiresLiabilityHandshake => true;
}

/// <summary>
/// Marks an ASP.NET Core endpoint as requiring a host-validated capability grant before execution.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public sealed class RequireCapabilityGrantAttribute : Attribute, IAsiBackboneEndpointCapabilityGrantMetadata
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RequireCapabilityGrantAttribute" /> class.
    /// </summary>
    /// <param name="scope">The required capability-grant scope.</param>
    public RequireCapabilityGrantAttribute(string scope)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(scope);
        Scope = scope.Trim();
    }

    /// <inheritdoc />
    public string Scope { get; }
}

/// <summary>
/// Marks an ASP.NET Core endpoint as requesting governance audit emission through the host-owned audit path.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class EmitGovernanceAuditAttribute : Attribute, IAsiBackboneEndpointAuditEmissionMetadata
{
    /// <inheritdoc />
    public bool EmitGovernanceAudit => true;
}
