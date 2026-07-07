using AsiBackbone.Core.Constraints;
using AsiBackbone.Core.Decisions;
using AsiBackbone.Core.Evaluation;

namespace AsiBackbone.Testing.Contracts;

/// <summary>
/// Reusable contract fixture for <see cref="IAsiBackboneDecisionPolicy{TContext}" /> implementations.
/// </summary>
/// <typeparam name="TContext">The framework-neutral evaluation context type.</typeparam>
public abstract class AsiBackboneDecisionPolicyContract<TContext>
    where TContext : IAsiBackboneConstraintEvaluationContext
{
    /// <summary>
    /// Creates the decision-policy implementation under test.
    /// </summary>
    /// <returns>The decision-policy implementation to validate.</returns>
    protected abstract IAsiBackboneDecisionPolicy<TContext> CreateDecisionPolicy();

    /// <summary>
    /// Creates the context supplied to the decision-policy implementation under test.
    /// </summary>
    /// <returns>The evaluation context to validate with.</returns>
    protected abstract TContext CreateEvaluationContext();

    /// <summary>
    /// Creates the composed decision supplied to the decision-policy implementation under test.
    /// </summary>
    /// <param name="context">The evaluation context created for the contract run.</param>
    /// <returns>The composed decision supplied to the decision policy.</returns>
    protected virtual GovernanceDecision CreateComposedDecision(TContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return GovernanceDecision.Allow(
            context.CorrelationId,
            traceId: null,
            context.PolicyVersion,
            context.PolicyHash);
    }

    /// <summary>
    /// Creates the constraint-result collection supplied to the decision-policy implementation under test.
    /// </summary>
    /// <returns>The constraint-result collection supplied to the decision policy.</returns>
    protected virtual IReadOnlyList<ConstraintEvaluationResult> CreateConstraintResults()
    {
        return Array.Empty<ConstraintEvaluationResult>();
    }

    /// <summary>
    /// Verifies that the decision policy returns a safe, non-null decision and preserves supplied decision telemetry.
    /// </summary>
    /// <param name="cancellationToken">A token that can cancel the contract validation.</param>
    /// <returns>The verified governance decision.</returns>
    public async ValueTask<GovernanceDecision> VerifyDecisionPolicyReturnsSafeDecisionAsync(CancellationToken cancellationToken = default)
    {
        IAsiBackboneDecisionPolicy<TContext> decisionPolicy = CreateDecisionPolicy()
            ?? throw new AsiBackboneContractViolationException("Decision-policy contract must provide a decision policy instance.");
        TContext context = CreateEvaluationContext()
            ?? throw new AsiBackboneContractViolationException("Decision-policy contract must provide an evaluation context.");
        GovernanceDecision composedDecision = CreateComposedDecision(context)
            ?? throw new AsiBackboneContractViolationException("Decision-policy contract must provide a composed decision.");
        IReadOnlyList<ConstraintEvaluationResult> constraintResults = CreateConstraintResults()
            ?? throw new AsiBackboneContractViolationException("Decision-policy contract must provide a constraint-result collection.");

        try
        {
            GovernanceDecision decision = await decisionPolicy.ApplyAsync(context, composedDecision, constraintResults, cancellationToken).ConfigureAwait(false);
            return AsiBackboneDecisionContract.VerifyTelemetryFromContext(decision, context, "Decision-policy decision");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (AsiBackboneContractViolationException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new AsiBackboneContractViolationException(
                "Decision-policy implementations must return safe decisions instead of bypassing the policy pipeline by throwing during normal contract validation.",
                exception);
        }
    }
}
