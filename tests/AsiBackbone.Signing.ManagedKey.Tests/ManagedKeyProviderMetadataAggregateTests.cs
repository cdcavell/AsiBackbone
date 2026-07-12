using Xunit;

namespace AsiBackbone.Signing.ManagedKey.Tests;

/// <summary>
/// Covers aggregate provider metadata budget behavior.
/// </summary>
public sealed class ManagedKeyProviderMetadataAggregateTests
{
    /// <summary>
    /// Verifies entries that would exceed the aggregate budget are omitted without mutating earlier entries.
    /// </summary>
    [Fact]
    public void FilterEnforcesAggregateLengthBudget()
    {
        string maximumValue = new('v', ManagedKeyProviderMetadataFilter.MaxValueLength);
        var source = new Dictionary<string, string>
        {
            ["provider_region"] = maximumValue,
            ["provider_zone"] = maximumValue,
            ["provider_service"] = maximumValue,
            ["provider_request_id"] = maximumValue,
            ["provider_status_code"] = "200"
        };

        IReadOnlyDictionary<string, string> result = ManagedKeyProviderMetadataFilter.Filter(source);

        Assert.Equal(3, result.Count);
        Assert.Contains("provider_region", result.Keys);
        Assert.Contains("provider_zone", result.Keys);
        Assert.Contains("provider_service", result.Keys);
        Assert.DoesNotContain("provider_request_id", result.Keys);
        Assert.Contains("provider_status_code", result.Keys);
    }
}
