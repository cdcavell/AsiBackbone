using AsiBackbone.Core.Constraints;
using AsiBackbone.Core.Decisions;
using AsiBackbone.Core.Evaluation;
using AsiBackbone.Core.ThreatModeling;
using Xunit;

namespace AsiBackbone.Core.Tests.Evaluation;

/// <summary>
/// Targeted branch coverage for threat model contributor decision composition.
/// </summary>
public sealed class DefaultAsiBackbonePolicyEvaluatorThreatModelingBranchCoverageTests
{
    [Fact]
    public async Task EvaluateThreatWarningAndConstraintDenialSuppressesThreatWarningByDefault()
    {
        TestPolicyContext context = CreateContext();
        var evaluator = new DefaultAsiBackbonePolicyEvaluator<TestPolicyContext>(
            [new StaticConstraint(ConstraintEvaluationResult.Deny("constraint.denied", "Constraint denied."))],
            [CreateWarningContributor()]);

        GovernanceDecision decision = await evaluator.EvaluateAsync(context, TestContext.Current.CancellationToken);

        Assert.True(decision.IsDenied);
        Assert.Equal("constraint.denied", Assert.Single(decision.ReasonCodes));
        Assert.DoesNotContain("threat.warning", decision.ReasonCodes);
    }

    [Fact]
    public async Task EvaluateThreatWarningAndConstraintDenialPreservesThreatWarningWhenShortCircuitEnabled()
    {
        TestPolicyContext context = CreateContext();
        var evaluator = new DefaultAsiBackbonePolicyEvaluator<TestPolicyContext>(
            [new StaticConstraint(ConstraintEvaluationResult.Deny("constraint.denied", "Constraint denied."))],
            [CreateWarningContributor()],
            null,
            new AsiBackbonePolicyEvaluatorOptions
            {
                ShortCircuitOnFirstDenial = true
            });

        GovernanceDecision decision = await evaluator.EvaluateAsync(context, TestContext.Current.CancellationToken);

        Assert.True(decision.IsDenied);
        Assert.Contains("threat.warning", decision.ReasonCodes);
        Assert.Contains("constraint.denied", decision.ReasonCodes);
    }

    [Fact]
    public async Task EvaluateConstraintWarningAfterDenialIsIgnoredWhenNotShortCircuiting()
    {
        TestPolicyContext context = CreateContext();
        var evaluator = new DefaultAsiBackbonePolicyEvaluator<TestPolicyContext>(
            [
                new StaticConstraint(ConstraintEvaluationResult.Deny("constraint.denied", "Constraint denied.")),
                new StaticConstraint(ConstraintEvaluationResult.Warning("constraint.warning", "Constraint warned."))
            ],
            []);

        GovernanceDecision decision = await evaluator.EvaluateAsync(context, TestContext.Current.CancellationToken);

        Assert.True(decision.IsDenied);
        Assert.Contains("constraint.denied", decision.ReasonCodes);
        Assert.DoesNotContain("constraint.warning", decision.ReasonCodes);
    }

    [Fact]
    public async Task EvaluateShortCircuitDenialWithoutWarningsReturnsSingleDenialReason()
    {
        TestPolicyContext context = CreateContext();
        var evaluator = new DefaultAsiBackbonePolicyEvaluator<TestPolicyContext>(
            [new StaticConstraint(ConstraintEvaluationResult.Deny("constraint.denied", "Constraint denied."))],
            [],
            null,
            new AsiBackbonePolicyEvaluatorOptions
            {
                ShortCircuitOnFirstDenial = true
            });

        GovernanceDecision decision = await evaluator.EvaluateAsync(context, TestContext.Current.CancellationToken);

        Assert.True(decision.IsDenied);
        Assert.Equal("constraint.denied", Assert.Single(decision.ReasonCodes));
    }

    [Fact]
    public async Task EvaluateShortCircuitDenialSkipsLaterConstraint()
    {
        TestPolicyContext context = CreateContext();
        bool secondConstraintRan = false;
        var evaluator = new DefaultAsiBackbonePolicyEvaluator<TestPolicyContext>(
            [
                new StaticConstraint(ConstraintEvaluationResult.Deny("constraint.denied", "Constraint denied.")),
                new DelegateConstraint(
                    (_, _) =>
                    {
                        secondConstraintRan = true;
                        return ConstraintEvaluationResult.Allow();
                    })
            ],
            [],
            null,
            new AsiBackbonePolicyEvaluatorOptions
            {
                ShortCircuitOnFirstDenial = true
            });

        GovernanceDecision decision = await evaluator.EvaluateAsync(context, TestContext.Current.CancellationToken);

        Assert.True(decision.IsDenied);
        Assert.False(secondConstraintRan);
    }

