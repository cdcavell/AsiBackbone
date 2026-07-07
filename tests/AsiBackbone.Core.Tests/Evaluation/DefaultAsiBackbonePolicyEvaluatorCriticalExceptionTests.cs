using AsiBackbone.Core.Constraints;
using AsiBackbone.Core.Decisions;
using AsiBackbone.Core.Evaluation;
using AsiBackbone.Core.ThreatModeling;
using Xunit;

namespace AsiBackbone.Core.Tests.Evaluation;

#pragma warning disable CA2201 // Reserved runtime exception types are intentionally constructed to verify catchable passthrough behavior.

/// <summary>
/// Regression coverage for the evaluator boundary between fail-closed policy exceptions and critical runtime failures.
/// </summary>
/// <remarks>
/// These tests use manually constructed reserved exception instances so the evaluator boundary can be exercised safely.
/// They do not imply that real corrupted-state failures, such as a process-ending stack overflow or process-corrupting
/// access violation, are recoverable or normally catchable by modern .NET.
/// </remarks>
public sealed class DefaultAsiBackbonePolicyEvaluatorCriticalExceptionTests
{
    /// <summary>
    /// Gets critical exception type names used to verify passthrough behavior when such failures reach managed code.
    /// </summary>
    /// <remarks>
    /// Some of these exception types are defensive/documentary in modern .NET because real corrupted-state failures may
    /// bypass ordinary <c>catch (Exception)</c> filters. The policy is still to propagate them if they are observed directly
    /// or through wrapper exceptions.
    /// </remarks>
    public static TheoryData<string> CriticalRuntimeExceptionTypes =>
    [
        nameof(OutOfMemoryException),
        nameof(StackOverflowException),
        nameof(AccessViolationException),
        nameof(AppDomainUnloadedException),
        nameof(BadImageFormatException),
        nameof(InvalidProgramException)
    ];

    [Fact]
    public async Task ConstraintExceptionAsDenialStillConvertsNormalConstraintException()
    {
        TestPolicyContext context = CreateContext();
        var evaluator = new DefaultAsiBackbonePolicyEvaluator<TestPolicyContext>(
            [new ThrowingConstraint(new InvalidOperationException("ordinary constraint failure"))],
            decisionPolicy: null,
            options: new AsiBackbonePolicyEvaluatorOptions
            {
                TreatConstraintExceptionAsDenial = true
            });

        GovernanceDecision decision = await evaluator.EvaluateAsync(context, TestContext.Current.CancellationToken);

        Assert.True(decision.IsDenied);
        Assert.Equal(
            AsiBackbonePolicyEvaluatorOptions.DefaultConstraintExceptionReasonCode,
            Assert.Single(decision.ReasonCodes));
    }

    [Fact]
    public async Task ConstraintExceptionAsDenialStillConvertsWrappedNonCriticalConstraintException()
    {
        TestPolicyContext context = CreateContext();
        var evaluator = new DefaultAsiBackbonePolicyEvaluator<TestPolicyContext>(
            [new ThrowingConstraint(new InvalidOperationException(
                "ordinary wrapper failure",
                new TimeoutException("ordinary inner failure")))],
            decisionPolicy: null,
            options: new AsiBackbonePolicyEvaluatorOptions
            {
                TreatConstraintExceptionAsDenial = true
            });

        GovernanceDecision decision = await evaluator.EvaluateAsync(context, TestContext.Current.CancellationToken);

        Assert.True(decision.IsDenied);
        Assert.Equal(
            AsiBackbonePolicyEvaluatorOptions.DefaultConstraintExceptionReasonCode,
            Assert.Single(decision.ReasonCodes));
    }

    [Theory]
    [MemberData(nameof(CriticalRuntimeExceptionTypes))]
    public async Task ConstraintExceptionAsDenialPropagatesCriticalRuntimeExceptions(string criticalExceptionType)
    {
        TestPolicyContext context = CreateContext();
        Exception expectedException = CreateCriticalException(criticalExceptionType);
        var evaluator = new DefaultAsiBackbonePolicyEvaluator<TestPolicyContext>(
            [new ThrowingConstraint(expectedException)],
            decisionPolicy: null,
            options: new AsiBackbonePolicyEvaluatorOptions
            {
                TreatConstraintExceptionAsDenial = true
            });

        Exception? exception = await Record.ExceptionAsync(async () =>
            await evaluator.EvaluateAsync(context, TestContext.Current.CancellationToken));

        Assert.Same(expectedException, exception);
    }

    [Fact]
    public async Task ConstraintExceptionAsDenialPropagatesWrappedCriticalRuntimeException()
    {
        TestPolicyContext context = CreateContext();
        var criticalException = new AccessViolationException("Critical inner failure should preserve host/runtime failure semantics.");
        var expectedException = new InvalidOperationException("Wrapper around critical failure.", criticalException);
        var evaluator = new DefaultAsiBackbonePolicyEvaluator<TestPolicyContext>(
            [new ThrowingConstraint(expectedException)],
            decisionPolicy: null,
            options: new AsiBackbonePolicyEvaluatorOptions
            {
                TreatConstraintExceptionAsDenial = true
            });

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await evaluator.EvaluateAsync(context, TestContext.Current.CancellationToken));

