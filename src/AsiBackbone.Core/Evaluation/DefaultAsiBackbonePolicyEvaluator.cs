using System.Collections.ObjectModel;
using AsiBackbone.Core.Constraints;
using AsiBackbone.Core.Decisions;
using AsiBackbone.Core.Results;
using AsiBackbone.Core.ThreatModeling;
using Microsoft.Extensions.Logging;

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

    private static readonly Action<ILogger, string, string, string, Exception?> EmptyPolicyAllowedWarning =
        LoggerMessage.Define<string, string, string>(
            LogLevel.Warning,
            new EventId(4110, nameof(EmptyPolicyAllowedWarning)),
            "Policy evaluation ran with zero constraints while DenyWhenNoConstraints is false; default empty-policy behavior allows the decision. CorrelationId: {CorrelationId}; PolicyVersion: {PolicyVersion}; PolicyHash: {PolicyHash}");

    private static readonly Action<ILogger, string, string, string, string, Exception?> ConstraintExceptionDeniedError =
        LoggerMessage.Define<string, string, string, string>(
            LogLevel.Error,
            new EventId(4120, nameof(ConstraintExceptionDeniedError)),
            "Policy constraint '{ConstraintName}' threw during evaluation and was converted to a denied governance decision. CorrelationId: {CorrelationId}; PolicyVersion: {PolicyVersion}; PolicyHash: {PolicyHash}");

    private static readonly Action<ILogger, string, string, string, string, Exception?> ThreatContributorExceptionDeniedError =
        LoggerMessage.Define<string, string, string, string>(
            LogLevel.Error,
            new EventId(4130, nameof(ThreatContributorExceptionDeniedError)),
            "Threat model contributor '{ContributorName}' threw during evaluation and was converted to a denied governance decision. CorrelationId: {CorrelationId}; PolicyVersion: {PolicyVersion}; PolicyHash: {PolicyHash}");

    private readonly IAsiBackboneConstraint<TContext>[] constraints;
    private readonly IThreatModelContributor<TContext>[] threatModelContributors;
    private readonly IAsiBackboneDecisionPolicy<TContext>? decisionPolicy;
    private readonly ILogger<DefaultAsiBackbonePolicyEvaluator<TContext>>? logger;
    private readonly AsiBackbonePolicyEvaluatorOptions options;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultAsiBackbonePolicyEvaluator{TContext}"/> class.
    /// </summary>
    /// <param name="constraints">The constraints that make up the active policy structure.</param>
    /// <param name="decisionPolicy">Optional decision policy applied after constraint composition.</param>
    public DefaultAsiBackbonePolicyEvaluator(
        IEnumerable<IAsiBackboneConstraint<TContext>> constraints,
        IAsiBackboneDecisionPolicy<TContext>? decisionPolicy = null)
        : this(constraints, threatModelContributors: null, decisionPolicy, options: null, logger: null)
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
        : this(constraints, threatModelContributors: null, decisionPolicy, options, logger: null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultAsiBackbonePolicyEvaluator{TContext}"/> class.
    /// </summary>
    /// <param name="constraints">The constraints that make up the active policy structure.</param>
    /// <param name="decisionPolicy">Optional decision policy applied after constraint composition.</param>
    /// <param name="options">Evaluator options applied during constraint composition.</param>
    /// <param name="logger">Optional logger used to emit operational warning signals.</param>
    public DefaultAsiBackbonePolicyEvaluator(
        IEnumerable<IAsiBackboneConstraint<TContext>> constraints,
        IAsiBackboneDecisionPolicy<TContext>? decisionPolicy,
        AsiBackbonePolicyEvaluatorOptions? options,
        ILogger<DefaultAsiBackbonePolicyEvaluator<TContext>>? logger)
        : this(constraints, threatModelContributors: null, decisionPolicy, options, logger)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultAsiBackbonePolicyEvaluator{TContext}"/> class.
    /// </summary>
    /// <param name="constraints">The constraints that make up the active policy structure.</param>
    /// <param name="threatModelContributors">Threat model contributors that inspect the context before constraint composition.</param>
    /// <param name="decisionPolicy">Optional decision policy applied after composition.</param>
    public DefaultAsiBackbonePolicyEvaluator(
        IEnumerable<IAsiBackboneConstraint<TContext>> constraints,
        IEnumerable<IThreatModelContributor<TContext>> threatModelContributors,
        IAsiBackboneDecisionPolicy<TContext>? decisionPolicy = null)
        : this(constraints, threatModelContributors, decisionPolicy, options: null, logger: null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultAsiBackbonePolicyEvaluator{TContext}"/> class.
    /// </summary>
    /// <param name="constraints">The constraints that make up the active policy structure.</param>
    /// <param name="threatModelContributors">Threat model contributors that inspect the context before constraint composition.</param>
    /// <param name="decisionPolicy">Optional decision policy applied after composition.</param>
    /// <param name="options">Evaluator options applied during composition.</param>
    public DefaultAsiBackbonePolicyEvaluator(
        IEnumerable<IAsiBackboneConstraint<TContext>> constraints,
        IEnumerable<IThreatModelContributor<TContext>> threatModelContributors,
        IAsiBackboneDecisionPolicy<TContext>? decisionPolicy,
        AsiBackbonePolicyEvaluatorOptions? options)
        : this(constraints, threatModelContributors, decisionPolicy, options, logger: null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultAsiBackbonePolicyEvaluator{TContext}"/> class.
    /// </summary>
    /// <param name="constraints">The constraints that make up the active policy structure.</param>
    /// <param name="threatModelContributors">Threat model contributors that inspect the context before constraint composition.</param>
    /// <param name="decisionPolicy">Optional decision policy applied after composition.</param>
    /// <param name="options">Evaluator options applied during composition.</param>
    /// <param name="logger">Optional logger used to emit operational warning signals.</param>
    public DefaultAsiBackbonePolicyEvaluator(
        IEnumerable<IAsiBackboneConstraint<TContext>> constraints,
        IEnumerable<IThreatModelContributor<TContext>>? threatModelContributors,
        IAsiBackboneDecisionPolicy<TContext>? decisionPolicy,
        AsiBackbonePolicyEvaluatorOptions? options,
        ILogger<DefaultAsiBackbonePolicyEvaluator<TContext>>? logger)
    {
        ArgumentNullException.ThrowIfNull(constraints);

        // Keep exact-sized private snapshots rather than wrapping caller-owned lists.
        // This avoids per-evaluator ReadOnlyCollection<T> wrappers and prevents later caller mutations
        // from changing deterministic constraint/contributor order or behavior.
        this.constraints = [.. constraints];
        this.threatModelContributors = threatModelContributors is null ? [] : [.. threatModelContributors];
        this.decisionPolicy = decisionPolicy;
        this.logger = logger;
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

        ThreatEvaluationResult threatEvaluation = await EvaluateThreatModelContributorsAsync(
            context,
            cancellationToken)
            .ConfigureAwait(false);

        if (threatEvaluation.BlockingDecision is not null)
        {
            return await ApplyDecisionPolicyAsync(
                context,
                threatEvaluation.BlockingDecision,
                EmptyConstraintResults,
                protectedThreatDecision: threatEvaluation.BlockingDecision,
                cancellationToken)
                .ConfigureAwait(false);
        }

        if (constraints.Length == 0)
        {
            if (!options.DenyWhenNoConstraints)
            {
                LogEmptyPolicyAllowed(context);
            }

            GovernanceDecision noConstraintDecision = CreateNoConstraintDecision(
                context,
                threatEvaluation.WarningReasons,
                options.DenyWhenNoConstraints);

            return await ApplyDecisionPolicyAsync(
                context,
                noConstraintDecision,
                EmptyConstraintResults,
                protectedThreatDecision: threatEvaluation.WarningReasons.Count > 0 ? noConstraintDecision : null,
                cancellationToken)
                .ConfigureAwait(false);
        }

        List<ConstraintEvaluationResult>? results = decisionPolicy is null
            ? null
            : new List<ConstraintEvaluationResult>(constraints.Length);
        var denials = new OperationReasonAccumulator();
        var warnings = new OperationReasonAccumulator();
        warnings.AddRange(threatEvaluation.WarningReasons);

        foreach (IAsiBackboneConstraint<TContext> constraint in constraints)
        {
            cancellationToken.ThrowIfCancellationRequested();

            ConstraintEvaluationResult result;
            try
            {
                result = await constraint
                    .EvaluateAsync(context, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception exception) when (options.TreatConstraintExceptionAsDenial && exception is not OperationCanceledException)
            {
                return await CreateConstraintExceptionDecisionAsync(
                    context,
                    constraint,
                    results,
                    exception,
                    cancellationToken)
                    .ConfigureAwait(false);
            }

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

        GovernanceDecision? protectedThreatDecision = threatEvaluation.WarningReasons.Count > 0 && composedDecision.IsAllowed
            ? GovernanceDecision.Warning(
                threatEvaluation.WarningReasons,
                correlationId: context.CorrelationId,
                policyVersion: context.PolicyVersion,
                policyHash: context.PolicyHash)
            : null;

        return await ApplyDecisionPolicyAsync(
            context,
            composedDecision,
            CreateConstraintResultsView(results),
            protectedThreatDecision,
            cancellationToken)
            .ConfigureAwait(false);
    }

    private static GovernanceDecision Compose(
        TContext context,
        OperationReasonAccumulator denials,
        OperationReasonAccumulator warnings,
        bool includeWarningsWhenDenied)
    {
        return denials.Count > 0
            ? includeWarningsWhenDenied && warnings.Count > 0
                ? GovernanceDecision.Deny(
                    warnings.Concat(denials),
                    correlationId: context.CorrelationId,
                    policyVersion: context.PolicyVersion,
                    policyHash: context.PolicyHash)
                : CreateDeniedDecision(context, denials)
            : warnings.Count > 0
            ? CreateWarningDecision(context, warnings)
            : GovernanceDecision.Allow(
            correlationId: context.CorrelationId,
            policyVersion: context.PolicyVersion,
            policyHash: context.PolicyHash);
    }

    private static GovernanceDecision CreateNoConstraintDecision(
        TContext context,
        IReadOnlyList<OperationReason> threatWarningReasons)
    {
        return threatWarningReasons.Count > 0
            ? GovernanceDecision.Warning(
                threatWarningReasons,
                correlationId: context.CorrelationId,
                policyVersion: context.PolicyVersion,
                policyHash: context.PolicyHash)
            : GovernanceDecision.Allow(
            correlationId: context.CorrelationId,
            policyVersion: context.PolicyVersion,
            policyHash: context.PolicyHash);
    }

    private GovernanceDecision CreateNoConstraintDecision(
        TContext context,
        IReadOnlyList<OperationReason> threatWarningReasons,
        bool denyWhenNoConstraints)
    {
        return !denyWhenNoConstraints
            ? CreateNoConstraintDecision(context, threatWarningReasons)
            : GovernanceDecision.Deny(
            options.NoConstraintsReasonCode,
            options.NoConstraintsReasonMessage,
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

    private async ValueTask<GovernanceDecision> ApplyDecisionPolicyAsync(
        TContext context,
        GovernanceDecision decision,
        IReadOnlyList<ConstraintEvaluationResult> results,
        GovernanceDecision? protectedThreatDecision,
        CancellationToken cancellationToken)
    {
        if (decisionPolicy is null)
        {
            return decision;
        }

        GovernanceDecision policyDecision = await decisionPolicy
            .ApplyAsync(context, decision, results, cancellationToken)
            .ConfigureAwait(false);

        return options.PreventThreatAssessmentAllowDowngrade && protectedThreatDecision is not null && policyDecision.IsAllowed
            ? protectedThreatDecision
            : policyDecision;
    }

    private async ValueTask<ThreatEvaluationResult> EvaluateThreatModelContributorsAsync(
        TContext context,
        CancellationToken cancellationToken)
    {
        if (threatModelContributors.Length == 0)
        {
            return ThreatEvaluationResult.Empty;
        }

        List<OperationReason>? reasons = null;
        GovernanceDecisionOutcome? selectedOutcome = null;

        foreach (IThreatModelContributor<TContext> contributor in threatModelContributors)
        {
            cancellationToken.ThrowIfCancellationRequested();

            ThreatAssessment? assessment;
            try
            {
                assessment = await contributor
                    .AssessAsync(context, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception exception) when (options.TreatThreatContributorExceptionAsDenial && exception is not OperationCanceledException)
            {
                return CreateThreatContributorExceptionResult(context, contributor, exception);
            }

            if (assessment is null || !assessment.IsActionable)
            {
                continue;
            }

            GovernanceDecisionOutcome effectiveOutcome = GetEffectiveThreatOutcome(assessment);
            selectedOutcome = SelectMoreRestrictiveOutcome(selectedOutcome, effectiveOutcome);
            reasons ??= [];
            reasons.Add(assessment.ToOperationReason(GetContributorName(contributor), effectiveOutcome));
        }

        return reasons is null || selectedOutcome is null
            ? ThreatEvaluationResult.Empty
            : CreateThreatEvaluationResult(context, selectedOutcome.Value, reasons.AsReadOnly());
    }

    private ThreatEvaluationResult CreateThreatContributorExceptionResult(
        TContext context,
        IThreatModelContributor<TContext> contributor,
        Exception exception)
    {
        string contributorName = GetContributorName(contributor);
        LogThreatContributorExceptionDenied(context, contributorName, exception);

        Dictionary<string, string> metadata = new(StringComparer.Ordinal)
        {
            ["threat.contributor"] = contributorName,
            ["threat.failure"] = exception.GetType().Name
        };

        var reason = OperationReason.Create(
            options.ThreatContributorExceptionReasonCode,
            options.ThreatContributorExceptionReasonMessage,
            metadata);

        var decision = GovernanceDecision.Deny(
            reason,
            correlationId: context.CorrelationId,
            policyVersion: context.PolicyVersion,
            policyHash: context.PolicyHash);

        return ThreatEvaluationResult.ForBlockingDecision(decision);
    }

    private static ThreatEvaluationResult CreateThreatEvaluationResult(
        TContext context,
        GovernanceDecisionOutcome outcome,
        ReadOnlyCollection<OperationReason> reasons)
    {
        return outcome switch
        {
            GovernanceDecisionOutcome.Denied => ThreatEvaluationResult.ForBlockingDecision(
                GovernanceDecision.Deny(
                    reasons,
                    correlationId: context.CorrelationId,
                    policyVersion: context.PolicyVersion,
                    policyHash: context.PolicyHash)),
            GovernanceDecisionOutcome.Deferred => ThreatEvaluationResult.ForBlockingDecision(
                GovernanceDecision.Defer(
                    reasons[0].Code,
                    reasons[0].Message,
                    correlationId: context.CorrelationId,
                    policyVersion: context.PolicyVersion,
                    policyHash: context.PolicyHash)),
            GovernanceDecisionOutcome.AcknowledgmentRequired => ThreatEvaluationResult.ForBlockingDecision(
                GovernanceDecision.RequireAcknowledgment(
                    reasons[0].Code,
                    reasons[0].Message,
                    correlationId: context.CorrelationId,
                    policyVersion: context.PolicyVersion,
                    policyHash: context.PolicyHash)),
            GovernanceDecisionOutcome.EscalationRecommended => ThreatEvaluationResult.ForBlockingDecision(
                GovernanceDecision.Escalate(
                    reasons[0].Code,
                    reasons[0].Message,
                    correlationId: context.CorrelationId,
                    policyVersion: context.PolicyVersion,
                    policyHash: context.PolicyHash)),
            GovernanceDecisionOutcome.Warning => ThreatEvaluationResult.ForWarningReasons(reasons),
            GovernanceDecisionOutcome.Allowed => throw new NotImplementedException(),
            _ => ThreatEvaluationResult.Empty
        };
    }

    private static GovernanceDecisionOutcome GetEffectiveThreatOutcome(ThreatAssessment assessment)
    {
        return assessment.RecommendedOutcome is not GovernanceDecisionOutcome.Allowed
            ? assessment.RecommendedOutcome
            : assessment.Severity >= ThreatSeverity.High
            ? GovernanceDecisionOutcome.EscalationRecommended
            : GovernanceDecisionOutcome.Warning;
    }

    private static GovernanceDecisionOutcome SelectMoreRestrictiveOutcome(
        GovernanceDecisionOutcome? current,
        GovernanceDecisionOutcome candidate)
    {
        return current is null
            ? candidate
            : GetThreatOutcomeRank(candidate) > GetThreatOutcomeRank(current.Value)
            ? candidate
            : current.Value;
    }

    private static int GetThreatOutcomeRank(GovernanceDecisionOutcome outcome)
    {
        return outcome switch
        {
            GovernanceDecisionOutcome.Denied => 5,
            GovernanceDecisionOutcome.EscalationRecommended => 4,
            GovernanceDecisionOutcome.AcknowledgmentRequired => 3,
            GovernanceDecisionOutcome.Deferred => 2,
            GovernanceDecisionOutcome.Warning => 1,
            GovernanceDecisionOutcome.Allowed => throw new NotImplementedException(),
            _ => 0
        };
    }

    private static string GetContributorName(IThreatModelContributor<TContext> contributor)
    {
        return string.IsNullOrWhiteSpace(contributor.Name)
            ? "<unnamed>"
            : contributor.Name.Trim();
    }

    private async ValueTask<GovernanceDecision> CreateConstraintExceptionDecisionAsync(
        TContext context,
        IAsiBackboneConstraint<TContext> constraint,
        List<ConstraintEvaluationResult>? results,
        Exception exception,
        CancellationToken cancellationToken)
    {
        LogConstraintExceptionDenied(context, constraint.Name, exception);

        var exceptionResult = ConstraintEvaluationResult.Deny(
            options.ConstraintExceptionReasonCode,
            options.ConstraintExceptionReasonMessage);
        results?.Add(exceptionResult);

        var exceptionDecision = GovernanceDecision.Deny(
            exceptionResult.Reasons,
            correlationId: context.CorrelationId,
            policyVersion: context.PolicyVersion,
            policyHash: context.PolicyHash);

        return await ApplyDecisionPolicyAsync(
            context,
            exceptionDecision,
            CreateConstraintResultsView(results),
            protectedThreatDecision: null,
            cancellationToken)
            .ConfigureAwait(false);
    }

    private void LogEmptyPolicyAllowed(TContext context)
    {
        if (logger is null)
        {
            return;
        }

        EmptyPolicyAllowedWarning(
            logger,
            context.CorrelationId ?? string.Empty,
            context.PolicyVersion ?? string.Empty,
            context.PolicyHash ?? string.Empty,
            null);
    }

    private void LogConstraintExceptionDenied(
        TContext context,
        string constraintName,
        Exception exception)
    {
        if (logger is null)
        {
            return;
        }

        ConstraintExceptionDeniedError(
            logger,
            string.IsNullOrWhiteSpace(constraintName) ? "<unnamed>" : constraintName,
            context.CorrelationId ?? string.Empty,
            context.PolicyVersion ?? string.Empty,
            context.PolicyHash ?? string.Empty,
            exception);
    }

    private void LogThreatContributorExceptionDenied(
        TContext context,
        string contributorName,
        Exception exception)
    {
        if (logger is null)
        {
            return;
        }

        ThreatContributorExceptionDeniedError(
            logger,
            contributorName,
            context.CorrelationId ?? string.Empty,
            context.PolicyVersion ?? string.Empty,
            context.PolicyHash ?? string.Empty,
            exception);
    }

    private sealed class ThreatEvaluationResult
    {
        private static readonly IReadOnlyList<OperationReason> EmptyReasons =
            Array.AsReadOnly(Array.Empty<OperationReason>());

        private ThreatEvaluationResult(
            IReadOnlyList<OperationReason> warningReasons,
            GovernanceDecision? blockingDecision)
        {
            WarningReasons = warningReasons;
            BlockingDecision = blockingDecision;
        }

        public static ThreatEvaluationResult Empty { get; } = new(EmptyReasons, blockingDecision: null);

        public IReadOnlyList<OperationReason> WarningReasons { get; }

        public GovernanceDecision? BlockingDecision { get; }

        public static ThreatEvaluationResult ForWarningReasons(IReadOnlyList<OperationReason> reasons)
        {
            return new ThreatEvaluationResult(reasons, blockingDecision: null);
        }

        public static ThreatEvaluationResult ForBlockingDecision(GovernanceDecision decision)
        {
            return new ThreatEvaluationResult(EmptyReasons, decision);
        }
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

        public readonly ReadOnlyCollection<OperationReason> AsReadOnlyList()
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
