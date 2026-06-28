using System.Collections.ObjectModel;
using Microsoft.AspNetCore.Http;

namespace AsiBackbone.AspNetCore.Endpoints;

/// <summary>
/// Represents normalized AsiBackbone governance metadata resolved from an ASP.NET Core endpoint.
/// </summary>
public sealed class AsiBackboneEndpointGovernanceDescriptor
{
    private static readonly ReadOnlyCollection<Type> EmptyPolicyTypes = Array.AsReadOnly(Array.Empty<Type>());
    private static readonly ReadOnlyCollection<string> EmptyScopes = Array.AsReadOnly(Array.Empty<string>());

    private AsiBackboneEndpointGovernanceDescriptor(
        string operationName,
        IReadOnlyList<Type> policyTypes,
        bool? shortCircuitOnFirstDenial,
        bool requiresLiabilityHandshake,
        IReadOnlyList<string> capabilityScopes,
        bool emitGovernanceAudit)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operationName);

        OperationName = operationName.Trim();
        PolicyTypes = policyTypes;
        ShortCircuitOnFirstDenial = shortCircuitOnFirstDenial;
        RequiresLiabilityHandshake = requiresLiabilityHandshake;
        CapabilityScopes = capabilityScopes;
        EmitGovernanceAudit = emitGovernanceAudit;
    }

    /// <summary>
    /// Gets the operation name used for audit residue and acknowledgment challenge construction.
    /// </summary>
    public string OperationName { get; }

    /// <summary>
    /// Gets the policy marker or resolver types attached to the endpoint.
    /// </summary>
    public IReadOnlyList<Type> PolicyTypes { get; }

    /// <summary>
    /// Gets an endpoint-scoped first-denial short-circuit preference, when endpoint metadata supplied one.
    /// </summary>
    public bool? ShortCircuitOnFirstDenial { get; }

    /// <summary>
    /// Gets a value indicating whether liability-handshake support is requested.
    /// </summary>
    public bool RequiresLiabilityHandshake { get; }

    /// <summary>
    /// Gets the required capability-grant scopes attached to the endpoint.
    /// </summary>
    public IReadOnlyList<string> CapabilityScopes { get; }

    /// <summary>
    /// Gets a value indicating whether governance audit emission is requested.
    /// </summary>
    public bool EmitGovernanceAudit { get; }

    /// <summary>
    /// Gets a value indicating whether the endpoint contains any AsiBackbone governance metadata.
    /// </summary>
    public bool HasGovernanceMetadata => PolicyTypes.Count > 0
        || ShortCircuitOnFirstDenial.HasValue
        || RequiresLiabilityHandshake
        || CapabilityScopes.Count > 0
        || EmitGovernanceAudit;

    /// <summary>
    /// Creates a descriptor from the selected ASP.NET Core endpoint.
    /// </summary>
    /// <param name="endpoint">The selected endpoint.</param>
    /// <returns>A normalized descriptor.</returns>
    public static AsiBackboneEndpointGovernanceDescriptor FromEndpoint(Endpoint? endpoint)
    {
        if (endpoint is null)
        {
            return None("unresolved-endpoint");
        }

        Type[] policyTypes = [.. endpoint.Metadata
            .GetOrderedMetadata<IAsiBackboneEndpointGovernancePolicyMetadata>()
            .Select(metadata => metadata.PolicyType)
            .Where(static policyType => policyType is not null)
            .Distinct()];

        string[] capabilityScopes = [.. endpoint.Metadata
            .GetOrderedMetadata<IAsiBackboneEndpointCapabilityGrantMetadata>()
            .Select(metadata => metadata.Scope)
            .Where(static scope => !string.IsNullOrWhiteSpace(scope))
            .Select(static scope => scope.Trim())
            .Distinct(StringComparer.Ordinal)];

        bool? shortCircuitOnFirstDenial = endpoint.Metadata
            .GetOrderedMetadata<IAsiBackboneEndpointPolicyEvaluationOptionsMetadata>()
            .Select(static metadata => metadata.ShortCircuitOnFirstDenial)
            .LastOrDefault();

        bool requiresLiabilityHandshake = endpoint.Metadata
            .GetOrderedMetadata<IAsiBackboneEndpointLiabilityHandshakeMetadata>()
            .Any(static metadata => metadata.RequiresLiabilityHandshake);

        bool emitGovernanceAudit = endpoint.Metadata
            .GetOrderedMetadata<IAsiBackboneEndpointAuditEmissionMetadata>()
            .Any(static metadata => metadata.EmitGovernanceAudit);

        return new AsiBackboneEndpointGovernanceDescriptor(
            ResolveOperationName(endpoint),
            policyTypes.Length == 0 ? EmptyPolicyTypes : Array.AsReadOnly(policyTypes),
            shortCircuitOnFirstDenial,
            requiresLiabilityHandshake,
            capabilityScopes.Length == 0 ? EmptyScopes : Array.AsReadOnly(capabilityScopes),
            emitGovernanceAudit);
    }

    /// <summary>
    /// Creates a descriptor that does not request governance handling.
    /// </summary>
    /// <param name="operationName">The operation name to associate with the descriptor.</param>
    /// <returns>A descriptor with no governance metadata.</returns>
    public static AsiBackboneEndpointGovernanceDescriptor None(string operationName)
    {
        return new AsiBackboneEndpointGovernanceDescriptor(
            operationName,
            EmptyPolicyTypes,
            shortCircuitOnFirstDenial: null,
            requiresLiabilityHandshake: false,
            EmptyScopes,
            emitGovernanceAudit: false);
    }

    /// <summary>
    /// Converts descriptor values into safe metadata for framework-neutral governance evaluation and audit residue.
    /// </summary>
    /// <returns>A normalized metadata dictionary.</returns>
    public IReadOnlyDictionary<string, string> ToMetadata()
    {
        Dictionary<string, string> metadata = new(StringComparer.Ordinal)
        {
            ["endpoint.operation_name"] = OperationName,
            ["endpoint.requires_liability_handshake"] = RequiresLiabilityHandshake ? "true" : "false",
            ["endpoint.emit_governance_audit"] = EmitGovernanceAudit ? "true" : "false"
        };

        if (PolicyTypes.Count > 0)
        {
            metadata["endpoint.policy_types"] = string.Join(",", PolicyTypes.Select(static policyType => policyType.FullName ?? policyType.Name));
        }

        if (ShortCircuitOnFirstDenial.HasValue)
        {
            metadata["endpoint.short_circuit_on_first_denial"] = ShortCircuitOnFirstDenial.Value ? "true" : "false";
        }

        if (CapabilityScopes.Count > 0)
        {
            metadata["endpoint.capability_scopes"] = string.Join(",", CapabilityScopes);
        }

        return metadata;
    }

    private static string ResolveOperationName(Endpoint endpoint)
    {
        return string.IsNullOrWhiteSpace(endpoint.DisplayName)
            ? "aspnetcore.endpoint"
            : endpoint.DisplayName.Trim();
    }
}
