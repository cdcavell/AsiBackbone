using CDCavell.ASIBackbone.Core.Constraints;
using CDCavell.ASIBackbone.Core.Decisions;
using CDCavell.ASIBackbone.Core.Results;

namespace CDCavell.ASIBackbone.Core.Evaluation;

/// <summary>
/// Default policy evaluator that runs the active constraint structure and composes the result into a governance decision.
/// </summary>
/// <typeparam name="TContext">The framework-neutral evaluation context type.</typeparam>
public sealed class DefaultAsiBackbonePolicyEvaluator<TContext> : IAsiBackbonePolicyEvaluator<TContext>
    where TContext : IAsiBackboneConstraintEvaluationContext
{
    private readonly IReadOnlyList<IAsiBackboneConstraint<TContext>> constraints;
    private readonly IAsiBackboneDecisionPolicy<TContext>? decisionPolicy;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultAsiBackbonePolicyEvaluator{TContext}"/> class.
    /// </summary>
    /// <param name="constraints">The constraints that make up the active policy structure.</param>
    /// <param name="decisionPolicy">Optional decision policy applied after constraint composition.</param>
    public DefaultAsiBackbonePolicyEvaluator(
        IEnumerable<IAsiBackboneConstraint<TContext>> constraints,
        IAsiBackboneDecisionPolicy<TContext>? decisionPolicy = null)
    {
        ArgumentNullException.ThrowIfNull(constraints);

        this.constraints = Array.AsReadOnly([.. constraints]);
        this.decisionPolicy = decisionPolicy;
    }

    /// <inheritdoc />
    public async ValueTask<GovernanceDecision> EvaluateAsync(
        TContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        List<ConstraintEvaluationResult> results = new(constraints.Count);
        List<OperationReason> denials = [];
        List<OperationReason> warnings = [];

        foreach (IAsiBackboneConstraint<TContext> constraint in constraints)
        {
            cancellationToken.ThrowIfCancellationRequested();

            ConstraintEvaluationResult result = await constraint
                .EvaluateAsync(context, cancellationToken)
                .ConfigureAwait(false);

            results.Add(result);

            if (result.IsDenied)
            {
                denials.AddRange(result.Reasons);
            }
            else if (result.IsWarning)
            {
                warnings.AddRange(result.Reasons);
            }
        }

        GovernanceDecision composedDecision = Compose(context, denials, warnings);

        if (decisionPolicy is null)
        {
            return composedDecision;
        }

        return await decisionPolicy
            .ApplyAsync(context, composedDecision, Array.AsReadOnly([.. results]), cancellationToken)
            .ConfigureAwait(false);
    }

    private static GovernanceDecision Compose(
        TContext context,
        IReadOnlyList<OperationReason> denials,
        IReadOnlyList<OperationReason> warnings)
    {
        if (denials.Count > 0)
        {
            return GovernanceDecision.Deny(
                denials,
                correlationId: context.CorrelationId,
                policyVersion: context.PolicyVersion,
                policyHash: context.PolicyHash);
        }

        if (warnings.Count > 0)
        {
            return GovernanceDecision.Warning(
                warnings,
                correlationId: context.CorrelationId,
                policyVersion: context.PolicyVersion,
                policyHash: context.PolicyHash);
        }

        return GovernanceDecision.Allow(
            correlationId: context.CorrelationId,
            policyVersion: context.PolicyVersion,
            policyHash: context.PolicyHash);
    }
}
