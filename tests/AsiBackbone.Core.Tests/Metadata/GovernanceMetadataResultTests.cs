using AsiBackbone.Core.Metadata;
using AsiBackbone.Core.Results;
using Xunit;

namespace AsiBackbone.Core.Tests.Metadata;

/// <summary>
/// Unit tests for provider-neutral governance metadata result and context types.
/// </summary>
public sealed class GovernanceMetadataResultTests
{
    [Fact]
    public void ClassificationAllowHasNoReasonOrReplacement()
    {
        GovernanceMetadataClassificationResult result = GovernanceMetadataClassificationResult.Allow();

        Assert.Equal(GovernanceMetadataSanitizationAction.Allow, result.Action);
        Assert.Null(result.Reason);
        Assert.Null(result.ReplacementValue);
    }

    [Theory]
    [InlineData(GovernanceMetadataSanitizationAction.Warn)]
    [InlineData(GovernanceMetadataSanitizationAction.Drop)]
    [InlineData(GovernanceMetadataSanitizationAction.Deny)]
    public void ClassificationFactoriesPreserveReason(GovernanceMetadataSanitizationAction action)
    {
        GovernanceMetadataClassificationResult result = action switch
        {
            GovernanceMetadataSanitizationAction.Warn => GovernanceMetadataClassificationResult.Warn(" metadata.warn ", " Warning message. "),
            GovernanceMetadataSanitizationAction.Drop => GovernanceMetadataClassificationResult.Drop(" metadata.drop ", " Drop message. "),
            GovernanceMetadataSanitizationAction.Deny => GovernanceMetadataClassificationResult.Deny(" metadata.deny ", " Deny message. "),
            _ => throw new InvalidOperationException("Unsupported test action.")
        };

        Assert.Equal(action, result.Action);
        Assert.NotNull(result.Reason);
        Assert.Equal($"metadata.{action.ToString().ToLowerInvariant()}", result.Reason.Code);
        Assert.Equal($"{action} message.", result.Reason.Message);
        Assert.Null(result.ReplacementValue);
    }

    [Fact]
    public void RedactUsesDefaultReplacementWhenOmitted()
    {
        GovernanceMetadataClassificationResult result = GovernanceMetadataClassificationResult.Redact(
            "metadata.redact",
            "Redact message.");

        Assert.Equal(GovernanceMetadataSanitizationAction.Redact, result.Action);
        Assert.Equal(GovernanceMetadataClassificationResult.DefaultRedactedValue, result.ReplacementValue);
        Assert.Equal("metadata.redact", result.Reason?.Code);
        Assert.Equal("Redact message.", result.Reason?.Message);
    }

    [Fact]
    public void RedactPreservesCustomReplacement()
    {
        GovernanceMetadataClassificationResult result = GovernanceMetadataClassificationResult.Redact(
            "metadata.redact",
            "Redact message.",
            "safe-value");

        Assert.Equal("safe-value", result.ReplacementValue);
    }

    [Fact]
    public void RedactRejectsNullReplacement()
    {
        _ = Assert.Throws<ArgumentNullException>(() => GovernanceMetadataClassificationResult.Redact(
            "metadata.redact",
            "Redact message.",
            null!));
    }

    [Theory]
    [InlineData(GovernanceMetadataSanitizationAction.Warn, null, "message")]
    [InlineData(GovernanceMetadataSanitizationAction.Warn, "", "message")]
    [InlineData(GovernanceMetadataSanitizationAction.Drop, "code", " ")]
    [InlineData(GovernanceMetadataSanitizationAction.Deny, " ", "message")]
    [InlineData(GovernanceMetadataSanitizationAction.Redact, "code", null)]
    public void ClassificationFactoriesRejectInvalidReasons(
        GovernanceMetadataSanitizationAction action,
        string? code,
        string? message)
    {
        _ = Assert.ThrowsAny<ArgumentException>(() => action switch
        {
            GovernanceMetadataSanitizationAction.Warn => GovernanceMetadataClassificationResult.Warn(code!, message!),
            GovernanceMetadataSanitizationAction.Redact => GovernanceMetadataClassificationResult.Redact(code!, message!),
            GovernanceMetadataSanitizationAction.Drop => GovernanceMetadataClassificationResult.Drop(code!, message!),
            GovernanceMetadataSanitizationAction.Deny => GovernanceMetadataClassificationResult.Deny(code!, message!),
            _ => throw new InvalidOperationException("Unsupported test action.")
        });
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ClassificationContextRejectsInvalidKeys(string? key)
    {
        _ = Assert.ThrowsAny<ArgumentException>(() => new GovernanceMetadataClassificationContext(
            key!,
            "value",
            new Dictionary<string, string>()));
    }

    [Fact]
    public void ClassificationContextRejectsNullValue()
    {
        _ = Assert.Throws<ArgumentNullException>(() => new GovernanceMetadataClassificationContext(
            "key",
            null!,
            new Dictionary<string, string>()));
    }

    [Fact]
    public void ClassificationContextRejectsNullMetadata()
    {
        _ = Assert.Throws<ArgumentNullException>(() => new GovernanceMetadataClassificationContext(
            "key",
            "value",
            null!));
    }

    [Fact]
    public void ClassificationContextPreservesSuppliedValuesAndReference()
    {
        IReadOnlyDictionary<string, string> metadata = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["key"] = "value"
        };

        var context = new GovernanceMetadataClassificationContext("key", "value", metadata);

        Assert.Equal("key", context.Key);
        Assert.Equal("value", context.Value);
        Assert.Same(metadata, context.Metadata);
    }