    [Fact]
    public async Task EvaluateMultipleWarningsProducesMultipleWarningReasons()
    {
        TestPolicyContext context = CreateContext();
        var evaluator = new DefaultAsiBackbonePolicyEvaluator<TestPolicyContext>(
            [new StaticConstraint(ConstraintEvaluationResult.Warning("constraint.warning", "Constraint warned."))],
            [CreateWarningContributor()]);

        GovernanceDecision decision = await evaluator.EvaluateAsync(context, TestContext.Current.CancellationToken);

        Assert.True(decision.IsWarning);
        Assert.Contains("threat.warning", decision.ReasonCodes);
        Assert.Contains("constraint.warning", decision.ReasonCodes);
    }

    [Fact]
    public async Task EvaluateMultipleDenialsProducesMultipleDenialReasons()
    {
        TestPolicyContext context = CreateContext();
        var evaluator = new DefaultAsiBackbonePolicyEvaluator<TestPolicyContext>(
            [
                new StaticConstraint(ConstraintEvaluationResult.Deny("constraint.denied.one", "Constraint one denied.")),
                new StaticConstraint(ConstraintEvaluationResult.Deny("constraint.denied.two", "Constraint two denied."))
            ],
            []);

        GovernanceDecision decision = await evaluator.EvaluateAsync(context, TestContext.Current.CancellationToken);

        Assert.True(decision.IsDenied);
        Assert.Contains("constraint.denied.one", decision.ReasonCodes);
        Assert.Contains("constraint.denied.two", decision.ReasonCodes);
    }

    [Fact]
    public async Task EvaluateDecisionPolicyDenialIsNotOverriddenByThreatWarningProtection()
    {
        TestPolicyContext context = CreateContext();
        var evaluator = new DefaultAsiBackbonePolicyEvaluator<TestPolicyContext>(
            [new StaticConstraint(ConstraintEvaluationResult.Allow())],
            [CreateWarningContributor()],
            new AlwaysDenyDecisionPolicy());

        GovernanceDecision decision = await evaluator.EvaluateAsync(context, TestContext.Current.CancellationToken);

        Assert.True(decision.IsDenied);
        Assert.Equal("decision.policy_denied", Assert.Single(decision.ReasonCodes));
    }

    [Fact]
    public async Task EvaluateNoConstraintThreatWarningCanBeDowngradedWhenProtectionDisabled()
    {
        TestPolicyContext context = CreateContext();
        var evaluator = new DefaultAsiBackbonePolicyEvaluator<TestPolicyContext>(
            [],
            [CreateWarningContributor()],
            new AlwaysAllowDecisionPolicy(),
            new AsiBackbonePolicyEvaluatorOptions
            {
                PreventThreatAssessmentAllowDowngrade = false
            });

        GovernanceDecision decision = await evaluator.EvaluateAsync(context, TestContext.Current.CancellationToken);

        Assert.True(decision.IsAllowed);
        Assert.Empty(decision.ReasonCodes);
    }

    [Fact]
    public async Task EvaluateCanceledTokenThrowsBeforeContributorRuns()
    {
        TestPolicyContext context = CreateContext();
        bool contributorRan = false;
        using var cancellationTokenSource = new CancellationTokenSource();
        await cancellationTokenSource.CancelAsync();
        var evaluator = new DefaultAsiBackbonePolicyEvaluator<TestPolicyContext>(
            [new StaticConstraint(ConstraintEvaluationResult.Allow())],
            [new DelegateThreatContributor(
                "cancel-observer",
                (_, _) =>
                {
                    contributorRan = true;
                    return ThreatAssessment.NoThreat();
                })]);

        _ = await Assert.ThrowsAsync<OperationCanceledException>(
            async () => await evaluator.EvaluateAsync(context, cancellationTokenSource.Token));
        Assert.False(contributorRan);
    }

