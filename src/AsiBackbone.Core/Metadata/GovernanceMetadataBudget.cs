using System.Collections.ObjectModel;

namespace AsiBackbone.Core.Metadata;

/// <summary>
/// Defines optional host-owned budget limits for governance metadata dictionaries.
/// </summary>
/// <remarks>
/// The budget is intentionally advisory and helper-oriented. AsiBackbone cannot determine whether
/// host-provided metadata is sensitive in a specific deployment, but hosts can use these limits to
/// keep audit and governance metadata bounded before durable storage, emission, or signing.
/// </remarks>
public sealed class GovernanceMetadataBudget
{
    /// <summary>
    /// Gets the default recommended maximum number of metadata entries.
    /// </summary>
    public const int DefaultMaxCount = 32;

    /// <summary>
    /// Gets the default recommended maximum metadata key length, in characters.
    /// </summary>
    public const int DefaultMaxKeyLength = 64;

    /// <summary>
    /// Gets the default recommended maximum metadata value length, in characters.
    /// </summary>
    public const int DefaultMaxValueLength = 512;

    /// <summary>
    /// Gets the default recommended estimated serialized metadata size, in UTF-8 bytes.
    /// </summary>
    public const int DefaultMaxSerializedBytes = 8192;

    private static readonly IReadOnlyList<string> DefaultReservedKeyFragments =
        new ReadOnlyCollection<string>(
            new[]
            {
                "accesstoken",
                "apikey",
                "authorization",
                "bearer",
                "connectionstring",
                "credential",
                "password",
                "privatekey",
                "refreshtoken",
                "secret",
                "socialsecurity",
                "ssn"
            });

    private GovernanceMetadataBudget(
        int maxCount,
        int maxKeyLength,
        int maxValueLength,
        int maxSerializedBytes,
        IReadOnlyList<string> reservedKeyFragments)
    {
        ThrowIfLessThanOne(maxCount, nameof(maxCount));
        ThrowIfLessThanOne(maxKeyLength, nameof(maxKeyLength));
        ThrowIfLessThanOne(maxValueLength, nameof(maxValueLength));
        ThrowIfLessThanOne(maxSerializedBytes, nameof(maxSerializedBytes));
        ArgumentNullException.ThrowIfNull(reservedKeyFragments);

        MaxCount = maxCount;
        MaxKeyLength = maxKeyLength;
        MaxValueLength = maxValueLength;
        MaxSerializedBytes = maxSerializedBytes;
        ReservedKeyFragments = reservedKeyFragments;
    }

    /// <summary>
    /// Gets the recommended governance metadata budget.
    /// </summary>
    public static GovernanceMetadataBudget Recommended { get; } = Create();

    /// <summary>
    /// Gets the maximum number of normalized metadata entries allowed by the budget.
    /// </summary>
    public int MaxCount { get; }

    /// <summary>
    /// Gets the maximum normalized metadata key length allowed by the budget, in characters.
    /// </summary>
    public int MaxKeyLength { get; }

    /// <summary>
    /// Gets the maximum normalized metadata value length allowed by the budget, in characters.
    /// </summary>
    public int MaxValueLength { get; }

    /// <summary>
    /// Gets the maximum estimated serialized metadata size allowed by the budget, in UTF-8 bytes.
    /// </summary>
    public int MaxSerializedBytes { get; }

    /// <summary>
    /// Gets normalized reserved or discouraged metadata key fragments.
    /// </summary>
    public IReadOnlyList<string> ReservedKeyFragments { get; }

    /// <summary>
    /// Creates a governance metadata budget.
    /// </summary>
    public static GovernanceMetadataBudget Create(
        int maxCount = DefaultMaxCount,
        int maxKeyLength = DefaultMaxKeyLength,
        int maxValueLength = DefaultMaxValueLength,
        int maxSerializedBytes = DefaultMaxSerializedBytes,
        IEnumerable<string>? reservedKeyFragments = null)
    {
        return new GovernanceMetadataBudget(
            maxCount,
            maxKeyLength,
            maxValueLength,
            maxSerializedBytes,
            NormalizeReservedKeyFragments(reservedKeyFragments ?? DefaultReservedKeyFragments));
    }

    internal static string NormalizeKeyForComparison(string key)
    {
        string normalizedKey = key.Trim().ToLowerInvariant();
        char[] buffer = new char[normalizedKey.Length];
        int count = 0;

        foreach (char character in normalizedKey)
        {
            if (char.IsLetterOrDigit(character))
            {
                buffer[count] = character;
                count++;
            }
        }

        return new string(buffer, 0, count);
    }

    private static IReadOnlyList<string> NormalizeReservedKeyFragments(IEnumerable<string> reservedKeyFragments)
    {
        string[] normalizedFragments = [.. reservedKeyFragments
            .Where(fragment => !string.IsNullOrWhiteSpace(fragment))
            .Select(NormalizeKeyForComparison)
            .Where(fragment => fragment.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(fragment => fragment, StringComparer.Ordinal)];

        return normalizedFragments.Length == 0
            ? Array.AsReadOnly(Array.Empty<string>())
            : Array.AsReadOnly(normalizedFragments);
    }

    private static void ThrowIfLessThanOne(int value, string parameterName)
    {
        if (value < 1)
        {
            throw new ArgumentOutOfRangeException(parameterName, value, "Value must be greater than or equal to one.");
        }
    }
}
