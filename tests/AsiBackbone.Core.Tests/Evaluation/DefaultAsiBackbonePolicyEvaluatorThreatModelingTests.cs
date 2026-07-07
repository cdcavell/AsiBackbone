using AsiBackbone.Core.Constraints;
using AsiBackbone.Core.Decisions;
using AsiBackbone.Core.Evaluation;
using AsiBackbone.Core.ThreatModeling;
using Xunit;

namespace AsiBackbone.Core.Tests.Evaluation;

/// <summary>
/// Behavior coverage for threat model contributor integration in policy evaluation.
/// </summary>
public sealed class DefaultAsiBackbonePolicyEvaluatorThreatModelingTests
{
    [Fact]
    public async Task EvaluateNoThreatContributorAllowsDecisionAndRunsContributor()
    {
        TestPolicyContext context = CreateContext();
        bool contributorRan = false;
        var evaluator = new DefaultAsiBackbonePolicyEvaluator<TestPolicyContext>(
            [new StaticConstraint(ConstraintEvaluationResult.Allow())],
            [new DelegateThreatContributor(
                "no-threat-contributor",
                (_, _) =>
                {
                    contributorRan = true;
                    return ThreatAssessment.NoThreat();
                })]);

        GovernanceDecision decision = await evaluator.EvaluateAsync(context, TestContext.Current.CancellationToken);

        Assert.True(contributorRan);
        Assert.True(decision.IsAllowed);
        Assert.True(decision.CanProceed);
        Assert.Empty(decision.ReasonCodes);
    }

    [Fact]
    public async Task EvaluateHighSeverityAllowRecommendationPromotesToEscalationAndSkipsConstraints()
    {
        TestPolicyContext context = CreateContext();
        bool constraintRan = false;
        var evaluator = new DefaultAsiBackbonePolicyEvaluator<TestPolicyContext>(
            [new DelegateConstraint(
                (_, _) =>
                {
                    constraintRan = true;
                    return ConstraintEvaluationResult.Allow();
                })],
            [new StaticThreatContributor(
                "policy-bypass-contributor",
                ThreatAssessment.Create(
                    ThreatSeverity.High,
                    ThreatCategories.PolicyBypassAttempt,
                    "threat.policy_bypass",
                    "Possible policy bypass attempt.",
                    GovernanceDecisionOutcome.Allowed))]);

        GovernanceDecision decision = await evaluator.EvaluateAsync(context, TestContext.Current.CancellationToken);

        Assert.False(constraintRan);
        Assert.True(decision.EscalationRecommended);
        Assert.False(decision.CanProceed);
        Assert.Equal("threat.policy_bypass", Assert.Single(decision.ReasonCodes));
    }

    [Fact]
    public async Task EvaluateMultipleThreatContributorsCanAggregateDeniedReasonsWithMetadata()
    {
        TestPolicyContext context = CreateContext();
        var observedOrder = new List<string>();
        var evaluator = new DefaultAsiBackbonePolicyEvaluator<TestPolicyContext>(
            [new StaticConstraint(ConstraintEvaluationResult.Allow())],
            [
                new DelegateThreatContributor(
                    "prompt-injection-contributor",
                    (_, _) =>
                    {
                        observedOrder.Add("prompt");
                        return ThreatAssessment.Create(
                            ThreatSeverity.Low,
                            ThreatCategories.PromptInjectionLikeInput,
                            "threat.prompt_injection_like_input",
                            "Prompt-injection-like input was reported.",
                            GovernanceDecisionOutcome.Warning);
                    }),
                new DelegateThreatContributor(
                    "capability-token-contributor",
                    (_, _) =>
                    {
                        observedOrder.Add("token");
                        return ThreatAssessment.Create(
                            ThreatSeverity.Critical,
                            ThreatCategories.CapabilityTokenMismatch,
                            "threat.capability_token_mismatch",
                            "Capability token mismatch was reported.",
                            GovernanceDecisionOutcome.Denied);
                    })
            ]);

        GovernanceDecision decision = await evaluator.EvaluateAsync(context, TestContext.Current.CancellationToken);

        Assert.Equal(["prompt", "token"], observedOrder);
        Assert.True(decision.IsDenied);
        Assert.False(decision.CanProceed);
        Assert.Contains("threat.prompt_injection_like_input", decision.ReasonCodes);
        Assert.Contains("threat.capability_token_mismatch", decision.ReasonCodes);

        Assert.Equal(
            ThreatCategories.PromptInjectionLikeInput,
            decision.Reasons.Single(reason => reason.Code == "threat.prompt_injection_like_input").Metadata["threat.category"]);
        Assert.Equal(
            "prompt-injection-contributor",
            decision.Reasons.Single(reason => reason.Code == "threat.prompt_injection_like_input").Metadata["threat.contributor"]);
        Assert.Equal(
            ThreatSeverity.Critical.ToString(),
            decision.Reasons.Single(reason => reason.Code == "threat.capability_token_mismatch").Metadata["threat.severity"]);
    }

