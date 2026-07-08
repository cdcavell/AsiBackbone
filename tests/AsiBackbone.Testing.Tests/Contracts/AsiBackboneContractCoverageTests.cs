using AsiBackbone.Core.Actors;
using AsiBackbone.Core.Audit;
using AsiBackbone.Core.Constraints;
using AsiBackbone.Core.Decisions;
using AsiBackbone.Core.Evaluation;
using AsiBackbone.Testing.Contracts;
using Xunit;

namespace AsiBackbone.Testing.Tests.Contracts;

/// <summary>
/// Contains unit tests that verify the behavior of the AsiBackbone contract classes, ensuring that they enforce the expected contracts and handle various scenarios correctly.
/// </summary>
public sealed class AsiBackboneContractCoverageTests
{
    /// <summary>
    /// Verifies that the VerifySafeDecision method accepts all supported safe decision shapes without throwing exceptions and returns the same decision instance.
    /// </summary>
    /// <param name="safeDecisionCase"></param>
    [Theory]
    [MemberData(nameof(SafeDecisionCases))]
    public void VerifySafeDecisionAcceptsSupportedSafeDecisionShapes(SafeDecisionCase safeDecisionCase)
    {
        GovernanceDecision decision = CreateSafeDecision(safeDecisionCase);

        GovernanceDecision verifiedDecision = AsiBackboneDecisionContract.VerifySafeDecision(decision, "coverage decision");

        Assert.Same(decision, verifiedDecision);
    }

