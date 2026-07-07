using AsiBackbone.Core.Constraints;
using AsiBackbone.Core.Decisions;
using AsiBackbone.Core.Evaluation;
using AsiBackbone.Core.Results;
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
        ThreatAssessment assessment = ThreatAssessment.NoThreat();

        Assert.False(assessment.IsActionable);
        Assert.Equal(ThreatSeverity.None, assessment.Severity);
        Assert.Equal(ThreatCategories.None, assessment.Category);
        Assert.Equal(GovernanceDecisionOutcome.Allowed, assessment.RecommendedOutcome);
    }

    [Fact]
    public void ThreatAssessmentRejectsBlankCategory()
    {
        Assert.Throws<ArgumentException>(() => ThreatAssessment.Create(
            ThreatSeverity.Low,
            " ",
            "threat.blank_category",
            "Blank category should be rejected.",
            GovernanceDecisionOutcome.Warning));
    }

    [Fact]
    public void ThreatAssessmentRejectsBlankReasonCode()
    {
        Assert.Throws<ArgumentException>(() => ThreatAssessment.Create(
            ThreatSeverity.Low,
            ThreatCategories.InputMalformed,
            " ",
            "Blank reason code should be rejected.",
            GovernanceDecisionOutcome.Warning));
    }

    [Fact]
    public void ThreatAssessmentRejectsBlankDescription()
    {
        Assert.Throws<ArgumentException>(() => ThreatAssessment.Create(
            ThreatSeverity.Low,
            ThreatCategories.InputMalformed,
            "threat.blank_description",
            " ",
            GovernanceDecisionOutcome.Warning));
    }

    [Fact]
    public void ThreatAssessmentRejectsNegativeConfidence()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ThreatAssessment.Create(
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
        ThreatAssessment assessment = ThreatAssessment.Create(
            ThreatSeverity.Low,
            ThreatCategories.InputMalformed,
            "threat.input_malformed",
            "Malformed input indicator was reported.",
            GovernanceDecisionOutcome.Warning);

        OperationReason reason = assessment.ToOperationReason(" ");

        Assert.False(reason.Metadata.ContainsKey("threat.contributor"));
        Assert.Equal(ThreatCategories.InputMalformed, reason.Metadata["threat.category"]);
    }

    [Fact]
    public void ThreatAssessmentOperationReasonNormalizesNullMetadataValue()
    {
        ThreatAssessment assessment = ThreatAssessment.Create(
            ThreatSeverity.Low,
            ThreatCategories.InputMalformed,
            "threat.null_metadata_value",
            "Null metadata value should be normalized.",
            GovernanceDecisionOutcome.Warning,
            metadata: new Dictionary<string, string?>
            {
                ["nullable.value"] = null
            }!);

        OperationReason reason = assessment.ToOperationReason();

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
}
