using AsiBackbone.Core.Evaluation;
using Xunit;

namespace AsiBackbone.Core.Tests.Evaluation;

/// <summary>
/// Targeted branch coverage for evaluator option validation.
/// </summary>
public sealed class AsiBackbonePolicyEvaluatorOptionsBranchCoverageTests
{
    /// <summary>
    /// Validates that the default options pass validation and preserve the 3.x fail-closed evaluator posture.
    /// </summary>
    [Fact]
    public void ValidateDefaultOptionsSucceeds()
    {
        var options = new AsiBackbonePolicyEvaluatorOptions();

        options.Validate();

        Assert.True(options.DenyWhenNoConstraints);
        Assert.True(options.TreatConstraintExceptionAsDenial);
        Assert.True(options.TreatThreatContributorExceptionAsDenial);
        Assert.True(options.PreventThreatAssessmentAllowDowngrade);
        Assert.False(options.ShortCircuitOnFirstDenial);
    }

    /// <summary>
    /// Validates that blank reason codes and messages are rejected by validation.
    /// </summary>
    [Fact]
    public void ValidateRejectsBlankNoConstraintsReasonCode()
    {
        var options = new AsiBackbonePolicyEvaluatorOptions
        {
            NoConstraintsReasonCode = " "
        };

        _ = Assert.Throws<InvalidOperationException>(options.Validate);
    }

    /// <summary>
    /// Validates that blank reason messages are rejected by validation.
    /// </summary>
    [Fact]
    public void ValidateRejectsBlankNoConstraintsReasonMessage()
    {
        var options = new AsiBackbonePolicyEvaluatorOptions
        {
            NoConstraintsReasonMessage = " "
        };

        _ = Assert.Throws<InvalidOperationException>(options.Validate);
    }

    /// <summary>
    /// Validates that blank constraint exception reason codes are rejected by validation.
    /// </summary>
    [Fact]
    public void ValidateRejectsBlankConstraintExceptionReasonCode()
    {
        var options = new AsiBackbonePolicyEvaluatorOptions
        {
            ConstraintExceptionReasonCode = " "
        };

        _ = Assert.Throws<InvalidOperationException>(options.Validate);
    }

    /// <summary>
    /// Validates that blank constraint exception reason messages are rejected by validation.
    /// </summary>  
    [Fact]
    public void ValidateRejectsBlankConstraintExceptionReasonMessage()
    {
        var options = new AsiBackbonePolicyEvaluatorOptions
        {
            ConstraintExceptionReasonMessage = " "
        };

        _ = Assert.Throws<InvalidOperationException>(options.Validate);
    }

    /// <summary>
    /// Validates that blank threat contributor exception reason codes are rejected by validation.
    /// </summary>
    [Fact]
    public void ValidateRejectsBlankThreatContributorExceptionReasonCode()
    {
        var options = new AsiBackbonePolicyEvaluatorOptions
        {
            ThreatContributorExceptionReasonCode = " "
        };

        _ = Assert.Throws<InvalidOperationException>(options.Validate);
    }

    /// <summary>
    /// Validates that blank threat contributor exception reason messages are rejected by validation.
    /// </summary>
    [Fact]
    public void ValidateRejectsBlankThreatContributorExceptionReasonMessage()
    {
        var options = new AsiBackbonePolicyEvaluatorOptions
        {
            ThreatContributorExceptionReasonMessage = " "
        };

        _ = Assert.Throws<InvalidOperationException>(options.Validate);
    }
}
