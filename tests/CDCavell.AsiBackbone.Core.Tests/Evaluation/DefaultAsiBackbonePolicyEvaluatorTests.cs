using CDCavell.AsiBackbone.Core.Constraints;
using CDCavell.AsiBackbone.Core.Decisions;
using CDCavell.AsiBackbone.Core.Evaluation;
using Xunit;

namespace CDCavell.AsiBackbone.Core.Tests.Evaluation;

/// <summary>
/// Branch-focused unit tests for <see cref="DefaultAsiBackbonePolicyEvaluator{TContext}"/>.
/// </summary>
public sealed class DefaultAsiBackbonePolicyEvaluatorTests
{
    [Fact]
    public void ConstructorThrowsForNullConstraints()
    {
        _ = Assert.Throws<ArgumentNullException>(() =>
            new DefaultAsiBackbonePolicyEvaluator<TestPolicyContext>(null!));
    }

    [Fact]
    public async Task EvaluateThrowsForNullContext()
    {
        var evaluator = new DefaultAsiBackbonePolicyEvaluator<TestPolicyContext>([]);

        _ = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await evaluator.EvaluateAsync(null!, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task EvaluateWithNoConstraintsProducesAllowedDecision()
    {
        TestPolicyContext context = CreateContext();

        var evaluator = new DefaultAsiBackbonePolicyEvaluator<TestPolicyContext>([]);

        GovernanceDecision decision = await evaluator.EvaluateAsync(context, TestContext.Current.CancellationToken);

        Assert.True(decision.IsAllowed);
        Assert.True(decision.CanProceed);
        Assert.Empty(decision.ReasonCodes);
        Assert.Equal(context.CorrelationId, decision.CorrelationId);
        Assert.Equal(context.PolicyVersion, decision.PolicyVersion);
        Assert.Equal(context.PolicyHash, decision.PolicyHash);
    }

    [Fact]
    public async Task ConstructorMaterializesNonListConstraintEnumerable()
    {
        TestPolicyContext context = CreateContext();

        var evaluator = new DefaultAsiBackbonePolicyEvaluator<TestPolicyContext>(
            CreateConstraintEnumerable(
                ConstraintEvaluationResult.Warning(
                    "constraint.warning",
                    "The constraint produced a warning.")));

        GovernanceDecision decision = await evaluator.EvaluateAsync(context, TestContext.Current.CancellationToken);

        Assert.True(decision.IsWarning);
        Assert.True(decision.CanProceed);
        Assert.Contains("constraint.warning", decision.ReasonCodes);
    }

    [Fact]
    public async Task EvaluateHonorsCancellationBeforeConstraintRuns()
    {
        TestPolicyContext context = CreateContext();
        int evaluationCount = 0;

        var constraint = new DelegateConstraint(
            (_, _) =>
            {
                evaluationCount++;
                return ConstraintEvaluationResult.Allow();
            });

        var evaluator = new DefaultAsiBackbonePolicyEvaluator<TestPolicyContext>([constraint]);

        using var cancellationTokenSource = new CancellationTokenSource();
        await cancellationTokenSource.CancelAsync();

        _ = await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await evaluator.EvaluateAsync(context, cancellationTokenSource.Token));

        Assert.Equal(0, evaluationCount);
    }

    [Fact]
    public async Task EvaluateComposesDeniedDecisionBeforeWarnings()
    {
        TestPolicyContext context = CreateContext();

        var evaluator = new DefaultAsiBackbonePolicyEvaluator<TestPolicyContext>(
            [
                new StaticConstraint(
                    ConstraintEvaluationResult.Warning(
                        "constraint.warning",
                        "The constraint produced a warning.")),
                new StaticConstraint(
                    ConstraintEvaluationResult.Deny(
                        "constraint.denied",
                        "The constraint denied the operation."))
            ]);

        GovernanceDecision decision = await evaluator.EvaluateAsync(context, TestContext.Current.CancellationToken);

        Assert.True(decision.IsDenied);
        Assert.False(decision.CanProceed);
        Assert.Contains("constraint.denied", decision.ReasonCodes);
        Assert.DoesNotContain("constraint.warning", decision.ReasonCodes);
    }

    [Fact]
    public async Task EvaluateAppliesDecisionPolicyWithReadOnlyConstraintResults()
    {
        TestPolicyContext context = CreateContext();
        using var cancellationTokenSource = new CancellationTokenSource();

        var policy = new CapturingDecisionPolicy();

        var evaluator = new DefaultAsiBackbonePolicyEvaluator<TestPolicyContext>(
            [
                new StaticConstraint(ConstraintEvaluationResult.Allow()),
                new StaticConstraint(
                    ConstraintEvaluationResult.Warning(
                        "constraint.warning",
                        "The constraint produced a warning."))
            ],
            policy);

        GovernanceDecision decision = await evaluator.EvaluateAsync(
            context,
            cancellationTokenSource.Token);

        Assert.True(decision.IsDeferred);
        Assert.Equal("policy.deferred", Assert.Single(decision.ReasonCodes));

        Assert.Equal(1, policy.ApplyCount);
        Assert.Same(context, policy.Context);
        Assert.NotNull(policy.ComposedDecision);
        Assert.True(policy.ComposedDecision.IsWarning);
        Assert.Equal(cancellationTokenSource.Token, policy.CancellationToken);

        IReadOnlyList<ConstraintEvaluationResult> constraintResults =
            Assert.IsType<IReadOnlyList<ConstraintEvaluationResult>>(policy.ConstraintResults, exactMatch: false);

        Assert.Equal(2, constraintResults.Count);

        IList<ConstraintEvaluationResult> listView =
            Assert.IsType<IList<ConstraintEvaluationResult>>(policy.ConstraintResults, exactMatch: false);

        _ = Assert.Throws<NotSupportedException>(() =>
            listView.Add(ConstraintEvaluationResult.Allow()));
    }

    private static TestPolicyContext CreateContext()
    {
        return new TestPolicyContext
        {
            CorrelationId = "corr-branch-123",
            PolicyVersion = "v-branch",
            PolicyHash = "hash-branch"
        };
    }

    private static IEnumerable<IAsiBackboneConstraint<TestPolicyContext>> CreateConstraintEnumerable(
        params ConstraintEvaluationResult[] results)
    {
        foreach (ConstraintEvaluationResult result in results)
        {
            yield return new StaticConstraint(result);
        }
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

        public string Name => "static-constraint";

        public ValueTask<ConstraintEvaluationResult> EvaluateAsync(
            TestPolicyContext context,
            CancellationToken cancellationToken = default)
        {
            return new ValueTask<ConstraintEvaluationResult>(result);
        }
    }

    private sealed class DelegateConstraint(
        Func<TestPolicyContext, CancellationToken, ConstraintEvaluationResult> evaluate) : IAsiBackboneConstraint<TestPolicyContext>
    {
        private readonly Func<TestPolicyContext, CancellationToken, ConstraintEvaluationResult> evaluate = evaluate;

        public string Name => "delegate-constraint";

        public ValueTask<ConstraintEvaluationResult> EvaluateAsync(
            TestPolicyContext context,
            CancellationToken cancellationToken = default)
        {
            return new ValueTask<ConstraintEvaluationResult>(
                evaluate(context, cancellationToken));
        }
    }

    private sealed class CapturingDecisionPolicy : IAsiBackboneDecisionPolicy<TestPolicyContext>
    {
        public int ApplyCount { get; private set; }

        public TestPolicyContext? Context { get; private set; }

        public GovernanceDecision? ComposedDecision { get; private set; }

        public IReadOnlyList<ConstraintEvaluationResult>? ConstraintResults { get; private set; }

        public CancellationToken CancellationToken { get; private set; }

        public ValueTask<GovernanceDecision> ApplyAsync(
            TestPolicyContext context,
            GovernanceDecision composedDecision,
            IReadOnlyList<ConstraintEvaluationResult> constraintResults,
            CancellationToken cancellationToken = default)
        {
            ApplyCount++;
            Context = context;
            ComposedDecision = composedDecision;
            ConstraintResults = constraintResults;
            CancellationToken = cancellationToken;

            return new ValueTask<GovernanceDecision>(
                GovernanceDecision.Defer(
                    "policy.deferred",
                    "The capturing policy deferred the operation.",
                    correlationId: context.CorrelationId,
                    policyVersion: context.PolicyVersion,
                    policyHash: context.PolicyHash));
        }
    }
}
