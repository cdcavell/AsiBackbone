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