    [Fact]
    public void SanitationResultRejectsUndefinedAction()
    {
        _ = Assert.Throws<ArgumentOutOfRangeException>(() => GovernanceMetadataSanitizationResult.Create(
            (GovernanceMetadataSanitizationAction)999,
            new Dictionary<string, string>(),
            Array.Empty<OperationReason>(),
            CreateValidBudgetValidation()));
    }

    [Fact]
    public void SanitationResultRejectsNullArguments()
    {
        _ = Assert.Throws<ArgumentNullException>(() => GovernanceMetadataSanitizationResult.Create(
            GovernanceMetadataSanitizationAction.Allow,
            null!,
            Array.Empty<OperationReason>(),
            CreateValidBudgetValidation()));

        _ = Assert.Throws<ArgumentNullException>(() => GovernanceMetadataSanitizationResult.Create(
            GovernanceMetadataSanitizationAction.Allow,
            new Dictionary<string, string>(),
            null!,
            CreateValidBudgetValidation()));

        _ = Assert.Throws<ArgumentNullException>(() => GovernanceMetadataSanitizationResult.Create(
            GovernanceMetadataSanitizationAction.Allow,
            new Dictionary<string, string>(),
            Array.Empty<OperationReason>(),
            null!));
    }

    [Theory]
    [InlineData(GovernanceMetadataSanitizationAction.Allow, true, false)]
    [InlineData(GovernanceMetadataSanitizationAction.Warn, true, false)]
    [InlineData(GovernanceMetadataSanitizationAction.Redact, true, false)]
    [InlineData(GovernanceMetadataSanitizationAction.Drop, true, false)]
    [InlineData(GovernanceMetadataSanitizationAction.Deny, false, true)]
    public void SanitationResultReportsContinuationState(
        GovernanceMetadataSanitizationAction action,
        bool canProceed,
        bool isDenied)
    {
        GovernanceMetadataSanitizationResult result = GovernanceMetadataSanitizationResult.Create(
            action,
            new Dictionary<string, string>(),
            Array.Empty<OperationReason>(),
            CreateValidBudgetValidation());

        Assert.Equal(canProceed, result.CanProceed);
        Assert.Equal(isDenied, result.IsDenied);
    }

    [Theory]
    [InlineData(GovernanceMetadataSanitizationAction.Allow)]
    [InlineData(GovernanceMetadataSanitizationAction.Warn)]
    [InlineData(GovernanceMetadataSanitizationAction.Redact)]
    [InlineData(GovernanceMetadataSanitizationAction.Drop)]
    public void ThrowIfDeniedIsNoOpForNonDeniedResults(GovernanceMetadataSanitizationAction action)
    {
        GovernanceMetadataSanitizationResult result = GovernanceMetadataSanitizationResult.Create(
            action,
            new Dictionary<string, string>(),
            Array.Empty<OperationReason>(),
            CreateValidBudgetValidation());

        result.ThrowIfDenied();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void DeniedResultWithoutReasonsUsesFallbackMessageAndParameter(string? parameterName)
    {
        GovernanceMetadataSanitizationResult result = GovernanceMetadataSanitizationResult.Create(
            GovernanceMetadataSanitizationAction.Deny,
            new Dictionary<string, string>(),
            Array.Empty<OperationReason>(),
            CreateValidBudgetValidation());

        ArgumentException exception = Assert.Throws<ArgumentException>(() => result.ThrowIfDenied(parameterName));

        Assert.Equal("metadata", exception.ParamName);
        Assert.Contains("Governance metadata sanitation denied the metadata collection.", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void DeniedResultComposesMultipleReasonMessagesAndPreservesParameterName()
    {
        GovernanceMetadataSanitizationResult result = GovernanceMetadataSanitizationResult.Create(
            GovernanceMetadataSanitizationAction.Deny,
            new Dictionary<string, string>(),
            [
                OperationReason.Create("metadata.first", "First reason."),
                OperationReason.Create("metadata.second", "Second reason.")
            ],
            CreateValidBudgetValidation());

        ArgumentException exception = Assert.Throws<ArgumentException>(() => result.ThrowIfDenied("governanceMetadata"));

        Assert.Equal("governanceMetadata", exception.ParamName);
        Assert.Contains("First reason.; Second reason.", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void SanitationResultPreservesSuppliedReferences()
    {
        IReadOnlyDictionary<string, string> metadata = new Dictionary<string, string>
        {
            ["key"] = "value"
        };
        IReadOnlyList<OperationReason> reasons = [OperationReason.Create("metadata.warning", "Warning.")];
        GovernanceMetadataBudgetValidationResult budget = CreateValidBudgetValidation();

        GovernanceMetadataSanitizationResult result = GovernanceMetadataSanitizationResult.Create(
            GovernanceMetadataSanitizationAction.Warn,
            metadata,
            reasons,
            budget);

        Assert.Same(metadata, result.SanitizedMetadata);
        Assert.Same(reasons, result.Reasons);
        Assert.Same(budget, result.BudgetValidation);
    }

    private static GovernanceMetadataBudgetValidationResult CreateValidBudgetValidation()
    {
        return GovernanceMetadataBudgetValidationResult.Create(
            new Dictionary<string, string>(),
            Array.Empty<string>(),
            estimatedSerializedBytes: 0);
    }
}
