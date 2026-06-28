using AsiBackbone.Core.Constraints;
using AsiBackbone.Core.Decisions;
using AsiBackbone.Core.Results;

namespace AsiBackbone.Core.Evaluation;

/// <summary>
/// Default policy evaluator that runs the active constraint structure and composes the result into a governance decision.
/// </summary>
/// <typeparam name="TContext">The framework-neutral evaluation context type.</typeparam>
public sealed class DefaultAsiBackbonePolicyEvaluator<TContext> : IAsiBackbonePolicyEvaluator<TContext>
    where TContext : IAsiBackboneConstraintEvaluationContext
{
    private readonly IAsiBackboneConstraint<TContext>[] constraints;
    private readonly IAsiBackboneDecisionPolicy<TContext>? decisionPolicy;
    private readonly AsiBackbonePolicyEvaluatorOptions options;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultAsiBackbonePolicyEvaluator{TContext}"/> class.
    /// </summary>
    /// <param name="constraints">The constraints that make up the active policy structure.</param>
    /// <param name="decisionPolicy">Optional decision policy applied after constraint composition.</param>
    public DefaultAsiBackbonePolicyEvaluator(
        IEnumerable<IAsiBackboneConstraint<TContext>> constraints,
        IAsiBackboneDecisionPolicy<TContext>? decisionPolicy = null)
        : this(constraints, decisionPolicy, options: null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultAsiBackbonePolicyEvaluator{TContext}"/> class.
    /// </summary>
    /// <param name="constraints">The constraints that make up the active policy structure.</param>
    /// <param name="decisionPolicy">Optional decision policy applied after constraint composition.</param>
    /// <param name="options">Evaluator options applied during constraint composition.</param>
    public DefaultAsiBackbonePolicyEvaluator(
        IEnumerable<IAsiBackboneConstraint<TContext>> constraints,
        IAsiBackboneDecisionPolicy<TContext>? decisionPolicy,
        AsiBackbonePolicyEvaluatorOptions? options)
    {
        ArgumentNullException.ThrowIfNull(constraints);

        // Keep an exact-sized private snapshot rather than wrapping a caller-owned list.
        // This avoids a per-evaluator ReadOnlyCollection<T> wrapper and prevents later caller mutations
        // from changing the evaluator's deterministic constraint order or behavior.
        this.constraints = [.. constraints];
        this.decisionPolicy = decisionPolicy;
        this.options = options ?? new AsiBackbonePolicyEvaluatorOptions();
        this.options.Validate();
    }

    /// <inheritdoc />
    public async ValueTask<GovernanceDecision> EvaluateAsync(
        TContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();

        if (constraints.Length == 0 && options.DenyWhenNoConstraints)
        {
            var noConstraintsDecision = GovernanceDecision.Deny(
                options.NoConstraintsReasonCode,
                options.NoConstraintsReasonMessage,
                correlationId: context.CorrelationId,
                policyVersion: context.PolicyVersion,
                policyHash: context.PolicyHash);

            return decisionPolicy is null
                ? noConstraintsDecision
                : await decisionPolicy
                .ApplyAsync(
                    context,
                    noConstraintsDecision,
                    Array.AsReadOnly(Array.Empty<ConstraintEvaluationResult>()),
                    cancellationToken)
                .ConfigureAwait(false);
        }

        List<ConstraintEvaluationResult> results = new(constraints.Length);
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

                if (options.ShortCircuitOnFirstDenial)
                {
                    break;
                }
            }
            else if (result.IsWarning)
            {
                warnings.AddRange(result.Reasons);
            }
        }

        GovernanceDecision composedDecision = Compose(context, denials, warnings);

        return decisionPolicy is null
            ? composedDecision
            : await decisionPolicy
            .ApplyAsync(context, composedDecision, Array.AsReadOnly([.. results]), cancellationToken)
            .ConfigureAwait(false);
    }

    private static GovernanceDecision Compose(
        TContext context,
        List<OperationReason> denials,
        List<OperationReason> warnings)
    {
        return denials.Count > 0
            ? GovernanceDecision.Deny(
                denials,
                correlationId: context.CorrelationId,
                policyVersion: context.PolicyVersion,
                policyHash: context.PolicyHash)
            : warnings.Count > 0
            ? GovernanceDecision.Warning(
                warnings,
                correlationId: context.CorrelationId,
                policyVersion: context.PolicyVersion,
                policyHash: context.PolicyHash)
            : GovernanceDecision.Allow(
            correlationId: context.CorrelationId,
            policyVersion: context.PolicyVersion,
            policyHash: context.PolicyHash);
    }
}
