using AsiBackbone.AspNetCore.Endpoints;
using AsiBackbone.Core.Decisions;

namespace Company.AsibackboneTemplate.Governance;

/// <summary>
/// Sample endpoint capability validator. Production hosts should replace this with host-owned scope, expiry, replay, actor, and downstream authorization checks.
/// </summary>
public sealed class SampleCapabilityGrantValidator : IAsiBackboneEndpointCapabilityGrantValidator
{
    public ValueTask<GovernanceDecision> ValidateAsync(
        HttpContext httpContext,
        AsiBackboneEndpointGovernanceDescriptor descriptor,
        GovernanceDecision currentDecision,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentNullException.ThrowIfNull(currentDecision);

        return ValueTask.FromResult(descriptor.CapabilityScopes.Contains("sample.execute", StringComparer.Ordinal)
            ? currentDecision
            : GovernanceDecision.Deny(
                "template.capability.missing",
                "The sample endpoint capability grant validator did not find the required scope.",
                correlationId: currentDecision.CorrelationId,
                traceId: currentDecision.TraceId,
                policyVersion: currentDecision.PolicyVersion,
                policyHash: currentDecision.PolicyHash));
    }
}