        Assert.Same(expectedException, exception);
        Assert.Same(criticalException, exception.InnerException);
    }

    [Fact]
    public async Task ThreatContributorExceptionAsDenialStillConvertsNormalContributorException()
    {
        TestPolicyContext context = CreateContext();
        var evaluator = new DefaultAsiBackbonePolicyEvaluator<TestPolicyContext>(
            [new StaticConstraint(ConstraintEvaluationResult.Allow())],
            [new ThrowingThreatContributor(new InvalidOperationException("ordinary contributor failure"))]);

        GovernanceDecision decision = await evaluator.EvaluateAsync(context, TestContext.Current.CancellationToken);

        Assert.True(decision.IsDenied);
        Assert.Equal(
            AsiBackbonePolicyEvaluatorOptions.DefaultThreatContributorExceptionReasonCode,
            Assert.Single(decision.ReasonCodes));
    }

    [Fact]
    public async Task ThreatContributorExceptionAsDenialStillConvertsWrappedNonCriticalContributorException()
    {
        TestPolicyContext context = CreateContext();
        var evaluator = new DefaultAsiBackbonePolicyEvaluator<TestPolicyContext>(
            [new StaticConstraint(ConstraintEvaluationResult.Allow())],
            [new ThrowingThreatContributor(new InvalidOperationException(
                "ordinary wrapper failure",
                new TimeoutException("ordinary inner failure")))]);

        GovernanceDecision decision = await evaluator.EvaluateAsync(context, TestContext.Current.CancellationToken);

        Assert.True(decision.IsDenied);
        Assert.Equal(
            AsiBackbonePolicyEvaluatorOptions.DefaultThreatContributorExceptionReasonCode,
            Assert.Single(decision.ReasonCodes));
    }

    [Theory]
    [MemberData(nameof(CriticalRuntimeExceptionTypes))]
    public async Task ThreatContributorExceptionAsDenialPropagatesCriticalRuntimeExceptions(string criticalExceptionType)
    {
        TestPolicyContext context = CreateContext();
        Exception expectedException = CreateCriticalException(criticalExceptionType);
        var evaluator = new DefaultAsiBackbonePolicyEvaluator<TestPolicyContext>(
            [new StaticConstraint(ConstraintEvaluationResult.Allow())],
            [new ThrowingThreatContributor(expectedException)]);

        Exception? exception = await Record.ExceptionAsync(async () =>
            await evaluator.EvaluateAsync(context, TestContext.Current.CancellationToken));

        Assert.Same(expectedException, exception);
    }

    [Fact]
    public async Task ThreatContributorExceptionAsDenialStillPropagatesOperationCanceledException()
    {
        TestPolicyContext context = CreateContext();
        var expectedException = new OperationCanceledException("Threat contributor cancellation should not be converted to denial.");
        var evaluator = new DefaultAsiBackbonePolicyEvaluator<TestPolicyContext>(
            [new StaticConstraint(ConstraintEvaluationResult.Allow())],
            [new ThrowingThreatContributor(expectedException)]);

        OperationCanceledException exception = await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await evaluator.EvaluateAsync(context, TestContext.Current.CancellationToken));

        Assert.Same(expectedException, exception);
    }

    private static TestPolicyContext CreateContext()
    {
        return new TestPolicyContext
        {
            CorrelationId = "corr-critical-123",
            PolicyVersion = "v-critical",
            PolicyHash = "hash-critical"
        };
    }

    private static Exception CreateCriticalException(string criticalExceptionType)
    {
        return criticalExceptionType switch
        {
            nameof(OutOfMemoryException) => new OutOfMemoryException("Critical host/runtime failures should not be converted to denials."),
            nameof(StackOverflowException) => new StackOverflowException("Catchable stack overflow exception instances should not be converted to denials."),
            nameof(AccessViolationException) => new AccessViolationException("Critical access violation failures should not be converted to denials."),
            nameof(AppDomainUnloadedException) => new AppDomainUnloadedException("AppDomain unload failures should not be converted to denials."),
            nameof(BadImageFormatException) => new BadImageFormatException("Bad image format failures should not be converted to denials."),
            nameof(InvalidProgramException) => new InvalidProgramException("Invalid program failures should not be converted to denials."),
            _ => throw new ArgumentOutOfRangeException(nameof(criticalExceptionType), criticalExceptionType, "Unknown critical exception type.")
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
        public string Name => "static-critical-constraint";

        public ValueTask<ConstraintEvaluationResult> EvaluateAsync(
            TestPolicyContext context,
            CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(result);
        }
    }

    private sealed class ThrowingConstraint(Exception exception) : IAsiBackboneConstraint<TestPolicyContext>
    {
        public string Name => "throwing-critical-constraint";

        public ValueTask<ConstraintEvaluationResult> EvaluateAsync(
            TestPolicyContext context,
            CancellationToken cancellationToken = default)
        {
            throw exception;
        }
    }

    private sealed class ThrowingThreatContributor(Exception exception) : IThreatModelContributor<TestPolicyContext>
    {
        public string Name => "throwing-critical-threat-contributor";

        public ValueTask<ThreatAssessment> AssessAsync(
            TestPolicyContext context,
            CancellationToken cancellationToken = default)
        {
            throw exception;
        }
    }
}

#pragma warning restore CA2201
