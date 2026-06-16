using CDCavell.AsiBackbone.Core.Signing;

namespace CDCavell.AsiBackbone.Core.Integrity;

/// <summary>
/// Verifies provider-neutral append-only audit integrity chains.
/// </summary>
public static class AuditIntegrityVerifier
{
    /// <summary>
    /// Verifies that the supplied links form one continuous append-only chain in the supplied order.
    /// </summary>
    public static AuditIntegrityVerificationResult Verify(
        IEnumerable<AuditIntegrityLink> links,
        string? expectedChainId = null,
        bool requireGenesis = true)
    {
        ArgumentNullException.ThrowIfNull(links);

        List<AuditIntegrityLink> orderedLinks = [.. links];

        if (orderedLinks.Count == 0)
        {
            return AuditIntegrityVerificationResult.Failed(
                AuditIntegrityVerificationCategory.EmptyChain,
                "integrity.chain-empty",
                "No integrity links were supplied.");
        }

        string chainId = string.IsNullOrWhiteSpace(expectedChainId)
            ? orderedLinks[0].ChainId
            : expectedChainId.Trim();
        HashSet<long> observedSequences = [];
        string expectedPreviousHash = string.Empty;
        long expectedSequence = requireGenesis ? 1 : orderedLinks[0].Sequence;

        foreach (AuditIntegrityLink link in orderedLinks)
        {
            AuditIntegrityVerificationResult? result = VerifyLink(
                link,
                chainId,
                expectedSequence,
                expectedPreviousHash,
                observedSequences,
                requireGenesis);

            if (result is not null)
            {
                return result;
            }

            observedSequences.Add(link.Sequence);
            expectedPreviousHash = link.LinkHash;
            expectedSequence = link.Sequence + 1;
        }

        AuditIntegrityLink tip = orderedLinks[^1];
        return AuditIntegrityVerificationResult.Valid(chainId, orderedLinks.Count, tip.LinkHash);
    }

    private static AuditIntegrityVerificationResult? VerifyLink(
        AuditIntegrityLink link,
        string expectedChainId,
        long expectedSequence,
        string expectedPreviousHash,
        HashSet<long> observedSequences,
        bool requireGenesis)
    {
        if (!string.Equals(link.HashAlgorithm, CanonicalPayloadOptions.DefaultHashAlgorithm, StringComparison.Ordinal))
        {
            return AuditIntegrityVerificationResult.Failed(
                AuditIntegrityVerificationCategory.UnsupportedAlgorithm,
                "integrity.hash-algorithm-unsupported",
                "The integrity link uses an unsupported hash algorithm.",
                link);
        }

        if (!string.Equals(link.ChainId, expectedChainId, StringComparison.Ordinal))
        {
            return AuditIntegrityVerificationResult.Failed(
                AuditIntegrityVerificationCategory.WrongChain,
                "integrity.chain-id-mismatch",
                "The integrity link belongs to a different chain.",
                link);
        }

        if (!observedSequences.Add(link.Sequence))
        {
            return AuditIntegrityVerificationResult.Failed(
                AuditIntegrityVerificationCategory.ForkedChain,
                "integrity.sequence-duplicate",
                "Multiple links claim the same chain sequence.",
                link);
        }

        observedSequences.Remove(link.Sequence);

        if (link.Sequence != expectedSequence)
        {
            AuditIntegrityVerificationCategory category = link.Sequence > expectedSequence
                ? AuditIntegrityVerificationCategory.MissingRecord
                : AuditIntegrityVerificationCategory.ReorderedRecord;

            return AuditIntegrityVerificationResult.Failed(
                category,
                category is AuditIntegrityVerificationCategory.MissingRecord
                    ? "integrity.sequence-missing"
                    : "integrity.sequence-reordered",
                "The integrity link sequence is not continuous in the supplied order.",
                link,
                new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["expected_sequence"] = expectedSequence.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    ["actual_sequence"] = link.Sequence.ToString(System.Globalization.CultureInfo.InvariantCulture)
                });
        }

        if (requireGenesis && link.Sequence == 1 && link.PreviousLinkHash.Length != 0)
        {
            return AuditIntegrityVerificationResult.Failed(
                AuditIntegrityVerificationCategory.HashMismatch,
                "integrity.genesis-previous-hash-present",
                "The genesis link must not point to a previous link hash.",
                link);
        }

        if (!string.Equals(link.PreviousLinkHash, expectedPreviousHash, StringComparison.Ordinal))
        {
            return AuditIntegrityVerificationResult.Failed(
                AuditIntegrityVerificationCategory.HashMismatch,
                "integrity.previous-link-hash-mismatch",
                "The integrity link does not point to the previous link hash.",
                link,
                new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["expected_previous_hash"] = expectedPreviousHash,
                    ["actual_previous_hash"] = link.PreviousLinkHash
                });
        }

        string expectedLinkHash = link.ComputeExpectedLinkHash();
        if (!string.Equals(link.LinkHash, expectedLinkHash, StringComparison.Ordinal))
        {
            return AuditIntegrityVerificationResult.Failed(
                AuditIntegrityVerificationCategory.ModifiedRecord,
                "integrity.link-hash-mismatch",
                "The integrity link hash no longer matches its canonical fields.",
                link,
                new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["expected_link_hash"] = expectedLinkHash,
                    ["actual_link_hash"] = link.LinkHash
                });
        }

        return null;
    }
}
