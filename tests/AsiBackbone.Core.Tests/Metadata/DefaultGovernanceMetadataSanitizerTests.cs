using AsiBackbone.Core.Metadata;
using Xunit;

namespace AsiBackbone.Core.Tests.Metadata;

/// <summary>
/// Unit tests for the <see cref="DefaultGovernanceMetadataSanitizer" /> class.
/// </summary>
public sealed class DefaultGovernanceMetadataSanitizerTests
{
    /// <summary>
    /// Verifies that classifier redaction produces a new safe metadata collection without mutating caller-owned metadata.
    /// </summary>
    [Fact]
    public async Task SanitizeAsyncRedactsWithoutMutatingCallerMetadata()
    {
        var metadata = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["note"] = "sensitive-value"
        };
        var sanitizer = new DefaultGovernanceMetadataSanitizer(
        [
            new DelegateMetadataClassifier(context =>
                context.Value == "sensitive-value"
                    ? GovernanceMetadataClassificationResult.Redact(
                        "metadata.test.redacted",
                        "The test metadata value was redacted.")
                    : GovernanceMetadataClassificationResult.Allow())
        ]);

        GovernanceMetadataSanitizationResult result = await sanitizer.SanitizeAsync(
            metadata,
            TestContext.Current.CancellationToken);

        Assert.True(result.CanProceed);
        Assert.Equal(GovernanceMetadataSanitizationAction.Redact, result.Action);
        Assert.Equal(
            GovernanceMetadataClassificationResult.DefaultRedactedValue,
            result.SanitizedMetadata["note"]);
        Assert.Equal("sensitive-value", metadata["note"]);
        Assert.Contains(result.Reasons, reason => reason.Code == "metadata.test.redacted");
    }

    /// <summary>
    /// Verifies that a classifier denial fails closed and exposes no forwardable metadata.
    /// </summary>
    [Fact]
    public async Task SanitizeAsyncDeniesUnsafeMetadata()
    {
        var metadata = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["note"] = "blocked-value"
        };
        var sanitizer = new DefaultGovernanceMetadataSanitizer(
        [
            new DelegateMetadataClassifier(context =>
                context.Value == "blocked-value"
                    ? GovernanceMetadataClassificationResult.Deny(
                        "metadata.test.denied",
                        "The test metadata value was denied.")
                    : GovernanceMetadataClassificationResult.Allow())
        ]);

        GovernanceMetadataSanitizationResult result = await sanitizer.SanitizeAsync(
            metadata,
            TestContext.Current.CancellationToken);

        Assert.True(result.IsDenied);
        Assert.False(result.CanProceed);
        Assert.Equal(GovernanceMetadataSanitizationAction.Deny, result.Action);
        Assert.Empty(result.SanitizedMetadata);
        Assert.Contains(result.Reasons, reason => reason.Code == "metadata.test.denied");
        _ = Assert.Throws<ArgumentException>(() => result.ThrowIfDenied(nameof(metadata)));
    }

    /// <summary>
    /// Verifies that null metadata produces an allowed empty result.
    /// </summary>
    [Fact]
    public async Task SanitizeAsyncAllowsEmptyMetadata()
    {
        var sanitizer = new DefaultGovernanceMetadataSanitizer();

        GovernanceMetadataSanitizationResult result = await sanitizer.SanitizeAsync(
            null,
            TestContext.Current.CancellationToken);

        Assert.True(result.CanProceed);
        Assert.Equal(GovernanceMetadataSanitizationAction.Allow, result.Action);
        Assert.Empty(result.SanitizedMetadata);
        Assert.Empty(result.Reasons);
        Assert.True(result.BudgetValidation.IsValid);
    }

    /// <summary>
    /// Verifies that reserved key fragments fail closed when a classifier does not remove the entry.
    /// </summary>
    [Fact]
    public async Task SanitizeAsyncDeniesReservedKeyFragments()
    {
        var metadata = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["api_key"] = "opaque-but-disallowed"
        };
        var sanitizer = new DefaultGovernanceMetadataSanitizer();

        GovernanceMetadataSanitizationResult result = await sanitizer.SanitizeAsync(
            metadata,
            TestContext.Current.CancellationToken);

        Assert.True(result.IsDenied);
        Assert.Empty(result.SanitizedMetadata);
        Assert.False(result.BudgetValidation.IsValid);
        Assert.Contains(
            result.Reasons,
            reason => reason.Code == GovernanceMetadataSanitizationReasonCodes.BudgetViolation);
    }

    /// <summary>
    /// Verifies that classifiers may drop a reserved-key entry before post-sanitation budget validation.
    /// </summary>
    [Fact]
    public async Task SanitizeAsyncDropsReservedKeyBeforeBudgetValidation()
    {
        var metadata = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["api_key"] = "value-that-must-not-continue"
        };
        var sanitizer = new DefaultGovernanceMetadataSanitizer(
        [
            new DelegateMetadataClassifier(context =>
                context.Key == "api_key"
                    ? GovernanceMetadataClassificationResult.Drop(
                        "metadata.test.dropped",
                        "The reserved test metadata entry was dropped.")
                    : GovernanceMetadataClassificationResult.Allow())
        ]);

        GovernanceMetadataSanitizationResult result = await sanitizer.SanitizeAsync(
            metadata,
            TestContext.Current.CancellationToken);

        Assert.True(result.CanProceed);
        Assert.Equal(GovernanceMetadataSanitizationAction.Drop, result.Action);
        Assert.Empty(result.SanitizedMetadata);
        Assert.True(result.BudgetValidation.IsValid);
        Assert.Contains(result.Reasons, reason => reason.Code == "metadata.test.dropped");
    }

    /// <summary>
    /// Verifies that the metadata budget is evaluated after classifier redaction.
    /// </summary>
    [Fact]
    public async Task SanitizeAsyncAppliesBudgetAfterRedaction()
    {
        var budget = GovernanceMetadataBudget.Create(
            maxValueLength: 4,
            reservedKeyFragments: []);
        var metadata = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["note"] = "12345"
        };
        var unsanitizedPipeline = new DefaultGovernanceMetadataSanitizer(budget: budget);
        var redactingPipeline = new DefaultGovernanceMetadataSanitizer(
        [
            new DelegateMetadataClassifier(_ =>
                GovernanceMetadataClassificationResult.Redact(
                    "metadata.test.shortened",
                    "The test metadata value was replaced before budget validation.",
                    "safe"))
        ],
        budget);

        GovernanceMetadataSanitizationResult denied = await unsanitizedPipeline.SanitizeAsync(
            metadata,
            TestContext.Current.CancellationToken);
        GovernanceMetadataSanitizationResult redacted = await redactingPipeline.SanitizeAsync(
            metadata,
            TestContext.Current.CancellationToken);

        Assert.True(denied.IsDenied);
        Assert.False(denied.BudgetValidation.IsValid);
        Assert.True(redacted.CanProceed);
        Assert.Equal(GovernanceMetadataSanitizationAction.Redact, redacted.Action);
        Assert.Equal("safe", redacted.SanitizedMetadata["note"]);
        Assert.True(redacted.BudgetValidation.IsValid);
    }

    private sealed class DelegateMetadataClassifier(
        Func<GovernanceMetadataClassificationContext, GovernanceMetadataClassificationResult> classify)
        : IGovernanceMetadataClassifier
    {
        public ValueTask<GovernanceMetadataClassificationResult> ClassifyAsync(
            GovernanceMetadataClassificationContext context,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult(classify(context));
        }
    }
}
