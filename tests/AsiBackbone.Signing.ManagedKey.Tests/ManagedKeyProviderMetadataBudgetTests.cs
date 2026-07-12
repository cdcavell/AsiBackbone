using Xunit;

namespace AsiBackbone.Signing.ManagedKey.Tests;

/// <summary>
/// Covers provider metadata count and aggregate-size limits.
/// </summary>
public sealed class ManagedKeyProviderMetadataBudgetTests
{
    /// <summary>
    /// Verifies the complete allowlist remains within the configured count and aggregate budgets.
    /// </summary>
    [Fact]
    public void FilterBoundsCompleteAllowlist()
    {
        string maximumValue = new('v', ManagedKeyProviderMetadataFilter.MaxValueLength);
        var source = new Dictionary<string, string>
        {
            ["provider_region"] = maximumValue,
            ["provider_zone"] = maximumValue,
            ["provider_service"] = maximumValue,
            ["provider_request_id"] = maximumValue,
            ["provider_status_code"] = maximumValue,
            ["provider_key_state"] = maximumValue
        };

        IReadOnlyDictionary<string, string> result = ManagedKeyProviderMetadataFilter.Filter(source);
        int aggregateLength = result.Sum(item => item.Key.Length + item.Value.Length);

        Assert.True(result.Count <= ManagedKeyProviderMetadataFilter.MaxMetadataCount);
        Assert.True(aggregateLength <= ManagedKeyProviderMetadataFilter.MaxAggregateLength);
        Assert.All(result, item => Assert.True(item.Value.Length <= ManagedKeyProviderMetadataFilter.MaxValueLength));
    }
}
