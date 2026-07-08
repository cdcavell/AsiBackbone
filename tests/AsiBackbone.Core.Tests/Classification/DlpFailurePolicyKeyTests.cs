using AsiBackbone.Core.Classification;
using Xunit;

namespace AsiBackbone.Core.Tests.Classification;

/// <summary>
/// Line coverage tests for <see cref="DlpFailurePolicyKey"/>.
/// </summary>
public sealed class DlpFailurePolicyKeyTests
{
    /// <summary>
    /// Verifies that the record struct exposes the risk level and failure kind supplied to its primary constructor.
    /// </summary>
    [Fact]
    public void ConstructorAssignsRiskLevelAndFailureKind()
    {
        var key = new DlpFailurePolicyKey(
            DlpIntentRiskLevel.High,
            DlpClassificationFailureKind.Timeout);

        Assert.Equal(DlpIntentRiskLevel.High, key.RiskLevel);
        Assert.Equal(DlpClassificationFailureKind.Timeout, key.FailureKind);
    }
}
