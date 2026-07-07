using AsiBackbone.Core.Actors;
using AsiBackbone.Core.Audit;
using AsiBackbone.Core.Constraints;
using AsiBackbone.Core.Decisions;
using AsiBackbone.Core.Evaluation;
using AsiBackbone.Testing.Contracts;
using Xunit;

namespace AsiBackbone.Testing.Tests.Contracts;

public sealed class AsiBackboneContractCoverageTests
{
    [Theory]
    [MemberData(nameof(SafeDecisions))]
    public void VerifySafeDecisionAcceptsSupportedSafeDecisionShapes(GovernanceDecision decision)
    {
        GovernanceDecision verifiedDecision = AsiBackboneDecisionContract.VerifySafeDecision(decision, "coverage decision");

        Assert.Same(decision, verifiedDecision);
    }

    [Fact]
    public void VerifySafeDecisionRejectsNullDecision()
    {
        AsiBackboneContractViolationException exception = Assert.Throws<AsiBackboneContractViolationException>(
            () => AsiBackboneDecisionContract.VerifySafeDecision(null));

        Assert.Contains("never return null", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void VerifyInvalidCapabilityGrantDoesNotAllowAcceptsDeniedDecision()
    {
        GovernanceDecision decision = GovernanceDecision.Deny("contract.capability_denied", "Capability grant denied.");

        GovernanceDecision verifiedDecision = AsiBackboneDecisionContract.VerifyInvalidCapabilityGrantDoesNotAllow(decision);

        Assert.Same(decision, verifiedDecision);
    }

    [Fact]
    public async Task PolicyEvaluatorContractRejectsNullDecision()
    {
        var contract = new PolicyEvaluatorContract(new NullDecisionPolicyEvaluator());

        AsiBackboneContractViolationException exception = await Assert.ThrowsAsync<AsiBackboneContractViolationException>(
            async () => await contract.VerifyEvaluatorReturnsSafeDecisionAsync(TestContext.Current.CancellationToken));

        Assert.Contains("must return a decision", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PolicyEvaluatorContractWrapsImplementationFailure()
    {
        var contract = new PolicyEvaluatorContract(new ThrowingPolicyEvaluator());

        AsiBackboneContractViolationException exception = await Assert.ThrowsAsync<AsiBackboneContractViolationException>(
            async () => await contract.VerifyEvaluatorReturnsSafeDecisionAsync(TestContext.Current.CancellationToken));

        Assert.Contains("must not fail open", exception.Message, StringComparison.Ordinal);
        Assert.IsType<InvalidOperationException>(exception.InnerException);
    }

    [Fact]
    public async Task DecisionPolicyContractPassesForSafeDecisionPolicy()
    {
        var contract = new DecisionPolicyContract(new PassthroughDecisionPolicy());

        GovernanceDecision decision = await contract.VerifyDecisionPolicyReturnsSafeDecisionAsync(TestContext.Current.CancellationToken);

        Assert.True(decision.IsAllowed);
        Assert.Equal("contract-correlation", decision.CorrelationId);
        Assert.Equal("contract-policy-v1", decision.PolicyVersion);
        Assert.Equal("contract-policy-hash", decision.PolicyHash);
    }

    [Fact]
    public async Task DecisionPolicyContractWrapsImplementationFailure()
    {
        var contract = new DecisionPolicyContract(new ThrowingDecisionPolicy());

        AsiBackboneContractViolationException exception = await Assert.ThrowsAsync<AsiBackboneContractViolationException>(
            async () => await contract.VerifyDecisionPolicyReturnsSafeDecisionAsync(TestContext.Current.CancellationToken));

        Assert.Contains("Decision-policy implementations", exception.Message, StringComparison.Ordinal);
        Assert.IsType<InvalidOperationException>(exception.InnerException);
    }

    [Fact]
    public async Task ConstraintContractPassesForAllowedConstraint()
    {
        var contract = new ConstraintContract(new AllowingConstraint());

        ConstraintEvaluationResult result = await contract.VerifyConstraintReturnsSafeResultAsync(TestContext.Current.CancellationToken);

        Assert.False(result.IsDenied);
    }

    [Fact]
    public async Task ConstraintContractPassesForDeniedConstraintWithReason()
    {
        var contract = new ConstraintContract(new DenyingConstraint());

        ConstraintEvaluationResult result = await contract.VerifyConstraintReturnsSafeResultAsync(TestContext.Current.CancellationToken);

        Assert.True(result.IsDenied);
        Assert.Contains("contract.constraint_denied", result.ReasonCodes);
    }

    [Fact]
    public async Task ConstraintContractRejectsEmptyConstraintName()
    {
        var contract = new ConstraintContract(new EmptyNameConstraint());

        AsiBackboneContractViolationException exception = await Assert.ThrowsAsync<AsiBackboneContractViolationException>(
            async () => await contract.VerifyConstraintReturnsSafeResultAsync(TestContext.Current.CancellationToken));

        Assert.Contains("stable, non-empty", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ConstraintContractRejectsNullResult()
    {
        var contract = new ConstraintContract(new NullResultConstraint());

        AsiBackboneContractViolationException exception = await Assert.ThrowsAsync<AsiBackboneContractViolationException>(
            async () => await contract.VerifyConstraintReturnsSafeResultAsync(TestContext.Current.CancellationToken));

        Assert.Contains("must return a result", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ConstraintContractWrapsImplementationFailure()
    {
        var contract = new ConstraintContract(new ThrowingConstraint());

        AsiBackboneContractViolationException exception = await Assert.ThrowsAsync<AsiBackboneContractViolationException>(
            async () => await contract.VerifyConstraintReturnsSafeResultAsync(TestContext.Current.CancellationToken));

        Assert.Contains("must not throw", exception.Message, StringComparison.Ordinal);
        Assert.IsType<InvalidOperationException>(exception.InnerException);
    }

    [Fact]
    public async Task AuditSinkContractWrapsWriteFailure()
    {
        var contract = new AuditSinkContract(new ThrowingAuditSink());

        AsiBackboneContractViolationException exception = await Assert.ThrowsAsync<AsiBackboneContractViolationException>(
            async () => await contract.VerifyAuditSinkAcceptsValidResidueAsync(TestContext.Current.CancellationToken));

        Assert.Contains("Audit sink implementations", exception.Message, StringComparison.Ordinal);
        Assert.IsType<InvalidOperationException>(exception.InnerException);
    }

    public static IEnumerable<object[]> SafeDecisions()
    {
        yield return [GovernanceDecision.Allow("contract-correlation", policyVersion: "policy-v1", policyHash: "policy-hash")];
        yield return [GovernanceDecision.Warning("contract.warning", "Warning reason.")];
        yield return [GovernanceDecision.Deny("contract.denied", "Denied reason.")];
        yield return [GovernanceDecision.Defer("contract.deferred", "Deferred reason.")];
        yield return [GovernanceDecision.RequireAcknowledgment("contract.acknowledgment", "Acknowledgment reason.")];
        yield return [GovernanceDecision.Escalate("contract.escalated", "Escalation reason.")];
    }

    private sealed class PolicyEvaluatorContract(
        IAsiBackbonePolicyEvaluator<AsiBackboneConstraintEvaluationContext> evaluator)
        : AsiBackbonePolicyEvaluatorContract<AsiBackboneConstraintEvaluationContext>
    {
        protected override IAsiBackbonePolicyEvaluator<AsiBackboneConstraintEvaluationContext> CreateEvaluator()
        {
            return evaluator;
        }

        protected override AsiBackboneConstraintEvaluationContext CreateEvaluationContext()
        {
            return CreateContractContext();
        }
    }

    private sealed class DecisionPolicyContract(
        IAsiBackboneDecisionPolicy<AsiBackboneConstraintEvaluationContext> decisionPolicy)
        : AsiBackboneDecisionPolicyContract<AsiBackboneConstraintEvaluationContext>
    {
        protected override IAsiBackboneDecisionPolicy<AsiBackboneConstraintEvaluationContext> CreateDecisionPolicy()
        {
            return decisionPolicy;
        }

        protected override AsiBackboneConstraintEvaluationContext CreateEvaluationContext()
        {
            return CreateContractContext();
        }
    }

    private sealed class ConstraintContract(
        IAsiBackboneConstraint<AsiBackboneConstraintEvaluationContext> constraint)
        : AsiBackboneConstraintContract<AsiBackboneConstraintEvaluationContext>
    {
        protected override IAsiBackboneConstraint<AsiBackboneConstraintEvaluationContext> CreateConstraint()
        {
            return constraint;
        }

        protected override AsiBackboneConstraintEvaluationContext CreateEvaluationContext()
        {
            return CreateContractContext();
        }
    }

    private sealed class AuditSinkContract(IAsiBackboneAuditSink auditSink) : AsiBackboneAuditSinkContract
    {
        protected override IAsiBackboneAuditSink CreateAuditSink()
        {
            return auditSink;
        }

        protected override IAsiBackboneAuditResidue CreateAuditResidue()
        {
            return new TestAuditResidue();
        }
    }

    private sealed class NullDecisionPolicyEvaluator : IAsiBackbonePolicyEvaluator<AsiBackboneConstraintEvaluationContext>
    {
        public ValueTask<GovernanceDecision> EvaluateAsync(
            AsiBackboneConstraintEvaluationContext context,
            CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult<GovernanceDecision>(null!);
        }
    }

    private sealed class ThrowingPolicyEvaluator : IAsiBackbonePolicyEvaluator<AsiBackboneConstraintEvaluationContext>
    {
        public ValueTask<GovernanceDecision> EvaluateAsync(
            AsiBackboneConstraintEvaluationContext context,
            CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("Policy evaluator failed.");
        }
    }

    private sealed class PassthroughDecisionPolicy : IAsiBackboneDecisionPolicy<AsiBackboneConstraintEvaluationContext>
    {
        public ValueTask<GovernanceDecision> ApplyAsync(
            AsiBackboneConstraintEvaluationContext context,
            GovernanceDecision composedDecision,
            IReadOnlyList<ConstraintEvaluationResult> constraintResults,
            CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(composedDecision);
        }
    }

    private sealed class ThrowingDecisionPolicy : IAsiBackboneDecisionPolicy<AsiBackboneConstraintEvaluationContext>
    {
        public ValueTask<GovernanceDecision> ApplyAsync(
            AsiBackboneConstraintEvaluationContext context,
            GovernanceDecision composedDecision,
            IReadOnlyList<ConstraintEvaluationResult> constraintResults,
            CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("Decision policy failed.");
        }
    }

    private sealed class AllowingConstraint : IAsiBackboneConstraint<AsiBackboneConstraintEvaluationContext>
    {
        public string Name => "contract.allow";

        public ValueTask<ConstraintEvaluationResult> EvaluateAsync(
            AsiBackboneConstraintEvaluationContext context,
            CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(ConstraintEvaluationResult.Allow());
        }
    }

    private sealed class DenyingConstraint : IAsiBackboneConstraint<AsiBackboneConstraintEvaluationContext>
    {
        public string Name => "contract.deny";

        public ValueTask<ConstraintEvaluationResult> EvaluateAsync(
            AsiBackboneConstraintEvaluationContext context,
            CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(ConstraintEvaluationResult.Deny("contract.constraint_denied", "Constraint denied."));
        }
    }

    private sealed class EmptyNameConstraint : IAsiBackboneConstraint<AsiBackboneConstraintEvaluationContext>
    {
        public string Name => " ";

        public ValueTask<ConstraintEvaluationResult> EvaluateAsync(
            AsiBackboneConstraintEvaluationContext context,
            CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(ConstraintEvaluationResult.Allow());
        }
    }

    private sealed class NullResultConstraint : IAsiBackboneConstraint<AsiBackboneConstraintEvaluationContext>
    {
        public string Name => "contract.null_result";

        public ValueTask<ConstraintEvaluationResult> EvaluateAsync(
            AsiBackboneConstraintEvaluationContext context,
            CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult<ConstraintEvaluationResult>(null!);
        }
    }

    private sealed class ThrowingConstraint : IAsiBackboneConstraint<AsiBackboneConstraintEvaluationContext>
    {
        public string Name => "contract.throwing";

        public ValueTask<ConstraintEvaluationResult> EvaluateAsync(
            AsiBackboneConstraintEvaluationContext context,
            CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("Constraint failed.");
        }
    }

    private sealed class ThrowingAuditSink : IAsiBackboneAuditSink
    {
        public ValueTask WriteAsync(
            IAsiBackboneAuditResidue residue,
            CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("Audit sink failed.");
        }
    }

    private sealed class TestAuditResidue : IAsiBackboneAuditResidue
    {
        public string EventId => "contract-event";

        public DateTimeOffset OccurredUtc => DateTimeOffset.UtcNow;

        public string ActorId => "contract-actor";

        public AsiBackboneActorType ActorType => AsiBackboneActorType.System;

        public string? ActorDisplayName => "Contract Actor";

        public string OperationName => "contract.operation";

        public string Outcome => "Allowed";

        public IReadOnlyList<string> ReasonCodes => Array.Empty<string>();

        public string? CorrelationId => "contract-correlation";

        public string? TraceId => "contract-trace";

        public string? PolicyVersion => "contract-policy-v1";

        public string? PolicyHash => "contract-policy-hash";

        public IReadOnlyDictionary<string, string> Metadata => new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["contract"] = "true"
        };
    }

    private static AsiBackboneConstraintEvaluationContext CreateContractContext()
    {
        return new AsiBackboneConstraintEvaluationContext(
            correlationId: "contract-correlation",
            policyVersion: "contract-policy-v1",
            policyHash: "contract-policy-hash");
    }
}
