using System.Collections;
using AsiBackbone.Core.Decisions;
using Xunit;

namespace AsiBackbone.Core.Tests.Decisions;

/// <summary>
/// Tests collection-backed governance reason normalization paths.
/// </summary>
public sealed class GovernanceDecisionReasonNormalizationTests
{
    /// <summary>
    /// Verifies empty collection-backed denied reasons use the fallback without enumeration.
    /// </summary>
    [Fact]
    public void DenyWithEmptyReasonCollectionUsesDefaultReasonWithoutEnumeration()
    {
        var reasons = new EnumerationCountingReasonCollection();

        var decision = GovernanceDecision.Deny(reasons);

        Assert.True(decision.IsDenied);
        Assert.Equal(0, reasons.EnumerationCount);

        OperationReason reason = Assert.Single(decision.Reasons);
        Assert.Equal("decision.denied", reason.Code);
        Assert.Equal("Decision denied the operation.", reason.Message);
    }

    /// <summary>
    /// Verifies a single collection-backed reason is retained after one enumeration.
    /// </summary>
    [Fact]
    public void DenyWithSingleReasonCollectionStoresReasonCodeAfterOneEnumeration()
    {
        var reasons = new EnumerationCountingReasonCollection(
            OperationReason.Create("policy.single", "Single policy failure."));

        var decision = GovernanceDecision.Deny(reasons);

        Assert.True(decision.IsDenied);
        Assert.Equal(1, reasons.EnumerationCount);
        Assert.Equal("policy.single", Assert.Single(decision.ReasonCodes));
    }

    /// <summary>
    /// Verifies multiple collection-backed reasons are retained after one enumeration.
    /// </summary>
    [Fact]
    public void DenyWithMultipleReasonCollectionStoresReasonCodesAfterOneEnumeration()
    {
        var reasons = new EnumerationCountingReasonCollection(
            OperationReason.Create("policy.first", "First policy failure."),
            OperationReason.Create("policy.second", "Second policy failure."));

        var decision = GovernanceDecision.Deny(reasons);

        Assert.True(decision.IsDenied);
        Assert.Equal(1, reasons.EnumerationCount);
        Assert.Equal(["policy.first", "policy.second"], decision.ReasonCodes);
    }

    /// <summary>
    /// Verifies mixed collection-backed reason entries are filtered after one enumeration.
    /// </summary>
    [Fact]
    public void DenyWithReasonCollectionContainingNullFiltersNullReasonsAfterOneEnumeration()
    {
        var reasons = new EnumerationCountingReasonCollection(
            OperationReason.Create("policy.first", "First policy failure."),
            null,
            OperationReason.Create("policy.second", "Second policy failure."));

        var decision = GovernanceDecision.Deny(reasons);

        Assert.True(decision.IsDenied);
        Assert.Equal(1, reasons.EnumerationCount);
        Assert.Equal(["policy.first", "policy.second"], decision.ReasonCodes);
    }

    /// <summary>
    /// Verifies all-null collection-backed warning reasons use the fallback after one enumeration.
    /// </summary>
    [Fact]
    public void WarningWithAllNullReasonCollectionUsesDefaultReasonAfterOneEnumeration()
    {
        var reasons = new EnumerationCountingReasonCollection(null, null);

        var decision = GovernanceDecision.Warning(reasons);

        Assert.True(decision.IsWarning);
        Assert.Equal(1, reasons.EnumerationCount);

        OperationReason reason = Assert.Single(decision.Reasons);
        Assert.Equal("decision.warning", reason.Code);
        Assert.Equal("Decision produced a warning.", reason.Message);
    }

    private sealed class EnumerationCountingReasonCollection : ICollection<OperationReason>
    {
        private readonly OperationReason?[] reasons;

        internal EnumerationCountingReasonCollection(params OperationReason?[] reasons)
        {
            this.reasons = reasons;
        }

        public int EnumerationCount { get; private set; }

        public int Count => reasons.Length;

        public bool IsReadOnly => true;

        public void Add(OperationReason item)
        {
            throw new NotSupportedException();
        }

        public void Clear()
        {
            throw new NotSupportedException();
        }

        public bool Contains(OperationReason item)
        {
            return Array.IndexOf(reasons, item) >= 0;
        }

        public void CopyTo(OperationReason[] array, int arrayIndex)
        {
            for (int index = 0; index < reasons.Length; index++)
            {
                array[arrayIndex + index] = reasons[index]!;
            }
        }

        public bool Remove(OperationReason item)
        {
            throw new NotSupportedException();
        }

        public IEnumerator<OperationReason> GetEnumerator()
        {
            EnumerationCount++;

            foreach (OperationReason? reason in reasons)
            {
                yield return reason!;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
