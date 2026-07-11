using System.Reflection;
using System.Runtime.CompilerServices;
using AsiBackbone.Core.Actors;
using AsiBackbone.Core.Audit;
using AsiBackbone.Core.Constraints;
using AsiBackbone.Core.Decisions;
using AsiBackbone.Core.Results;
using AsiBackbone.Testing.Contracts;
using Xunit;

namespace AsiBackbone.Testing.Tests.Contracts;

/// <summary>
/// Covers defensive decision and audit-residue contract branches.
/// </summary>
public sealed class AsiBackboneDecisionContractDefensiveTests
{
    [Fact]
    public void VerifySafeDecisionRejectsUnsupportedOutcome()
    {
        GovernanceDecision decision = GovernanceDecision.Allow();
        SetAutoProperty(decision, nameof(GovernanceDecision.Outcome), (GovernanceDecisionOutcome)int.MaxValue);

        AsiBackboneContractViolationException exception = Assert.Throws<AsiBackboneContractViolationException>(
            () => AsiBackboneDecisionContract.VerifySafeDecision(decision, "unsupported decision"));

        Assert.Contains("unsupported outcome", exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(GovernanceDecisionOutcome.Warning)]
    [InlineData(GovernanceDecisionOutcome.Denied)]
    [InlineData(GovernanceDecisionOutcome.Deferred)]
    [InlineData(GovernanceDecisionOutcome.AcknowledgmentRequired)]
    [InlineData(GovernanceDecisionOutcome.EscalationRecommended)]
    public void VerifySafeDecisionRejectsReasonRequiredOutcomeWithoutReasons(GovernanceDecisionOutcome outcome)
    {
        GovernanceDecision decision = GovernanceDecision.Allow();
        SetAutoProperty(decision, nameof(GovernanceDecision.Outcome), outcome);

        AsiBackboneContractViolationException exception = Assert.Throws<AsiBackboneContractViolationException>(
            () => AsiBackboneDecisionContract.VerifySafeDecision(decision, "reason-required decision"));

        Assert.Contains("at least one reason code", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void VerifySafeDecisionRejectsNullReason()
    {
        GovernanceDecision decision = GovernanceDecision.Warning("contract.warning", "Warning.");
        SetAutoProperty(decision, nameof(GovernanceDecision.Reasons), new OperationReason?[] { null! });

        AsiBackboneContractViolationException exception = Assert.Throws<AsiBackboneContractViolationException>(
            () => AsiBackboneDecisionContract.VerifySafeDecision(decision, "null-reason decision"));

        Assert.Contains("null reason", exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void VerifySafeDecisionRejectsBlankReasonMembers(bool blankCode)
    {
        OperationReason reason = CreateMalformedReason(
            blankCode ? " " : "contract.reason",
            blankCode ? "Reason message." : " ");
        GovernanceDecision decision = GovernanceDecision.Warning("contract.warning", "Warning.");
        SetAutoProperty(decision, nameof(GovernanceDecision.Reasons), new[] { reason });

        AsiBackboneContractViolationException exception = Assert.Throws<AsiBackboneContractViolationException>(
            () => AsiBackboneDecisionContract.VerifySafeDecision(decision, "malformed-reason decision"));

        Assert.Contains(blankCode ? "empty code" : "empty message", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void VerifyInvalidCapabilityGrantRejectsAllow()
    {
        AsiBackboneContractViolationException exception = Assert.Throws<AsiBackboneContractViolationException>(
            () => AsiBackboneDecisionContract.VerifyInvalidCapabilityGrantDoesNotAllow(GovernanceDecision.Allow()));

        Assert.Contains("must not return Allow", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void VerifyTelemetryRejectsCorrelationMismatch()
    {
        var context = new AsiBackboneConstraintEvaluationContext(correlationId: "expected-correlation");
        GovernanceDecision decision = GovernanceDecision.Allow(correlationId: "different-correlation");

        AsiBackboneContractViolationException exception = Assert.Throws<AsiBackboneContractViolationException>(
            () => AsiBackboneDecisionContract.VerifyTelemetryFromContext(decision, context));

        Assert.Contains("preserve the supplied correlation ID", exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void VerifyTelemetryRejectsMissingRequiredPolicyTelemetry(bool requireVersion)
    {
        var context = new AsiBackboneConstraintEvaluationContext(
            policyVersion: requireVersion ? "policy-v1" : null,
            policyHash: requireVersion ? null : "policy-hash");

        AsiBackboneContractViolationException exception = Assert.Throws<AsiBackboneContractViolationException>(
            () => AsiBackboneDecisionContract.VerifyTelemetryFromContext(GovernanceDecision.Allow(), context));

        Assert.Contains(requireVersion ? "policy version" : "policy hash", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void VerifyTelemetryAcceptsAbsentOptionalTelemetry()
    {
        var context = new AsiBackboneConstraintEvaluationContext();
        GovernanceDecision decision = GovernanceDecision.Allow();

        GovernanceDecision verified = AsiBackboneDecisionContract.VerifyTelemetryFromContext(decision, context);

        Assert.Same(decision, verified);
    }

    [Fact]
    public void VerifyTelemetryRejectsNullContext()
    {
        _ = Assert.Throws<ArgumentNullException>(() =>
            AsiBackboneDecisionContract.VerifyTelemetryFromContext<AsiBackboneConstraintEvaluationContext>(
                GovernanceDecision.Allow(),
                null!));
    }

    [Fact]
    public void VerifyAuditResidueRejectsNull()
    {
        AsiBackboneContractViolationException exception = Assert.Throws<AsiBackboneContractViolationException>(
            () => AsiBackboneDecisionContract.VerifyAuditResidue(null));

        Assert.Contains("must not be null", exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(nameof(TestAuditResidue.EventId), "event ID")]
    [InlineData(nameof(TestAuditResidue.ActorId), "actor ID")]
    [InlineData(nameof(TestAuditResidue.OperationName), "operation name")]
    [InlineData(nameof(TestAuditResidue.Outcome), "outcome")]
    public void VerifyAuditResidueRejectsMissingRequiredStrings(string propertyName, string expectedMessagePart)
    {
        var residue = new TestAuditResidue();
        typeof(TestAuditResidue).GetProperty(propertyName)!.SetValue(residue, " ");

        AsiBackboneContractViolationException exception = Assert.Throws<AsiBackboneContractViolationException>(
            () => AsiBackboneDecisionContract.VerifyAuditResidue(residue));

        Assert.Contains(expectedMessagePart, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void VerifyAuditResidueRejectsNullReasonCodes()
    {
        var residue = new TestAuditResidue { ReasonCodes = null! };

        AsiBackboneContractViolationException exception = Assert.Throws<AsiBackboneContractViolationException>(
            () => AsiBackboneDecisionContract.VerifyAuditResidue(residue));

        Assert.Contains("reason-code collection", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void VerifyAuditResidueRejectsBlankReasonCode()
    {
        var residue = new TestAuditResidue { ReasonCodes = new[] { "contract.reason", " " } };

        AsiBackboneContractViolationException exception = Assert.Throws<AsiBackboneContractViolationException>(
            () => AsiBackboneDecisionContract.VerifyAuditResidue(residue));

        Assert.Contains("empty reason code at index 1", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void VerifyAuditResidueRejectsNullMetadata()
    {
        var residue = new TestAuditResidue { Metadata = null! };

        AsiBackboneContractViolationException exception = Assert.Throws<AsiBackboneContractViolationException>(
            () => AsiBackboneDecisionContract.VerifyAuditResidue(residue));

        Assert.Contains("metadata collection", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void VerifyAuditResidueReturnsOriginalValidResidue()
    {
        var residue = new TestAuditResidue();

        IAsiBackboneAuditResidue verified = AsiBackboneDecisionContract.VerifyAuditResidue(residue);

        Assert.Same(residue, verified);
    }

    private static OperationReason CreateMalformedReason(string code, string message)
    {
        var reason = (OperationReason)RuntimeHelpers.GetUninitializedObject(typeof(OperationReason));
        SetAutoProperty(reason, nameof(OperationReason.Code), code);
        SetAutoProperty(reason, nameof(OperationReason.Message), message);
        SetAutoProperty(
            reason,
            nameof(OperationReason.Metadata),
            new Dictionary<string, string>(StringComparer.Ordinal));
        return reason;
    }

    private static void SetAutoProperty<TTarget, TValue>(TTarget target, string propertyName, TValue value)
    {
        FieldInfo field = typeof(TTarget).GetField(
            $"<{propertyName}>k__BackingField",
            BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"Backing field for '{propertyName}' was not found.");
        field.SetValue(target, value);
    }

    private sealed class TestAuditResidue : IAsiBackboneAuditResidue
    {
        public string EventId { get; set; } = "contract-event";
        public DateTimeOffset OccurredUtc { get; set; } = DateTimeOffset.UtcNow;
        public string ActorId { get; set; } = "contract-actor";
        public AsiBackboneActorType ActorType { get; set; } = AsiBackboneActorType.System;
        public string? ActorDisplayName { get; set; } = "Contract Actor";
        public string OperationName { get; set; } = "contract.operation";
        public string Outcome { get; set; } = "Allowed";
        public IReadOnlyList<string> ReasonCodes { get; set; } = Array.Empty<string>();
        public string? CorrelationId { get; set; } = "contract-correlation";
        public string? TraceId { get; set; } = "contract-trace";
        public string? PolicyVersion { get; set; } = "contract-policy-v1";
        public string? PolicyHash { get; set; } = "contract-policy-hash";
        public IReadOnlyDictionary<string, string> Metadata { get; set; } =
            new Dictionary<string, string>(StringComparer.Ordinal);
    }
}
