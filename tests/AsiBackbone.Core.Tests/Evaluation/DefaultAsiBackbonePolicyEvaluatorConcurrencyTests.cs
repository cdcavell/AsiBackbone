using AsiBackbone.Core.Constraints;
using AsiBackbone.Core.Decisions;
using AsiBackbone.Core.Evaluation;
using AsiBackbone.Core.ThreatModeling;
using Xunit;

namespace AsiBackbone.Core.Tests.Evaluation;

/// <summary>
/// Regression coverage for the documented evaluator concurrency and construction-snapshot contract.
/// These tests do not prove thread safety for arbitrary host extensions.
/// </summary>
public sealed class DefaultAsiBackbonePolicyEvaluatorConcurrencyTests
{
    [Fact]
    public async Task ConstructorFreezesOptionsAndPreservesConfiguredBehavior()
    {
        var options = new AsiBackbonePolicyEvaluatorOptions
        {
            DenyWhenNoConstraints = false
        };

        var evaluator = new DefaultAsiBackbonePolicyEvaluator<TestContext>(
            constraints: [],
            decisionPolicy: null,
            options: options);

        _ = Assert.Throws<InvalidOperationException>(() => options.DenyWhenNoConstraints = true);

        GovernanceDecision decision = await evaluator.EvaluateAsync(
            CreateContext(),
            TestContext.Current.CancellationToken);

        Assert.True(decision.IsAllowed);
    }

    [Fact]
    public async Task ConstructorSnapshotsConstraintCollectionAndOrdering()
    {
        var constraints = new List<IAsiBackboneConstraint<TestContext>>
        {
            new FixedConstraint("first", ConstraintEvaluationResult.Warning("warning.first", "First warning.")),
            new FixedConstraint("second", ConstraintEvaluationResult.Deny("denial.second", "Second denial."))
        };

        var evaluator = new DefaultAsiBackbonePolicyEvaluator<TestContext>(
            constraints,
            decisionPolicy: null,
            options: new AsiBackbonePolicyEvaluatorOptions
            {
                ShortCircuitOnFirstDenial = true
            });

        constraints.Clear();
        constraints.Add(new FixedConstraint("replacement", ConstraintEvaluationResult.Allow()));

        GovernanceDecision decision = await evaluator.EvaluateAsync(
            CreateContext(),
            TestContext.Current.CancellationToken);

        Assert.True(decision.IsDenied);
        Assert.Equal(["warning.first", "denial.second"], decision.ReasonCodes);
    }

    [Fact]
    public async Task ConstructorSnapshotsThreatContributorCollectionAndOrdering()
    {
        var contributors = new List<IThreatModelContributor<TestContext>>
        {
            new FixedThreatContributor("first", CreateWarningAssessment("threat.first")),
            new FixedThreatContributor("second", CreateWarningAssessment("threat.second"))
        };

        var evaluator = new DefaultAsiBackbonePolicyEvaluator<TestContext>(
            constraints: [new FixedConstraint("allow", ConstraintEvaluationResult.Allow())],
            threatModelContributors: contributors,
            decisionPolicy: null,
            options: null);

        contributors.Clear();
        contributors.Add(new FixedThreatContributor("replacement", ThreatAssessment.NoThreat()));

        GovernanceDecision decision = await evaluator.EvaluateAsync(
            CreateContext(),
            TestContext.Current.CancellationToken);

        Assert.True(decision.IsWarning);
        Assert.Equal(["threat.first", "threat.second"], decision.ReasonCodes);
    }

    [Fact]
    public async Task ConcurrentEvaluationWithStatelessExtensionsProducesDeterministicResults()
    {
        var evaluator = new DefaultAsiBackbonePolicyEvaluator<TestContext>(
            constraints:
            [
                new FixedConstraint("warning", ConstraintEvaluationResult.Warning("policy.warning", "Warning.")),
                new FixedConstraint("allow", ConstraintEvaluationResult.Allow())
            ]);

        Task<GovernanceDecision>[] evaluations = Enumerable.Range(0, 64)
            .Select(index => evaluator
                .EvaluateAsync(
                    CreateContext($"correlation-{index}"),
                    TestContext.Current.CancellationToken)
                .AsTask())
            .ToArray();

        GovernanceDecision[] decisions = await Task.WhenAll(evaluations);

        Assert.All(decisions, decision =>
        {
            Assert.True(decision.IsWarning);
            Assert.Equal("policy.warning", Assert.Single(decision.ReasonCodes));
        });

        Assert.Equal(
            Enumerable.Range(0, 64).Select(index => $"correlation-{index}"),
            decisions.Select(decision => decision.CorrelationId));
    }

    private static TestContext CreateContext(string correlationId = "correlation-1")
    {
        return new TestContext(correlationId);
    }

    private static ThreatAssessment CreateWarningAssessment(string reasonCode)
    {
        return ThreatAssessment.Create(
            ThreatSeverity.Low,
            ThreatCategories.InputMalformed,
            reasonCode,
            "Threat warning.",
            GovernanceDecisionOutcome.Warning);
    }

    private sealed class TestContext(string correlationId) : IAsiBackboneConstraintEvaluationContext
    {
        public string? CorrelationId { get; } = correlationId;
        public string? PolicyVersion { get; } = "policy-v1";
        public string? PolicyHash { get; } = "policy-hash";
        public IReadOnlyDictionary<string, string> Metadata { get; } =
            new Dictionary<string, string>(StringComparer.Ordinal);
    }

    private sealed class FixedConstraint(
        string name,
        ConstraintEvaluationResult result) : IAsiBackboneConstraint<TestContext>
    {
        public string Name { get; } = name;

        public ValueTask<ConstraintEvaluationResult> EvaluateAsync(
            TestContext context,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(context);
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult(result);
        }
    }

    private sealed class FixedThreatContributor(
        string name,
        ThreatAssessment assessment) : IThreatModelContributor<TestContext>
    {
        public string Name { get; } = name;

        public ValueTask<ThreatAssessment> AssessAsync(
            TestContext context,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(context);
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult(assessment);
        }
    }
}