    [Fact]
    public async Task EvaluateThreatContributorExceptionFailsClosedByDefault()
    {
        TestPolicyContext context = CreateContext();
        var evaluator = new DefaultAsiBackbonePolicyEvaluator<TestPolicyContext>(
            [new StaticConstraint(ConstraintEvaluationResult.Allow())],
            [new ThrowingThreatContributor("throwing-threat-contributor")]);

        GovernanceDecision decision = await evaluator.EvaluateAsync(context, TestContext.Current.CancellationToken);

        Assert.True(decision.IsDenied);
        Assert.False(decision.CanProceed);
        Assert.Equal(
            AsiBackbonePolicyEvaluatorOptions.DefaultThreatContributorExceptionReasonCode,
            Assert.Single(decision.ReasonCodes));
        Assert.Equal(
            "throwing-threat-contributor",
            Assert.Single(decision.Reasons).Metadata["threat.contributor"]);
    }

    [Fact]
    public async Task EvaluateThreatWarningCannotBeDowngradedToAllowByDecisionPolicyByDefault()
    {
        TestPolicyContext context = CreateContext();
        var evaluator = new DefaultAsiBackbonePolicyEvaluator<TestPolicyContext>(
            [new StaticConstraint(ConstraintEvaluationResult.Allow())],
            [new StaticThreatContributor(
                "warning-threat-contributor",
                ThreatAssessment.Create(
                    ThreatSeverity.Low,
                    ThreatCategories.InputMalformed,
                    "threat.input_malformed",
                    "Malformed input indicator was reported.",
                    GovernanceDecisionOutcome.Warning))],
            new AlwaysAllowDecisionPolicy());

        GovernanceDecision decision = await evaluator.EvaluateAsync(context, TestContext.Current.CancellationToken);

        Assert.True(decision.IsWarning);
        Assert.True(decision.CanProceed);
        Assert.Equal("threat.input_malformed", Assert.Single(decision.ReasonCodes));
    }

    private static TestPolicyContext CreateContext()
    {
        return new TestPolicyContext
        {
            CorrelationId = "corr-threat-model-123",
            PolicyVersion = "v-threat-model",
            PolicyHash = "hash-threat-model"
        };
    }

    private sealed class TestPolicyContext : IAsiBackboneConstraintEvaluationContext
    {
        public string? CorrelationId { get; init; }

        public string? PolicyVersion { get; init; }

        public string? PolicyHash { get; init; }

        public IReadOnlyDictionary<string, string> Metadata { get; init; } =
            new Dictionary<string, string>(StringComparer.Ordinal);
    }

    private sealed class StaticConstraint(ConstraintEvaluationResult result) : IAsiBackboneConstraint<TestPolicyContext>
    {
        private readonly ConstraintEvaluationResult result = result;

        public string Name => "static-threat-model-constraint";

        public ValueTask<ConstraintEvaluationResult> EvaluateAsync(
            TestPolicyContext context,
            CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(result);
        }
    }

    private sealed class DelegateConstraint(
        Func<TestPolicyContext, CancellationToken, ConstraintEvaluationResult> evaluate) : IAsiBackboneConstraint<TestPolicyContext>
    {
        private readonly Func<TestPolicyContext, CancellationToken, ConstraintEvaluationResult> evaluate = evaluate;

        public string Name => "delegate-threat-model-constraint";

        public ValueTask<ConstraintEvaluationResult> EvaluateAsync(
            TestPolicyContext context,
            CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(evaluate(context, cancellationToken));
        }
    }

    private sealed class StaticThreatContributor(
        string name,
        ThreatAssessment assessment) : IThreatModelContributor<TestPolicyContext>
    {
        private readonly ThreatAssessment assessment = assessment;

        public string Name { get; } = name;

        public ValueTask<ThreatAssessment> AssessAsync(
            TestPolicyContext context,
            CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(assessment);
        }
    }

    private sealed class DelegateThreatContributor(
        string name,
        Func<TestPolicyContext, CancellationToken, ThreatAssessment> assess) : IThreatModelContributor<TestPolicyContext>
    {
        private readonly Func<TestPolicyContext, CancellationToken, ThreatAssessment> assess = assess;

        public string Name { get; } = name;

        public ValueTask<ThreatAssessment> AssessAsync(
            TestPolicyContext context,
            CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(assess(context, cancellationToken));
        }
    }

    private sealed class ThrowingThreatContributor(string name) : IThreatModelContributor<TestPolicyContext>
    {
        public string Name { get; } = name;

        public ValueTask<ThreatAssessment> AssessAsync(
            TestPolicyContext context,
            CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("Threat contributor failed.");
        }
    }

    private sealed class AlwaysAllowDecisionPolicy : IAsiBackboneDecisionPolicy<TestPolicyContext>
    {
        public ValueTask<GovernanceDecision> ApplyAsync(
            TestPolicyContext context,
            GovernanceDecision composedDecision,
            IReadOnlyList<ConstraintEvaluationResult> constraintResults,
            CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(GovernanceDecision.Allow(
                correlationId: context.CorrelationId,
                policyVersion: context.PolicyVersion,
                policyHash: context.PolicyHash));
        }
    }
}
