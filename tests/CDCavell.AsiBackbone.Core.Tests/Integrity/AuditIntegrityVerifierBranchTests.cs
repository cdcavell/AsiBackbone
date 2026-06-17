using CDCavell.AsiBackbone.Core.Integrity;
using CDCavell.AsiBackbone.Core.Serialization;
using CDCavell.AsiBackbone.Core.Signing;
using Xunit;

namespace CDCavell.AsiBackbone.Core.Tests.Integrity;

public sealed class AuditIntegrityVerifierBranchTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 16, 12, 0, 0, TimeSpan.Zero);

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
    public void VerifyUsesFirstLinkChainWhenNoExpectedChainIsSupplied()
    {
        var first = AuditIntegrityLink.CreateGenesis(
            "audit-ledger",
            CreateRecordHash("record-1"),
            Now);

        AuditIntegrityVerificationResult result = AuditIntegrityVerifier.Verify([first]);

        Assert.True(result.IsValid);
        Assert.Equal("audit-ledger", result.ChainId);
        Assert.Equal(first.LinkHash, result.SafeMetadata["tip_hash"]);
    }

    [Fact]
    public void VerifyTrimsExpectedChainId()
    {
        var first = AuditIntegrityLink.CreateGenesis(
            "audit-ledger",
            CreateRecordHash("record-1"),
            Now);

        AuditIntegrityVerificationResult result = AuditIntegrityVerifier.Verify([first], "  audit-ledger  ");

        Assert.True(result.IsValid);
        Assert.Equal("audit-ledger", result.ChainId);
    }

    [Fact]
    public void VerifyDetectsWrongChain()
    {
        var first = AuditIntegrityLink.CreateGenesis(
            "audit-ledger",
            CreateRecordHash("record-1"),
            Now);

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
        var first = AuditIntegrityLink.CreateGenesis(
            "audit-ledger",
            CreateRecordHash("record-1"),
            Now);
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
    public void VerifyDetectsReorderedSequenceWhenGenesisIsNotRequired()
    {
        var startingAtSecond = CreateComputedLink(
            chainId: "audit-ledger",
            sequence: 2,
            recordHash: CreateRecordHash("record-2"),
            previousLinkHash: string.Empty,
            createdUtc: Now.AddSeconds(1));
        var earlier = AuditIntegrityLink.CreateGenesis(
            "audit-ledger",
            CreateRecordHash("record-1"),
            Now);

        AuditIntegrityVerificationResult result = AuditIntegrityVerifier.Verify(
            [startingAtSecond, earlier],
            "audit-ledger",
            requireGenesis: false);

        Assert.False(result.IsValid);
        Assert.Equal(AuditIntegrityVerificationCategory.ReorderedRecord, result.Category);
        Assert.Equal("integrity.sequence-reordered", result.FailureCode);
        Assert.Equal("3", result.SafeMetadata["expected_sequence"]);
        Assert.Equal("1", result.SafeMetadata["actual_sequence"]);
    }

    [Fact]
    public void VerifyDetectsGenesisPreviousHashWhenGenesisIsRequired()
    {
        var first = AuditIntegrityLink.CreateGenesis(
            "audit-ledger",
            CreateRecordHash("record-1"),
            Now);
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
    public void VerifyAcceptsNonGenesisStartingSequenceWhenGenesisIsNotRequired()
    {
        var partial = CreateComputedLink(
            chainId: "audit-ledger",
            sequence: 5,
            recordHash: CreateRecordHash("record-5"),
            previousLinkHash: string.Empty,
            createdUtc: Now.AddSeconds(5));

        AuditIntegrityVerificationResult result = AuditIntegrityVerifier.Verify(
            [partial],
            "audit-ledger",
            requireGenesis: false);

        Assert.True(result.IsValid);
        Assert.Equal("audit-ledger", result.ChainId);
        Assert.Equal("1", result.SafeMetadata["link_count"]);
        Assert.Equal(partial.LinkHash, result.SafeMetadata["tip_hash"]);
    }

    private static AuditIntegrityLink CreateComputedLink(
        string chainId,
        long sequence,
        CanonicalPayloadHash recordHash,
        string previousLinkHash,
        DateTimeOffset createdUtc)
    {
        var draft = AuditIntegrityLink.Rehydrate(
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
}
