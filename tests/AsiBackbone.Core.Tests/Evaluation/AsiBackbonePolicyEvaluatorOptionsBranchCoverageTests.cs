using AsiBackbone.Core.Evaluation;
using Xunit;

namespace AsiBackbone.Core.Tests.Evaluation;

/// <summary>
/// Targeted branch coverage for evaluator option validation.
/// </summary>
public sealed class AsiBackbonePolicyEvaluatorOptionsBranchCoverageTests
{
    [Fact]
    public void ValidateDefaultOptionsSucceeds()
    {
        var options = new AsiBackbonePolicyEvaluatorOptions();

        options.Validate();
    }

    [Fact]
    public void ValidateRejectsBlankNoConstraintsReasonCode()
    {
        var options = new AsiBackbonePolicyEvaluatorOptions
        {
            NoConstraintsReasonCode = " "
        };

        _ = Assert.Throws<InvalidOperationException>(options.Validate);
    }

    [Fact]
    public void ValidateRejectsBlankNoConstraintsReasonMessage()
    {
        var options = new AsiBackbonePolicyEvaluatorOptions
        {
            NoConstraintsReasonMessage = " "
        };

        _ = Assert.Throws<InvalidOperationException>(options.Validate);
    }

    [Fact]
    public void ValidateRejectsBlankConstraintExceptionReasonCode()
    {
        var options = new AsiBackbonePolicyEvaluatorOptions
        {
            ConstraintExceptionReasonCode = " "
        };

        _ = Assert.Throws<InvalidOperationException>(options.Validate);
    }

    [Fact]
    public void ValidateRejectsBlankConstraintExceptionReasonMessage()
    {
        var options = new AsiBackbonePolicyEvaluatorOptions
        {
            ConstraintExceptionReasonMessage = " "
        };

        _ = Assert.Throws<InvalidOperationException>(options.Validate);
    }

    [Fact]
    public void ValidateRejectsBlankThreatContributorExceptionReasonCode()
    {
        var options = new AsiBackbonePolicyEvaluatorOptions
        {
            ThreatContributorExceptionReasonCode = " "
        };

        _ = Assert.Throws<InvalidOperationException>(options.Validate);
    }

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
