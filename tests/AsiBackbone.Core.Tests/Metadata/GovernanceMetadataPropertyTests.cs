using AsiBackbone.Core.Metadata;
using FsCheck;
using FsCheck.Fluent;
using Xunit;

namespace AsiBackbone.Core.Tests.Metadata;

/// <summary>
/// Contains property-based tests for governance-related Core behavior.
/// </summary>
public sealed class GovernanceMetadataPropertyTests
{
    /// <summary>
    /// Verifies that normalizing governance metadata more than once produces
    /// the same result as normalizing it once.
    /// </summary>
    [Fact]
    public void Metadata_normalization_should_be_idempotent()
    {
        Prop.ForAll<string, string>((key, value) =>
        {
            var metadata = new Dictionary<string, string>
            {
                [key ?? string.Empty] = value ?? string.Empty
            };

            IReadOnlyDictionary<string, string> first =
                GovernanceMetadataBudgetValidator.Normalize(metadata);

            IReadOnlyDictionary<string, string> second =
                GovernanceMetadataBudgetValidator.Normalize(first);

            return AreEquivalent(first, second);
        }).QuickCheckThrowOnFailure();
    }

    private static bool AreEquivalent(
        IReadOnlyDictionary<string, string> left,
        IReadOnlyDictionary<string, string> right)
    {
        return left.Count == right.Count
            && left.All(item =>
                right.TryGetValue(item.Key, out string? value)
                && StringComparer.Ordinal.Equals(item.Value, value));
    }
}
