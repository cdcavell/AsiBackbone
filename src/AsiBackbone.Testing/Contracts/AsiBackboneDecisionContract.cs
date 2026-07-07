using AsiBackbone.Core.Audit;
using AsiBackbone.Core.Constraints;
using AsiBackbone.Core.Decisions;
using AsiBackbone.Core.Results;

namespace AsiBackbone.Testing.Contracts;

/// <summary>
/// Provides reusable safe-collapse assertions for governance decisions and audit residue.
/// </summary>
public static class AsiBackboneDecisionContract
{
    /// <summary>
    /// Verifies that a governance decision is present and carries the minimum shape required for safe downstream handling.
    /// </summary>
    /// <param name="decision">The decision returned by an implementation under test.</param>
    /// <param name="contractName">The human-readable contract name used in failure messages.</param>
    /// <returns>The verified decision.</returns>
    public static GovernanceDecision VerifySafeDecision(
        GovernanceDecision? decision,
        string contractName = "Governance decision")
    {
        if (decision is null)
        {
            throw new AsiBackboneContractViolationException($"{contractName} must return a decision and must never return null.");
        }

        VerifySupportedOutcome(decision, contractName);

        if (RequiresReasonCodes(decision) && decision.Reasons.Count == 0)
        {
            throw new AsiBackboneContractViolationException($"{contractName} outcome '{decision.Outcome}' must include at least one reason code.");
        }

        for (int index = 0; index < decision.Reasons.Count; index++)
        {
            OperationReason reason = decision.Reasons[index] ?? throw new AsiBackboneContractViolationException($"{contractName} contains a null reason at index {index}.");

            if (string.IsNullOrWhiteSpace(reason.Code))
            {
                throw new AsiBackboneContractViolationException($"{contractName} contains a reason with an empty code at index {index}.");
            }

            if (string.IsNullOrWhiteSpace(reason.Message))
            {
                throw new AsiBackboneContractViolationException($"{contractName} contains a reason with an empty message at index {index}.");
            }
        }

        return decision;
    }

    /// <summary>
    /// Verifies decision shape and checks that telemetry supplied by the evaluation context is preserved when present.
    /// </summary>
    /// <typeparam name="TContext">The framework-neutral evaluation context type.</typeparam>
    /// <param name="decision">The decision returned by an implementation under test.</param>
    /// <param name="context">The context supplied to the implementation under test.</param>
    /// <param name="contractName">The human-readable contract name used in failure messages.</param>
    /// <returns>The verified decision.</returns>
    public static GovernanceDecision VerifyTelemetryFromContext<TContext>(
        GovernanceDecision? decision,
        TContext context,
        string contractName = "Governance decision")
        where TContext : IAsiBackboneConstraintEvaluationContext
    {
        ArgumentNullException.ThrowIfNull(context);

        GovernanceDecision verifiedDecision = VerifySafeDecision(decision, contractName);
        VerifyTelemetryValue(context.CorrelationId, verifiedDecision.CorrelationId, "correlation ID", contractName);
        VerifyTelemetryValue(context.PolicyVersion, verifiedDecision.PolicyVersion, "policy version", contractName);
        VerifyTelemetryValue(context.PolicyHash, verifiedDecision.PolicyHash, "policy hash", contractName);
        return verifiedDecision;
    }

    /// <summary>
    /// Verifies that a known invalid capability-grant scenario does not produce an allow decision.
    /// </summary>
    /// <param name="decision">The decision returned by the capability validator for a known invalid scenario.</param>
    /// <param name="contractName">The human-readable contract name used in failure messages.</param>
    /// <returns>The verified decision.</returns>
    public static GovernanceDecision VerifyInvalidCapabilityGrantDoesNotAllow(
        GovernanceDecision? decision,
        string contractName = "Invalid capability grant")
    {
        GovernanceDecision verifiedDecision = VerifySafeDecision(decision, contractName);

        return verifiedDecision.IsAllowed
            ? throw new AsiBackboneContractViolationException($"{contractName} must not return Allow for an invalid or unsupported capability grant.")
            : verifiedDecision;
    }

    /// <summary>
    /// Verifies that audit residue contains the minimum identity, operation, outcome, and policy telemetry shape.
    /// </summary>
    /// <param name="residue">The audit residue to verify.</param>
    /// <param name="contractName">The human-readable contract name used in failure messages.</param>
    /// <returns>The verified audit residue.</returns>
    public static IAsiBackboneAuditResidue VerifyAuditResidue(
        IAsiBackboneAuditResidue? residue,
        string contractName = "Audit residue")
    {
        if (residue is null)
        {
            throw new AsiBackboneContractViolationException($"{contractName} must not be null.");
        }

        VerifyRequiredString(residue.EventId, "event ID", contractName);
        VerifyRequiredString(residue.ActorId, "actor ID", contractName);
        VerifyRequiredString(residue.OperationName, "operation name", contractName);
        VerifyRequiredString(residue.Outcome, "outcome", contractName);

        if (residue.ReasonCodes is null)
        {
            throw new AsiBackboneContractViolationException($"{contractName} reason-code collection must not be null.");
        }

        for (int index = 0; index < residue.ReasonCodes.Count; index++)
        {
            if (string.IsNullOrWhiteSpace(residue.ReasonCodes[index]))
            {
                throw new AsiBackboneContractViolationException($"{contractName} contains an empty reason code at index {index}.");
            }
        }

        return residue.Metadata is null
            ? throw new AsiBackboneContractViolationException($"{contractName} metadata collection must not be null.")
            : residue;
    }

    private static void VerifySupportedOutcome(GovernanceDecision decision, string contractName)
    {
        if (decision.Outcome is not GovernanceDecisionOutcome.Allowed
            and not GovernanceDecisionOutcome.Warning
            and not GovernanceDecisionOutcome.Denied
            and not GovernanceDecisionOutcome.Deferred
            and not GovernanceDecisionOutcome.AcknowledgmentRequired
            and not GovernanceDecisionOutcome.EscalationRecommended)
        {
            throw new AsiBackboneContractViolationException($"{contractName} returned unsupported outcome '{decision.Outcome}'.");
        }
    }

    private static bool RequiresReasonCodes(GovernanceDecision decision)
    {
        return decision.Outcome is GovernanceDecisionOutcome.Warning
            or GovernanceDecisionOutcome.Denied
            or GovernanceDecisionOutcome.Deferred
            or GovernanceDecisionOutcome.AcknowledgmentRequired
            or GovernanceDecisionOutcome.EscalationRecommended;
    }

    private static void VerifyTelemetryValue(
        string? expected,
        string? actual,
        string telemetryName,
        string contractName)
    {
        if (!string.IsNullOrWhiteSpace(expected) && !string.Equals(expected, actual, StringComparison.Ordinal))
        {
            throw new AsiBackboneContractViolationException($"{contractName} must preserve the supplied {telemetryName} when present.");
        }
    }

    private static void VerifyRequiredString(string? value, string name, string contractName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new AsiBackboneContractViolationException($"{contractName} must include a non-empty {name}.");
        }
    }
}
