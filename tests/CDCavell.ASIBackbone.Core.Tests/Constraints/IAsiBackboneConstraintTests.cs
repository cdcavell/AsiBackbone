using CDCavell.AsiBackbone.Core.Constraints;
using Xunit;

namespace CDCavell.AsiBackbone.Core.Tests.Constraints;

/// <summary>
/// Tests for <see cref="IAsiBackboneConstraint{TContext}"/> implementations.
/// </summary>
public sealed class IAsiBackboneConstraintTests
{
    /// <summary>
    /// Verifies that a constraint can evaluate a typed context and return the expected result.
    /// </summary>
    /// <returns>
    /// A task that represents the asynchronous test operation. The task result is not used.
    /// </returns>
    [Fact]
    public async Task ConstraintCanEvaluateTypedContext()
    {
        var constraint = new TestConstraint();
        var context = new TestContext("document.approve");

        ConstraintEvaluationResult result = await constraint.EvaluateAsync(context, Xunit.TestContext.Current.CancellationToken);

        Assert.Equal("test.constraint", constraint.Name);
        Assert.True(result.CanProceed);
        Assert.Equal(ConstraintEvaluationOutcome.Allowed, result.Outcome);
    }

    private sealed record TestContext(string IntentName);

    private sealed class TestConstraint : IAsiBackboneConstraint<TestContext>
    {
        public string Name => "test.constraint";

        public ValueTask<ConstraintEvaluationResult> EvaluateAsync(
            TestContext context,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(context);

            return context.IntentName == "document.approve"
                ? ValueTask.FromResult(ConstraintEvaluationResult.Allow())
                : ValueTask.FromResult(ConstraintEvaluationResult.NotApplicable());
        }
    }
}
