using CDCavell.ASIBackbone.Core.Constraints;
using CDCavell.ASIBackbone.Core.Results;
using Xunit;

namespace CDCavell.ASIBackbone.Core.Tests.Constraints;

/// <summary>
/// Unit tests for the <see cref="ConstraintEvaluationResult"/> class, verifying the correctness of factory methods and properties for different constraint evaluation outcomes.
/// </summary>
public sealed class ConstraintEvaluationResultTests
{
    /// <summary>
    /// Verifies that the Allow factory method creates a result with the expected properties for an allowed outcome.
    /// </summary>
    [Fact]
    public void AllowCreatesAllowedResult()
    {
        var result = ConstraintEvaluationResult.Allow();

        Assert.Equal(ConstraintEvaluationOutcome.Allowed, result.Outcome);
        Assert.True(result.CanProceed);
        Assert.False(result.IsDenied);
        Assert.False(result.HasReasons);
        Assert.Empty(result.Reasons);
        Assert.Empty(result.ReasonCodes);
    }

    /// <summary>
    /// Verifies that the Deny factory method creates a result with the expected properties for a denied outcome, including the provided reason code and message.
    /// </summary>
    [Fact]
    public void DenyCreatesDeniedResultWithReasonCode()
    {
        var result = ConstraintEvaluationResult.Deny(
            "constraint.role_required",
            "Required role is missing.");

        Assert.Equal(ConstraintEvaluationOutcome.Denied, result.Outcome);
        Assert.False(result.CanProceed);
        Assert.True(result.IsDenied);

        OperationReason reason = Assert.Single(result.Reasons);
        Assert.Equal("constraint.role_required", reason.Code);
        Assert.Equal("Required role is missing.", reason.Message);
        Assert.Equal("constraint.role_required", Assert.Single(result.ReasonCodes));
    }

    /// <summary>
    /// Verifies that the Deny factory method creates a result with the expected properties for a denied outcome when no reasons are provided.
    /// </summary>
    [Fact]
    public void DenyWithNoReasonsUsesDefaultDeniedReason()
    {
        var result = ConstraintEvaluationResult.Deny([]);

        Assert.True(result.IsDenied);

        OperationReason reason = Assert.Single(result.Reasons);
        Assert.Equal("constraint.denied", reason.Code);
        Assert.Equal("Constraint denied the operation.", reason.Message);
    }

    /// <summary>
    /// Verifies that the Warning factory method creates a result with the expected properties for a warning outcome, including the provided reason code and message.
    /// </summary>
    [Fact]
    public void WarningCreatesWarningResultWithReasonCode()
    {
        var result = ConstraintEvaluationResult.Warning(
            "constraint.high_risk",
            "Operation is allowed but high risk.");

        Assert.Equal(ConstraintEvaluationOutcome.Warning, result.Outcome);
        Assert.True(result.CanProceed);
        Assert.True(result.IsWarning);

        OperationReason reason = Assert.Single(result.Reasons);
        Assert.Equal("constraint.high_risk", reason.Code);
        Assert.Equal("Operation is allowed but high risk.", reason.Message);
        Assert.Equal("constraint.high_risk", Assert.Single(result.ReasonCodes));
    }

    /// <summary>
    /// Verifies that the Warning factory method creates a result with the expected properties for a warning outcome when no reasons are provided.
    /// </summary>
    [Fact]
    public void WarningWithNoReasonsUsesDefaultWarningReason()
    {
        var result = ConstraintEvaluationResult.Warning([]);

        Assert.True(result.IsWarning);

        OperationReason reason = Assert.Single(result.Reasons);
        Assert.Equal("constraint.warning", reason.Code);
        Assert.Equal("Constraint produced a warning.", reason.Message);
    }

    /// <summary>
    /// Verifies that the NotApplicable factory method creates a result with the expected properties for a not applicable outcome, indicating that the constraint does not apply to the evaluated operation.
    /// </summary>
    [Fact]
    public void NotApplicableCreatesNonBlockingResult()
    {
        var result = ConstraintEvaluationResult.NotApplicable();

        Assert.Equal(ConstraintEvaluationOutcome.NotApplicable, result.Outcome);
        Assert.True(result.CanProceed);
        Assert.True(result.IsNotApplicable);
        Assert.Empty(result.Reasons);
        Assert.Empty(result.ReasonCodes);
    }
}