    /// <summary>
    /// Verifies that the VerifySafeDecision method rejects a null decision and throws an AsiBackboneContractViolationException with an appropriate message.
    /// </summary>
    [Fact]
    public void VerifySafeDecisionRejectsNullDecision()
    {
        AsiBackboneContractViolationException exception = Assert.Throws<AsiBackboneContractViolationException>(
            () => AsiBackboneDecisionContract.VerifySafeDecision(null));

        Assert.Contains("never return null", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// Verifies that the VerifyInvalidCapabilityGrantDoesNotAllow method rejects a denied decision and throws an AsiBackboneContractViolationException with an appropriate message.
    /// </summary>
    [Fact]
    public void VerifyInvalidCapabilityGrantDoesNotAllowAcceptsDeniedDecision()
    {
        var decision = GovernanceDecision.Deny("contract.capability_denied", "Capability grant denied.");

        GovernanceDecision verifiedDecision = AsiBackboneDecisionContract.VerifyInvalidCapabilityGrantDoesNotAllow(decision);

        Assert.Same(decision, verifiedDecision);
    }

    /// <summary>
    /// Verifies that the PolicyEvaluatorContract rejects a null decision returned by the policy evaluator and throws an AsiBackboneContractViolationException with an appropriate message.
    /// </summary>
    /// <returns>
    /// A task representing the asynchronous operation.
    /// </returns>
    [Fact]
    public async Task PolicyEvaluatorContractRejectsNullDecision()
    {
        var contract = new PolicyEvaluatorContract(new NullDecisionPolicyEvaluator());

        AsiBackboneContractViolationException exception = await Assert.ThrowsAsync<AsiBackboneContractViolationException>(
            async () => await contract.VerifyEvaluatorReturnsSafeDecisionAsync(TestContext.Current.CancellationToken));

        Assert.Contains("must return a decision", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// Verifies that the PolicyEvaluatorContract wraps any exceptions thrown by the policy evaluator and throws an AsiBackboneContractViolationException with an appropriate message and inner exception.
    /// </summary>
    /// <returns>
    /// A task representing the asynchronous operation.
    /// </returns>
    [Fact]
    public async Task PolicyEvaluatorContractWrapsImplementationFailure()
    {
        var contract = new PolicyEvaluatorContract(new ThrowingPolicyEvaluator());

        AsiBackboneContractViolationException exception = await Assert.ThrowsAsync<AsiBackboneContractViolationException>(
            async () => await contract.VerifyEvaluatorReturnsSafeDecisionAsync(TestContext.Current.CancellationToken));

        Assert.Contains("must not fail open", exception.Message, StringComparison.Ordinal);
        _ = Assert.IsType<InvalidOperationException>(exception.InnerException);
    }

    /// <summary>
    /// Verifies that the DecisionPolicyContract passes for a safe decision policy and returns the expected safe decision with the correct properties.
    /// </summary>
    /// <returns>
    /// A task representing the asynchronous operation.
    /// </returns>
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

    /// <summary>
    /// Verifies that the DecisionPolicyContract wraps any exceptions thrown by the decision policy and throws an AsiBackboneContractViolationException with an appropriate message and inner exception.
    /// </summary>
    /// <returns>
    /// A task representing the asynchronous operation.
    /// </returns>
    [Fact]
    public async Task DecisionPolicyContractWrapsImplementationFailure()
    {
        var contract = new DecisionPolicyContract(new ThrowingDecisionPolicy());

        AsiBackboneContractViolationException exception = await Assert.ThrowsAsync<AsiBackboneContractViolationException>(
            async () => await contract.VerifyDecisionPolicyReturnsSafeDecisionAsync(TestContext.Current.CancellationToken));

        Assert.Contains("Decision-policy implementations", exception.Message, StringComparison.Ordinal);
        _ = Assert.IsType<InvalidOperationException>(exception.InnerException);
    }

    /// <summary>
    /// Verifies that the ConstraintContract passes for an allowed constraint and returns the expected safe result.
    /// </summary>
    /// <returns>
    /// A task representing the asynchronous operation.
    /// </returns>
    [Fact]
    public async Task ConstraintContractPassesForAllowedConstraint()
    {
        var contract = new ConstraintContract(new AllowingConstraint());

        ConstraintEvaluationResult result = await contract.VerifyConstraintReturnsSafeResultAsync(TestContext.Current.CancellationToken);

        Assert.False(result.IsDenied);
    }

    /// <summary>
    /// Verifies that the ConstraintContract passes for a denied constraint and returns the expected safe result with the correct reason codes.
    /// </summary>
    /// <returns>
    /// A task representing the asynchronous operation.
    /// </returns>
    [Fact]
    public async Task ConstraintContractPassesForDeniedConstraintWithReason()
    {
        var contract = new ConstraintContract(new DenyingConstraint());

        ConstraintEvaluationResult result = await contract.VerifyConstraintReturnsSafeResultAsync(TestContext.Current.CancellationToken);

        Assert.True(result.IsDenied);
        Assert.Contains("contract.constraint_denied", result.ReasonCodes);
    }

    /// <summary>
    /// Verifies that the ConstraintContract rejects constraints with empty names.
    /// </summary>
    /// <returns>
    /// A task representing the asynchronous operation.
    /// </returns>
    [Fact]
    public async Task ConstraintContractRejectsEmptyConstraintName()
    {
        var contract = new ConstraintContract(new EmptyNameConstraint());

        AsiBackboneContractViolationException exception = await Assert.ThrowsAsync<AsiBackboneContractViolationException>(
            async () => await contract.VerifyConstraintReturnsSafeResultAsync(TestContext.Current.CancellationToken));

        Assert.Contains("stable, non-empty", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// Verifies that the ConstraintContract rejects constraints that return null results.
    /// </summary>
    /// <returns>
    /// A task representing the asynchronous operation.
    /// </returns>
    [Fact]
    public async Task ConstraintContractRejectsNullResult()
    {
        var contract = new ConstraintContract(new NullResultConstraint());

        AsiBackboneContractViolationException exception = await Assert.ThrowsAsync<AsiBackboneContractViolationException>(
            async () => await contract.VerifyConstraintReturnsSafeResultAsync(TestContext.Current.CancellationToken));

        Assert.Contains("must return a result", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// Verifies that the ConstraintContract wraps any exceptions thrown by the constraint and throws an AsiBackboneContractViolationException with an appropriate message and inner exception.
    /// </summary>
    /// <returns>
    /// A task representing the asynchronous operation.
    /// </returns>
    [Fact]
    public async Task ConstraintContractWrapsImplementationFailure()
    {
        var contract = new ConstraintContract(new ThrowingConstraint());

        AsiBackboneContractViolationException exception = await Assert.ThrowsAsync<AsiBackboneContractViolationException>(
            async () => await contract.VerifyConstraintReturnsSafeResultAsync(TestContext.Current.CancellationToken));

        Assert.Contains("must not throw", exception.Message, StringComparison.Ordinal);
        _ = Assert.IsType<InvalidOperationException>(exception.InnerException);
    }

    /// <summary>
    /// Verifies that the AuditSinkContract wraps any exceptions thrown by the audit sink and throws an AsiBackboneContractViolationException with an appropriate message and inner exception.
    /// </summary>
    /// <returns>
    /// A task representing the asynchronous operation.
    /// </returns>
    [Fact]
    public async Task AuditSinkContractWrapsWriteFailure()
    {
        var contract = new AuditSinkContract(new ThrowingAuditSink());

        AsiBackboneContractViolationException exception = await Assert.ThrowsAsync<AsiBackboneContractViolationException>(
            async () => await contract.VerifyAuditSinkAcceptsValidResidueAsync(TestContext.Current.CancellationToken));

        Assert.Contains("Audit sink implementations", exception.Message, StringComparison.Ordinal);
        _ = Assert.IsType<InvalidOperationException>(exception.InnerException);
    }

    /// <summary>
    /// Gets the test cases for all supported safe decision shapes, which are used to verify that the VerifySafeDecision method accepts them without throwing exceptions.
    /// </summary>
    public static TheoryData<SafeDecisionCase> SafeDecisionCases =>
    [
        SafeDecisionCase.Allowed,
        SafeDecisionCase.Warning,
        SafeDecisionCase.Denied,
        SafeDecisionCase.Deferred,
        SafeDecisionCase.AcknowledgmentRequired,
        SafeDecisionCase.EscalationRecommended
    ];

    private static GovernanceDecision CreateSafeDecision(SafeDecisionCase safeDecisionCase)
    {
        return safeDecisionCase switch
        {
            SafeDecisionCase.Allowed => GovernanceDecision.Allow(
                "contract-correlation",
                policyVersion: "policy-v1",
                policyHash: "policy-hash"),

            SafeDecisionCase.Warning => GovernanceDecision.Warning(
                "contract.warning",
                "Warning reason."),

            SafeDecisionCase.Denied => GovernanceDecision.Deny(
                "contract.denied",
                "Denied reason."),

            SafeDecisionCase.Deferred => GovernanceDecision.Defer(
                "contract.deferred",
                "Deferred reason."),

            SafeDecisionCase.AcknowledgmentRequired => GovernanceDecision.RequireAcknowledgment(
                "contract.acknowledgment",
                "Acknowledgment reason."),

            SafeDecisionCase.EscalationRecommended => GovernanceDecision.Escalate(
                "contract.escalated",
                "Escalation reason."),

            _ => throw new ArgumentOutOfRangeException(
                nameof(safeDecisionCase),
                safeDecisionCase,
                "Unsupported safe decision case.")
        };
    }

    /// <summary>
    /// Represents the different cases of safe decisions that can be created and verified in the contract coverage tests.
    /// </summary>
    public enum SafeDecisionCase
    {
        /// <summary>
        /// Represents a safe decision that allows the requested action without any warnings or restrictions.
        /// </summary>
        Allowed,
        /// <summary>
        /// Represents a safe decision that allows the requested action but includes a warning message indicating potential issues or considerations.
        /// </summary>
        Warning,
        /// <summary>
        /// Represents a safe decision that denies the requested action, providing a reason for the denial.
        /// </summary>
        Denied,
        /// <summary>
        /// Represents a safe decision that defers the requested action, indicating that further evaluation or information is needed before a final decision can be made.
        /// </summary>
        Deferred,
        /// <summary>
        /// Represents a safe decision that requires acknowledgment before proceeding.
        /// </summary>
        AcknowledgmentRequired,
        /// <summary>
        /// Represents a safe decision that recommends escalation to a higher authority or department.
        /// </summary>
        EscalationRecommended
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
