using AsiBackbone.AspNetCore.Endpoints;
using AsiBackbone.Core.Actors;
using AsiBackbone.Core.Audit;
using AsiBackbone.Core.Constraints;
using AsiBackbone.Core.Decisions;
using AsiBackbone.Core.Evaluation;
using AsiBackbone.Testing.Contracts;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace AsiBackbone.Testing.Tests.Contracts;

public sealed class AsiBackboneContractFixtureTests
{
    [Fact]
    public async Task PolicyEvaluatorContractPassesForHarnessEvaluator()
    {
        AsiBackboneTestHarnessOptions options = new();
        _ = options.DenyAllPolicies("contract.policy_denied", "Denied by contract test.");
        var evaluator = new AsiBackboneTestHarnessPolicyEvaluator(options);
        var contract = new HarnessPolicyEvaluatorContract(evaluator);

        GovernanceDecision decision = await contract.VerifyEvaluatorReturnsSafeDecisionAsync(TestContext.Current.CancellationToken);

        Assert.True(decision.IsDenied);
        Assert.Equal("contract-correlation", decision.CorrelationId);
        Assert.Equal("test-harness", decision.PolicyVersion);
        Assert.Equal("contract-policy-hash", decision.PolicyHash);
        Assert.Contains("contract.policy_denied", decision.ReasonCodes);
    }

    [Fact]
    public async Task CapabilityGrantContractPassesWhenInvalidGrantFailsClosed()
    {
        AsiBackboneTestHarnessOptions options = new();
        _ = options.DenyCapabilityGrants("contract.capability_denied", "Capability grant denied by contract test.");
        var validator = new AsiBackboneTestHarnessEndpointCapabilityGrantValidator(options);
        var contract = new HarnessCapabilityGrantValidatorContract(validator);

        GovernanceDecision decision = await contract.VerifyKnownInvalidCapabilityGrantDoesNotAllowAsync(TestContext.Current.CancellationToken);

        Assert.True(decision.IsDenied);
        Assert.Contains("contract.capability_denied", decision.ReasonCodes);
    }

    [Fact]
    public async Task CapabilityGrantContractFailsWhenInvalidGrantAllows()
    {
        var contract = new HarnessCapabilityGrantValidatorContract(new AllowingCapabilityGrantValidator());

        AsiBackboneContractViolationException exception = await Assert.ThrowsAsync<AsiBackboneContractViolationException>(
            async () => await contract.VerifyKnownInvalidCapabilityGrantDoesNotAllowAsync(TestContext.Current.CancellationToken));

        Assert.Contains("must not return Allow", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AuditSinkContractPassesForTestAuditSink()
    {
        var auditSink = new AsiBackboneTestAuditSink();
        var contract = new TestAuditSinkContract(auditSink);

        await contract.VerifyAuditSinkAcceptsValidResidueAsync(TestContext.Current.CancellationToken);

        _ = Assert.Single(auditSink.Entries);
        Assert.Equal("contract-event", auditSink.Entries[0].EventId);
    }

    [Fact]
    public void DecisionContractRejectsAuditResidueWithoutEventId()
    {
        var residue = new TestAuditResidue
        {
            EventId = ""
        };

        AsiBackboneContractViolationException exception = Assert.Throws<AsiBackboneContractViolationException>(
            () => AsiBackboneDecisionContract.VerifyAuditResidue(residue));

        Assert.Contains("event ID", exception.Message, StringComparison.Ordinal);
    }

    private sealed class HarnessPolicyEvaluatorContract(
        IAsiBackbonePolicyEvaluator<AsiBackboneConstraintEvaluationContext> evaluator)
        : AsiBackbonePolicyEvaluatorContract<AsiBackboneConstraintEvaluationContext>
    {
        protected override IAsiBackbonePolicyEvaluator<AsiBackboneConstraintEvaluationContext> CreateEvaluator()
        {
            return evaluator;
        }

        protected override AsiBackboneConstraintEvaluationContext CreateEvaluationContext()
        {
            return new AsiBackboneConstraintEvaluationContext(
                correlationId: "contract-correlation",
                policyVersion: "contract-policy-v1",
                policyHash: "contract-policy-hash");
        }
    }

    private sealed class HarnessCapabilityGrantValidatorContract(
        IAsiBackboneEndpointCapabilityGrantValidator validator)
        : AsiBackboneEndpointCapabilityGrantValidatorContract
    {
        protected override IAsiBackboneEndpointCapabilityGrantValidator CreateValidator()
        {
            return validator;
        }
    }

    private sealed class TestAuditSinkContract(AsiBackboneTestAuditSink auditSink) : AsiBackboneAuditSinkContract
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

    private sealed class AllowingCapabilityGrantValidator : IAsiBackboneEndpointCapabilityGrantValidator
    {
        public ValueTask<GovernanceDecision> ValidateAsync(
            HttpContext httpContext,
            AsiBackboneEndpointGovernanceDescriptor descriptor,
            GovernanceDecision currentDecision,
            CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(GovernanceDecision.Allow(
                currentDecision.CorrelationId,
                currentDecision.TraceId,
                currentDecision.PolicyVersion,
                currentDecision.PolicyHash));
        }
    }

    private sealed class TestAuditResidue : IAsiBackboneAuditResidue
    {
        public string EventId { get; init; } = "contract-event";

        public DateTimeOffset OccurredUtc { get; init; } = DateTimeOffset.UtcNow;

        public string ActorId { get; init; } = "contract-actor";

        public AsiBackboneActorType ActorType { get; init; } = AsiBackboneActorType.System;

        public string? ActorDisplayName { get; init; } = "Contract Actor";

        public string OperationName { get; init; } = "contract.operation";

        public string Outcome { get; init; } = "Allowed";

        public IReadOnlyList<string> ReasonCodes { get; init; } = Array.Empty<string>();

        public string? CorrelationId { get; init; } = "contract-correlation";

        public string? TraceId { get; init; } = "contract-trace";

        public string? PolicyVersion { get; init; } = "contract-policy-v1";

        public string? PolicyHash { get; init; } = "contract-policy-hash";

        public IReadOnlyDictionary<string, string> Metadata { get; init; } = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["contract"] = "true"
        };
    }
}
