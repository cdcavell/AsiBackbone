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
    private static readonly IReadOnlyList<ConstraintEvaluationResult> EmptyConstraintResults =
        Array.AsReadOnly(Array.Empty<ConstraintEvaluationResult>());

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

        if (constraints.Length == 0)
        {
            GovernanceDecision noConstraintDecision = options.DenyWhenNoConstraints
                ? GovernanceDecision.Deny(
                    options.NoConstraintsReasonCode,
                    options.NoConstraintsReasonMessage,
                    correlationId: context.CorrelationId,
                    policyVersion: context.PolicyVersion,
                    policyHash: context.PolicyHash)
                : GovernanceDecision.Allow(
                    correlationId: context.CorrelationId,
                    policyVersion: context.PolicyVersion,
                    policyHash: context.PolicyHash);

            return decisionPolicy is null
                ? noConstraintDecision
                : await decisionPolicy
                .ApplyAsync(
                    context,
                    noConstraintDecision,
                    EmptyConstraintResults,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        List<ConstraintEvaluationResult>? results = decisionPolicy is null
            ? null
            : new List<ConstraintEvaluationResult>(constraints.Length);
        var denials = new OperationReasonAccumulator();
        var warnings = new OperationReasonAccumulator();

        foreach (IAsiBackboneConstraint<TContext> constraint in constraints)
        {
            cancellationToken.ThrowIfCancellationRequested();

            ConstraintEvaluationResult result = await constraint
                .EvaluateAsync(context, cancellationToken)
                .ConfigureAwait(false);

            results?.Add(result);

            if (result.IsDenied)
            {
                denials.AddRange(result.Reasons);

                if (!options.ShortCircuitOnFirstDenial)
                {
                    warnings = default;
                }

                if (options.ShortCircuitOnFirstDenial)
                {
                    break;
                }
            }
            else if (result.IsWarning && denials.Count == 0)
            {
                warnings.AddRange(result.Reasons);
            }
        }

        GovernanceDecision composedDecision = Compose(
            context,
            denials,
            warnings,
            includeWarningsWhenDenied: options.ShortCircuitOnFirstDenial);

        return decisionPolicy is null
            ? composedDecision
            : await decisionPolicy
            .ApplyAsync(context, composedDecision, CreateConstraintResultsView(results), cancellationToken)
            .ConfigureAwait(false);
    }

    private static GovernanceDecision Compose(
        TContext context,
        OperationReasonAccumulator denials,
        OperationReasonAccumulator warnings,
        bool includeWarningsWhenDenied)
    {
        if (denials.Count > 0)
        {
            return includeWarningsWhenDenied && warnings.Count > 0
                ? GovernanceDecision.Deny(
                    warnings.Concat(denials),
                    correlationId: context.CorrelationId,
                    policyVersion: context.PolicyVersion,
                    policyHash: context.PolicyHash)
                : CreateDeniedDecision(context, denials);
        }

        return warnings.Count > 0
            ? CreateWarningDecision(context, warnings)
            : GovernanceDecision.Allow(
            correlationId: context.CorrelationId,
            policyVersion: context.PolicyVersion,
            policyHash: context.PolicyHash);
    }

    private static GovernanceDecision CreateDeniedDecision(
        TContext context,
        OperationReasonAccumulator denials)
    {
        return denials.Count == 1
            ? GovernanceDecision.Deny(
                denials.FirstReason!,
                correlationId: context.CorrelationId,
                policyVersion: context.PolicyVersion,
                policyHash: context.PolicyHash)
            : GovernanceDecision.Deny(
                denials.AsReadOnlyList(),
                correlationId: context.CorrelationId,
                policyVersion: context.PolicyVersion,
                policyHash: context.PolicyHash);
    }

    private static GovernanceDecision CreateWarningDecision(
        TContext context,
        OperationReasonAccumulator warnings)
    {
        return warnings.Count == 1
            ? GovernanceDecision.Warning(
                warnings.FirstReason!,
                correlationId: context.CorrelationId,
                policyVersion: context.PolicyVersion,
                policyHash: context.PolicyHash)
            : GovernanceDecision.Warning(
                warnings.AsReadOnlyList(),
                correlationId: context.CorrelationId,
                policyVersion: context.PolicyVersion,
                policyHash: context.PolicyHash);
    }

    private static IReadOnlyList<ConstraintEvaluationResult> CreateConstraintResultsView(
        List<ConstraintEvaluationResult>? results)
    {
        return results is null || results.Count == 0
            ? EmptyConstraintResults
            : results.AsReadOnly();
    }

    private struct OperationReasonAccumulator
    {
        private List<OperationReason>? additionalReasons;

        public OperationReason? FirstReason { get; private set; }

        public int Count { get; private set; }

        public void AddRange(IReadOnlyList<OperationReason> reasons)
        {
            for (int index = 0; index < reasons.Count; index++)
            {
                Add(reasons[index]);
            }
        }

        public readonly IEnumerable<OperationReason> Concat(OperationReasonAccumulator other)
        {
            foreach (OperationReason reason in AsEnumerable())
            {
                yield return reason;
            }

            foreach (OperationReason reason in other.AsEnumerable())
            {
                yield return reason;
            }
        }

        public readonly IReadOnlyList<OperationReason> AsReadOnlyList()
        {
            return Count switch
            {
                0 => Array.AsReadOnly(Array.Empty<OperationReason>()),
                1 => Array.AsReadOnly([FirstReason!]),
                _ => additionalReasons!.AsReadOnly()
            };
        }

        private void Add(OperationReason? reason)
        {
            if (reason is null)
            {
                return;
            }

            if (Count == 0)
            {
                FirstReason = reason;
                Count = 1;
                return;
            }

            additionalReasons ??= [FirstReason!];
            additionalReasons.Add(reason);
            Count++;
        }

        private readonly IEnumerable<OperationReason> AsEnumerable()
        {
            if (Count == 0)
            {
                yield break;
            }

            if (Count == 1)
            {
                yield return FirstReason!;
                yield break;
            }

            foreach (OperationReason reason in additionalReasons!)
            {
                yield return reason;
            }
        }
    }
}