    [Fact]
    public async Task EvaluateContributorOperationCanceledExceptionPropagates()
    {
        TestPolicyContext context = CreateContext();
        var evaluator = new DefaultAsiBackbonePolicyEvaluator<TestPolicyContext>(
            [new StaticConstraint(ConstraintEvaluationResult.Allow())],
            [new DelegateThreatContributor(
                "canceling-threat-contributor",
                (_, _) => throw new OperationCanceledException())]);

        _ = await Assert.ThrowsAsync<OperationCanceledException>(
            async () => await evaluator.EvaluateAsync(context, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task EvaluateConstraintOperationCanceledExceptionPropagates()
    {
        TestPolicyContext context = CreateContext();
        var evaluator = new DefaultAsiBackbonePolicyEvaluator<TestPolicyContext>(
            [new ThrowingConstraint(new OperationCanceledException())],
            []);

        _ = await Assert.ThrowsAsync<OperationCanceledException>(
            async () => await evaluator.EvaluateAsync(context, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task EvaluateLessRestrictiveSecondThreatKeepsMoreRestrictiveFirstOutcome()
    {
        TestPolicyContext context = CreateContext();
        var evaluator = new DefaultAsiBackbonePolicyEvaluator<TestPolicyContext>(
            [new StaticConstraint(ConstraintEvaluationResult.Allow())],
            [
                new StaticThreatContributor(
                    "denial-threat-contributor",
                    ThreatAssessment.Create(
                        ThreatSeverity.Critical,
                        ThreatCategories.CapabilityTokenMismatch,
                        "threat.denied",
                        "Denied threat indicator was reported.",
                        GovernanceDecisionOutcome.Denied)),
                CreateWarningContributor()
            ]);

        GovernanceDecision decision = await evaluator.EvaluateAsync(context, TestContext.Current.CancellationToken);

        Assert.True(decision.IsDenied);
        Assert.Contains("threat.denied", decision.ReasonCodes);
        Assert.Contains("threat.warning", decision.ReasonCodes);
    }

    [Fact]
    public async Task EvaluateUnknownThreatOutcomeFallsBackToNormalConstraintComposition()
    {
        TestPolicyContext context = CreateContext();
        var evaluator = new DefaultAsiBackbonePolicyEvaluator<TestPolicyContext>(
            [new StaticConstraint(ConstraintEvaluationResult.Allow())],
            [new StaticThreatContributor(
                "unknown-outcome-threat-contributor",
                ThreatAssessment.Create(
                    ThreatSeverity.Medium,
                    ThreatCategories.AuditIntegrityRisk,
                    "threat.unknown_outcome",
                    "Unknown threat outcome was reported.",
                    (GovernanceDecisionOutcome)999))]);

        GovernanceDecision decision = await evaluator.EvaluateAsync(context, TestContext.Current.CancellationToken);

        Assert.True(decision.IsAllowed);
        Assert.Empty(decision.ReasonCodes);
    }

    [Fact]
    public async Task EvaluateUnknownSecondThreatOutcomeKeepsExistingWarningOutcome()
    {
        TestPolicyContext context = CreateContext();
        var evaluator = new DefaultAsiBackbonePolicyEvaluator<TestPolicyContext>(
            [new StaticConstraint(ConstraintEvaluationResult.Allow())],
            [
                CreateWarningContributor(),
                new StaticThreatContributor(
                    "unknown-outcome-threat-contributor",
                    ThreatAssessment.Create(
                        ThreatSeverity.Medium,
                        ThreatCategories.AuditIntegrityRisk,
                        "threat.unknown_outcome",
                        "Unknown threat outcome was reported.",
                        (GovernanceDecisionOutcome)999))
            ]);

        GovernanceDecision decision = await evaluator.EvaluateAsync(context, TestContext.Current.CancellationToken);

        Assert.True(decision.IsWarning);
        Assert.Contains("threat.warning", decision.ReasonCodes);
        Assert.Contains("threat.unknown_outcome", decision.ReasonCodes);
    }

    [Fact]
    public void ThreatAssessmentNoThreatCreatesNonActionableAllowedAssessment()
    {
        var assessment = ThreatAssessment.NoThreat();

        Assert.False(assessment.IsActionable);
        Assert.Equal(ThreatSeverity.None, assessment.Severity);
        Assert.Equal(ThreatCategories.None, assessment.Category);
        Assert.Equal(GovernanceDecisionOutcome.Allowed, assessment.RecommendedOutcome);
    }

    [Fact]
    public void ThreatAssessmentNoneSeverityWarningOutcomeIsActionable()
    {
        var assessment = ThreatAssessment.Create(
            ThreatSeverity.None,
            ThreatCategories.InputMalformed,
            "threat.none_warning",
            "None severity warning should still be actionable.",
            GovernanceDecisionOutcome.Warning);

        Assert.True(assessment.IsActionable);
    }

    [Fact]
    public void ThreatAssessmentEmptyMetadataUsesEmptyMetadataCollection()
    {
        var assessment = ThreatAssessment.Create(
            ThreatSeverity.Low,
            ThreatCategories.InputMalformed,
            "threat.empty_metadata",
            "Empty metadata should stay empty.",
            GovernanceDecisionOutcome.Warning,
            metadata: new Dictionary<string, string>());

        Assert.Empty(assessment.Metadata);
    }

    [Fact]
    public void ThreatAssessmentBlankMetadataKeysAreIgnored()
    {
        var assessment = ThreatAssessment.Create(
            ThreatSeverity.Low,
            ThreatCategories.InputMalformed,
            "threat.blank_metadata_key",
            "Blank metadata keys should be ignored.",
            GovernanceDecisionOutcome.Warning,
            metadata: new Dictionary<string, string>
            {
                [" "] = "ignored"
            });

        Assert.Empty(assessment.Metadata);
    }

    [Fact]
    public void ThreatAssessmentRejectsBlankCategory()
    {
        _ = Assert.Throws<ArgumentException>(() => ThreatAssessment.Create(
            ThreatSeverity.Low,
            " ",
            "threat.blank_category",
            "Blank category should be rejected.",
            GovernanceDecisionOutcome.Warning));
    }

    [Fact]
    public void ThreatAssessmentRejectsBlankReasonCode()
    {
        _ = Assert.Throws<ArgumentException>(() => ThreatAssessment.Create(
            ThreatSeverity.Low,
            ThreatCategories.InputMalformed,
            " ",
            "Blank reason code should be rejected.",
            GovernanceDecisionOutcome.Warning));
    }

    [Fact]
    public void ThreatAssessmentRejectsBlankDescription()
    {
        _ = Assert.Throws<ArgumentException>(() => ThreatAssessment.Create(
            ThreatSeverity.Low,
            ThreatCategories.InputMalformed,
            "threat.blank_description",
            " ",
            GovernanceDecisionOutcome.Warning));
    }

    [Fact]
    public void ThreatAssessmentRejectsNegativeConfidence()
    {
        _ = Assert.Throws<ArgumentOutOfRangeException>(() => ThreatAssessment.Create(
            ThreatSeverity.Low,
            ThreatCategories.InputMalformed,
            "threat.negative_confidence",
            "Negative confidence should be rejected.",
            GovernanceDecisionOutcome.Warning,
            confidence: -0.1D));
    }

    [Fact]
    public void ThreatAssessmentOperationReasonOmitsContributorMetadataWhenNameIsBlank()
    {
        var assessment = ThreatAssessment.Create(
            ThreatSeverity.Low,
            ThreatCategories.InputMalformed,
            "threat.input_malformed",
            "Malformed input indicator was reported.",
            GovernanceDecisionOutcome.Warning);

        var reason = assessment.ToOperationReason(" ");

        Assert.False(reason.Metadata.ContainsKey("threat.contributor"));
        Assert.Equal(ThreatCategories.InputMalformed, reason.Metadata["threat.category"]);
    }

    [Fact]
    public void ThreatAssessmentOperationReasonNormalizesNullMetadataValue()
    {
        var assessment = ThreatAssessment.Create(
            ThreatSeverity.Low,
            ThreatCategories.InputMalformed,
            "threat.null_metadata_value",
            "Null metadata value should be normalized.",
            GovernanceDecisionOutcome.Warning,
            metadata: new Dictionary<string, string>
            {
                ["nullable.value"] = null!
            });

        var reason = assessment.ToOperationReason();

        Assert.Equal(string.Empty, reason.Metadata["nullable.value"]);
    }

    private static TestPolicyContext CreateContext()
    {
        return new TestPolicyContext
        {
            CorrelationId = "corr-threat-branch-123",
            PolicyVersion = "v-threat-branch",
            PolicyHash = "hash-threat-branch"
        };
    }

    private static StaticThreatContributor CreateWarningContributor()
    {
        return new StaticThreatContributor(
            "warning-threat-contributor",
            ThreatAssessment.Create(
                ThreatSeverity.Low,
                ThreatCategories.InputMalformed,
                "threat.warning",
                "Warning threat indicator was reported.",
                GovernanceDecisionOutcome.Warning));
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

        public string Name => "static-threat-branch-constraint";

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

        public string Name => "delegate-threat-branch-constraint";

        public ValueTask<ConstraintEvaluationResult> EvaluateAsync(
            TestPolicyContext context,
            CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(evaluate(context, cancellationToken));
        }
    }

    private sealed class ThrowingConstraint(Exception exception) : IAsiBackboneConstraint<TestPolicyContext>
    {
        private readonly Exception exception = exception;

        public string Name => "throwing-threat-branch-constraint";

        public ValueTask<ConstraintEvaluationResult> EvaluateAsync(
            TestPolicyContext context,
            CancellationToken cancellationToken = default)
        {
            throw exception;
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

    private sealed class AlwaysDenyDecisionPolicy : IAsiBackboneDecisionPolicy<TestPolicyContext>
    {
        public ValueTask<GovernanceDecision> ApplyAsync(
            TestPolicyContext context,
            GovernanceDecision composedDecision,
            IReadOnlyList<ConstraintEvaluationResult> constraintResults,
            CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(GovernanceDecision.Deny(
                "decision.policy_denied",
                "Decision policy denied the operation.",
                correlationId: context.CorrelationId,
                policyVersion: context.PolicyVersion,
                policyHash: context.PolicyHash));
        }
    }
}
