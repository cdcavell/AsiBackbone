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

        GovernanceMetadataBudgetValidationResult result = GovernanceMetadataBudgetValidator.Validate(metadata);

        Assert.True(result.IsValid);
        _ = Assert.Single(result.NormalizedMetadata);
        Assert.Equal("approval.execute", result.NormalizedMetadata["operation.name"]);
        Assert.True(result.EstimatedSerializedBytes > 0);
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

        GovernanceMetadataBudgetValidationResult result = GovernanceMetadataBudgetValidator.Validate(metadata, budget);

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

        GovernanceMetadataBudgetValidationResult result = GovernanceMetadataBudgetValidator.Validate(metadata);

        Assert.False(result.IsValid);
        Assert.Contains(result.Violations, violation => violation.Contains("reserved or discouraged key fragment 'apikey'", StringComparison.Ordinal));
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

        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => GovernanceMetadataBudgetValidator.NormalizeAndValidate(metadata, budget));

        Assert.Contains("Metadata budget validation failed", exception.Message, StringComparison.Ordinal);
    }
}
