using CDCavell.AsiBackbone.Core.Integrity;
using CDCavell.AsiBackbone.Core.Serialization;
using CDCavell.AsiBackbone.Core.Signing;
using Xunit;

namespace CDCavell.AsiBackbone.Core.Tests.Integrity;

public sealed class AuditIntegrityVerifierTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 16, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void VerifyAcceptsContinuousChain()
    {
        var first = AuditIntegrityLink.CreateGenesis("audit-ledger", CreateRecordHash("record-1"), Now);
        var second = AuditIntegrityLink.Append(first, CreateRecordHash("record-2"), Now.AddSeconds(1));
        var third = AuditIntegrityLink.Append(second, CreateRecordHash("record-3"), Now.AddSeconds(2));

        AuditIntegrityVerificationResult result = AuditIntegrityVerifier.Verify([first, second, third], "audit-ledger");

        Assert.True(result.IsValid);
        Assert.Equal(AuditIntegrityVerificationCategory.Valid, result.Category);
        Assert.Equal("audit-ledger", result.ChainId);
        Assert.Equal("3", result.SafeMetadata["link_count"]);
        Assert.Equal(third.LinkHash, result.SafeMetadata["tip_hash"]);
    }

    [Fact]
    public void VerifyUsesFirstLinkChainWhenExpectedChainIsNotSupplied()
    {
        var first = AuditIntegrityLink.CreateGenesis("audit-ledger", CreateRecordHash("record-1"), Now);
        var second = AuditIntegrityLink.Append(first, CreateRecordHash("record-2"), Now.AddSeconds(1));

        AuditIntegrityVerificationResult result = AuditIntegrityVerifier.Verify([first, second]);

        Assert.True(result.IsValid);
        Assert.Equal("audit-ledger", result.ChainId);
        Assert.Equal(second.LinkHash, result.SafeMetadata["tip_hash"]);
    }

    [Fact]
    public void VerifyTrimsExpectedChainIdBeforeComparison()
    {
        var first = AuditIntegrityLink.CreateGenesis("audit-ledger", CreateRecordHash("record-1"), Now);

        AuditIntegrityVerificationResult result = AuditIntegrityVerifier.Verify([first], "  audit-ledger  ");

        Assert.True(result.IsValid);
        Assert.Equal("audit-ledger", result.ChainId);
    }

    [Fact]
    public void VerifyRejectsNullLinks()
    {
        Assert.Throws<ArgumentNullException>(() => AuditIntegrityVerifier.Verify(null!));
    }

    [Fact]
    public void VerifyRejectsEmptyChain()
    {
        AuditIntegrityVerificationResult result = AuditIntegrityVerifier.Verify([]);

        Assert.False(result.IsValid);
        Assert.Equal(AuditIntegrityVerificationCategory.EmptyChain, result.Category);
        Assert.Equal("integrity.chain-empty", result.FailureCode);
    }

    [Fact]
    public void AppendBindsPreviousHashAndNextSequence()
    {
        var first = AuditIntegrityLink.CreateGenesis("audit-ledger", CreateRecordHash("record-1"), Now);
        var second = AuditIntegrityLink.Append(first, CreateRecordHash("record-2"), Now.AddSeconds(1));

        Assert.True(first.IsGenesis);
        Assert.Equal(2, second.Sequence);
        Assert.Equal(first.LinkHash, second.PreviousLinkHash);
        Assert.NotEqual(first.LinkHash, second.LinkHash);
    }

    [Fact]
    public void VerifyDetectsModifiedLinkFields()
    {
        var first = AuditIntegrityLink.CreateGenesis("audit-ledger", CreateRecordHash("record-1"), Now);
        var modified = AuditIntegrityLink.Rehydrate(
            first.ChainId,
            first.Sequence,
            first.RecordId,
            first.RecordType,
            MutateHash(first.RecordHash),
            first.PreviousLinkHash,
            first.LinkHash,
            first.HashAlgorithm,
            first.CanonicalizationVersion,
            first.SchemaVersion,
            first.CreatedUtc);

        AuditIntegrityVerificationResult result = AuditIntegrityVerifier.Verify([modified], "audit-ledger");

        Assert.False(result.IsValid);
        Assert.Equal(AuditIntegrityVerificationCategory.ModifiedRecord, result.Category);
        Assert.Equal("integrity.link-hash-mismatch", result.FailureCode);
    }

    [Fact]
    public void VerifyDetectsMissingSequence()
    {
        var first = AuditIntegrityLink.CreateGenesis("audit-ledger", CreateRecordHash("record-1"), Now);
        var second = AuditIntegrityLink.Append(first, CreateRecordHash("record-2"), Now.AddSeconds(1));
        var third = AuditIntegrityLink.Append(second, CreateRecordHash("record-3"), Now.AddSeconds(2));

        AuditIntegrityVerificationResult result = AuditIntegrityVerifier.Verify([first, third], "audit-ledger");

        Assert.False(result.IsValid);
        Assert.Equal(AuditIntegrityVerificationCategory.MissingRecord, result.Category);
        Assert.Equal("integrity.sequence-missing", result.FailureCode);
        Assert.Equal("2", result.SafeMetadata["expected_sequence"]);
        Assert.Equal("3", result.SafeMetadata["actual_sequence"]);
    }

    [Fact]
    public void VerifyDetectsReorderedSequenceWhenGenesisIsNotRequired()
    {
        var first = AuditIntegrityLink.CreateGenesis("audit-ledger", CreateRecordHash("record-1"), Now);
        var second = AuditIntegrityLink.Append(first, CreateRecordHash("record-2"), Now.AddSeconds(1));

        AuditIntegrityVerificationResult result = AuditIntegrityVerifier.Verify([second, first], "audit-ledger", requireGenesis: false);

        Assert.False(result.IsValid);
        Assert.Equal(AuditIntegrityVerificationCategory.ReorderedRecord, result.Category);
        Assert.Equal("integrity.sequence-reordered", result.FailureCode);
        Assert.Equal("3", result.SafeMetadata["expected_sequence"]);
        Assert.Equal("1", result.SafeMetadata["actual_sequence"]);
    }

    [Fact]
    public void VerifyAcceptsNonGenesisStartingSequenceWhenGenesisIsNotRequiredAndPreviousHashBoundaryIsEmpty()
    {
        var partial = CreateComputedLink(
            chainId: "audit-ledger",
            sequence: 5,
            recordHash: CreateRecordHash("record-5"),
            previousLinkHash: string.Empty,
            createdUtc: Now.AddSeconds(5));

        AuditIntegrityVerificationResult result = AuditIntegrityVerifier.Verify([partial], "audit-ledger", requireGenesis: false);

        Assert.True(result.IsValid);
        Assert.Equal("audit-ledger", result.ChainId);
        Assert.Equal("1", result.SafeMetadata["link_count"]);
        Assert.Equal(partial.LinkHash, result.SafeMetadata["tip_hash"]);
    }

    [Fact]
    public void VerifyDetectsForkedSequence()
    {
        var first = AuditIntegrityLink.CreateGenesis("audit-ledger", CreateRecordHash("record-1"), Now);
        var second = AuditIntegrityLink.Append(first, CreateRecordHash("record-2"), Now.AddSeconds(1));
        var fork = AuditIntegrityLink.Rehydrate(
            second.ChainId,
            second.Sequence,
            "record-2b",
            second.RecordType,
            second.RecordHash,
            second.PreviousLinkHash,
            second.LinkHash,
            second.HashAlgorithm,
            second.CanonicalizationVersion,
            second.SchemaVersion,
            second.CreatedUtc);

        AuditIntegrityVerificationResult result = AuditIntegrityVerifier.Verify([first, second, fork], "audit-ledger");

        Assert.False(result.IsValid);
        Assert.Equal(AuditIntegrityVerificationCategory.ForkedChain, result.Category);
        Assert.Equal("integrity.sequence-duplicate", result.FailureCode);
    }

    [Fact]
    public void VerifyDetectsWrongChain()
    {
        var first = AuditIntegrityLink.CreateGenesis("audit-ledger", CreateRecordHash("record-1"), Now);

        AuditIntegrityVerificationResult result = AuditIntegrityVerifier.Verify([first], "other-ledger");

        Assert.False(result.IsValid);
        Assert.Equal(AuditIntegrityVerificationCategory.WrongChain, result.Category);
        Assert.Equal("integrity.chain-id-mismatch", result.FailureCode);
        Assert.Equal("audit-ledger", result.SafeMetadata["chain_id"]);
        Assert.Equal("record-1", result.SafeMetadata["record_id"]);
        Assert.Equal("1", result.SafeMetadata["sequence"]);
    }

    [Fact]
    public void VerifyDetectsUnsupportedHashAlgorithmBeforeHashComparison()
    {
        var first = AuditIntegrityLink.CreateGenesis("audit-ledger", CreateRecordHash("record-1"), Now);
        var unsupported = AuditIntegrityLink.Rehydrate(
            first.ChainId,
            first.Sequence,
            first.RecordId,
            first.RecordType,
            first.RecordHash,
            first.PreviousLinkHash,
            first.LinkHash,
            "SHA-512",
            first.CanonicalizationVersion,
            first.SchemaVersion,
            first.CreatedUtc);

        AuditIntegrityVerificationResult result = AuditIntegrityVerifier.Verify([unsupported], "audit-ledger");

        Assert.False(result.IsValid);
        Assert.Equal(AuditIntegrityVerificationCategory.UnsupportedAlgorithm, result.Category);
        Assert.Equal("integrity.hash-algorithm-unsupported", result.FailureCode);
    }

    [Fact]
    public void VerifyDetectsGenesisPreviousHashWhenGenesisIsRequired()
    {
        var first = AuditIntegrityLink.CreateGenesis("audit-ledger", CreateRecordHash("record-1"), Now);
        var brokenGenesis = AuditIntegrityLink.Rehydrate(
            first.ChainId,
            first.Sequence,
            first.RecordId,
            first.RecordType,
            first.RecordHash,
            "unexpected-previous-hash",
            first.LinkHash,
            first.HashAlgorithm,
            first.CanonicalizationVersion,
            first.SchemaVersion,
            first.CreatedUtc);

        AuditIntegrityVerificationResult result = AuditIntegrityVerifier.Verify([brokenGenesis], "audit-ledger");

        Assert.False(result.IsValid);
        Assert.Equal(AuditIntegrityVerificationCategory.HashMismatch, result.Category);
        Assert.Equal("integrity.genesis-previous-hash-present", result.FailureCode);
    }

    [Fact]
    public void VerifyDetectsPreviousHashMismatch()
    {
        var first = AuditIntegrityLink.CreateGenesis("audit-ledger", CreateRecordHash("record-1"), Now);
        var second = AuditIntegrityLink.Append(first, CreateRecordHash("record-2"), Now.AddSeconds(1));
        var broken = AuditIntegrityLink.Rehydrate(
            second.ChainId,
            second.Sequence,
            second.RecordId,
            second.RecordType,
            second.RecordHash,
            MutateHash(second.PreviousLinkHash),
            second.LinkHash,
            second.HashAlgorithm,
            second.CanonicalizationVersion,
            second.SchemaVersion,
            second.CreatedUtc);

        AuditIntegrityVerificationResult result = AuditIntegrityVerifier.Verify([first, broken], "audit-ledger");

        Assert.False(result.IsValid);
        Assert.Equal(AuditIntegrityVerificationCategory.HashMismatch, result.Category);
        Assert.Equal("integrity.previous-link-hash-mismatch", result.FailureCode);
    }

    private static AuditIntegrityLink CreateComputedLink(
        string chainId,
        long sequence,
        CanonicalPayloadHash recordHash,
        string previousLinkHash,
        DateTimeOffset createdUtc)
    {
        AuditIntegrityLink draft = AuditIntegrityLink.Rehydrate(
            chainId,
            sequence,
            recordHash.ArtifactId,
            recordHash.ArtifactType,
            recordHash.HashValue,
            previousLinkHash,
            recordHash.HashValue,
            recordHash.HashAlgorithm,
            recordHash.CanonicalizationVersion,
            AsiBackboneSchemaVersions.StableArtifactsV1,
            createdUtc);

        return AuditIntegrityLink.Rehydrate(
            draft.ChainId,
            draft.Sequence,
            draft.RecordId,
            draft.RecordType,
            draft.RecordHash,
            draft.PreviousLinkHash,
            draft.ComputeExpectedLinkHash(),
            draft.HashAlgorithm,
            draft.CanonicalizationVersion,
            draft.SchemaVersion,
            draft.CreatedUtc);
    }

    private static CanonicalPayloadHash CreateRecordHash(string recordId)
    {
        var payload = CanonicalPayload.Create(
            CanonicalArtifactTypes.AuditLedgerRecord,
            recordId,
            AsiBackboneSchemaVersions.StableArtifactsV1,
            CanonicalPayloadOptions.DefaultCanonicalizationVersion,
            new Dictionary<string, object?>
            {
                ["recordId"] = recordId,
                ["outcome"] = "Allowed"
            });

        return CanonicalPayloadHasher.ComputeHash(payload);
    }

    private static string MutateHash(string hash)
    {
        return hash.StartsWith('a') ? $"b{hash[1..]}" : $"a{hash[1..]}";
    }
}
