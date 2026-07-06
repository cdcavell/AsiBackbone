using AsiBackbone.Core.Metadata;
using Xunit;

namespace AsiBackbone.Core.Tests.Metadata;

public sealed class GovernanceMetadataBudgetValidatorTests
{
    [Fact]
    public void ValidateNormalizesMetadataAndDropsBlankKeys()
    {
        var metadata = new Dictionary<string, string>
        {
            [" operation.name "] = " approval.execute ",
            [" "] = " ignored "
        };

        var result = GovernanceMetadataBudgetValidator.Validate(metadata);

        Assert.True(result.IsValid);
        _ = Assert.Single(result.NormalizedMetadata);
        Assert.Equal("approval.execute", result.NormalizedMetadata["operation.name"]);
        Assert.True(result.EstimatedSerializedBytes > 0);
    }

    [Fact]
    public void ValidateReturnsValidEmptyResultForNullMetadata()
    {
        var result = GovernanceMetadataBudgetValidator.Validate(null);

        Assert.True(result.IsValid);
        Assert.Empty(result.NormalizedMetadata);
        Assert.Empty(result.Violations);
        Assert.Equal(2, result.EstimatedSerializedBytes);
    }

    [Fact]
    public void NormalizeAndValidateReturnsNormalizedMetadataWhenBudgetPasses()
    {
        var metadata = new Dictionary<string, string>
        {
            [" region "] = " US-LA "
        };

        var normalizedMetadata = GovernanceMetadataBudgetValidator.NormalizeAndValidate(metadata);

        _ = Assert.Single(normalizedMetadata);
        Assert.Equal("US-LA", normalizedMetadata["region"]);
    }

    [Fact]
    public void EstimateSerializedSizeBytesNormalizesMetadataBeforeCounting()
    {
        var metadata = new Dictionary<string, string>
        {
            [" key "] = " value ",
            [" "] = " ignored "
        };

        int estimatedBytes = GovernanceMetadataBudgetValidator.EstimateSerializedSizeBytes(metadata);

        Assert.Equal(16, estimatedBytes);
    }

    [Fact]
    public void ValidateReportsCountLengthValueAndSerializedSizeViolations()
    {
        var budget = GovernanceMetadataBudget.Create(
            maxCount: 1,
            maxKeyLength: 8,
            maxValueLength: 4,
            maxSerializedBytes: 10,
            reservedKeyFragments: []);

        var metadata = new Dictionary<string, string>
        {
            ["long.metadata.key"] = "abcde",
            ["region"] = "US-LA"
        };

        var result = GovernanceMetadataBudgetValidator.Validate(metadata, budget);

        Assert.False(result.IsValid);
        Assert.Contains(result.Violations, violation => violation.Contains("maximum metadata count", StringComparison.Ordinal));
        Assert.Contains(result.Violations, violation => violation.Contains("maximum key length", StringComparison.Ordinal));
        Assert.Contains(result.Violations, violation => violation.Contains("maximum value length", StringComparison.Ordinal));
        Assert.Contains(result.Violations, violation => violation.Contains("maximum serialized metadata size", StringComparison.Ordinal));
    }

    [Fact]
    public void ValidateReportsReservedSensitiveKeyFragments()
    {
        var metadata = new Dictionary<string, string>
        {
            ["api_key"] = "sk-test-value"
        };

        var result = GovernanceMetadataBudgetValidator.Validate(metadata);

        Assert.False(result.IsValid);
        Assert.Contains(result.Violations, violation => violation.Contains("reserved or discouraged key fragment 'apikey'", StringComparison.Ordinal));
    }

    [Fact]
    public void ValidateAllowsReservedLookingKeysWhenReservedFragmentsAreEmpty()
    {
        var budget = GovernanceMetadataBudget.Create(reservedKeyFragments: []);
        var metadata = new Dictionary<string, string>
        {
            ["api_key"] = "opaque-reference-only"
        };

        var result = GovernanceMetadataBudgetValidator.Validate(metadata, budget);

        Assert.True(result.IsValid);
        Assert.Empty(result.Violations);
    }

    [Fact]
    public void CreateNormalizesReservedKeyFragments()
    {
        var budget = GovernanceMetadataBudget.Create(
            reservedKeyFragments:
            [
                " api-key ",
                "api_key",
                " ",
                "token"
            ]);

        Assert.Equal(["apikey", "token"], budget.ReservedKeyFragments);
    }

    [Fact]
    public void CreateRejectsInvalidBudgetLimits()
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => GovernanceMetadataBudget.Create(maxCount: 0));

        Assert.Equal("maxCount", exception.ParamName);
    }

    [Fact]
    public void NormalizeAndValidateThrowsWhenBudgetFails()
    {
        var budget = GovernanceMetadataBudget.Create(
            maxCount: 1,
            reservedKeyFragments: []);

        var metadata = new Dictionary<string, string>
        {
            ["one"] = "1",
            ["two"] = "2"
        };

        var exception = Assert.Throws<ArgumentException>(
            () => GovernanceMetadataBudgetValidator.NormalizeAndValidate(metadata, budget));

        Assert.Contains("Metadata budget validation failed", exception.Message, StringComparison.Ordinal);
    }
}
