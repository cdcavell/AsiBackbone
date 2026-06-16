using CDCavell.AsiBackbone.Core.Actors;
using CDCavell.AsiBackbone.Core.Audit;
using CDCavell.AsiBackbone.Core.Signing;
using Xunit;

namespace CDCavell.AsiBackbone.Signing.LocalDevelopment.Tests;

public sealed class LocalDevelopmentSigningServiceTests
{
    [Fact]
    public async Task SignAsyncReturnsProviderNeutralSigningMetadata()
    {
        using var service = new LocalDevelopmentSigningService(LocalDevelopmentSigningOptions.Create(
            keyId: "test-key",
            keyVersion: "v1"));
        var request = new SigningRequest(
            "abc123",
            hashAlgorithm: "SHA-256",
            purpose: "audit-ledger-record",
            keyId: "test-key",
            keyVersion: "v1");

        SigningResult result = await service.SignAsync(request, TestContext.Current.CancellationToken);

        Assert.True(result.IsSigned);
        Assert.Equal("abc123", result.Metadata.SigningHash);
        Assert.Equal("SHA-256", result.Metadata.HashAlgorithm);
        Assert.NotNull(result.Metadata.Signature);
        Assert.Equal(LocalDevelopmentSigningOptions.DefaultSignatureAlgorithm, result.Metadata.SignatureAlgorithm);
        Assert.Equal("test-key", result.Metadata.KeyId);
        Assert.Equal("v1", result.Metadata.KeyVersion);
        Assert.Equal(LocalDevelopmentSigningOptions.DefaultProviderName, result.Metadata.Provider);
        Assert.True(result.Metadata.SignedUtc.HasValue);
        Assert.Equal("signed", result.Metadata.Metadata["signing_status"]);
        Assert.Equal("local-development-only", result.Metadata.Metadata["provider_warning"]);
    }

    [Fact]
    public async Task VerifyAsyncValidatesSignatureProducedBySameProviderInstance()
    {
        using var service = new LocalDevelopmentSigningService(LocalDevelopmentSigningOptions.Create(
            keyId: "test-key",
            keyVersion: "v1"));
        var signingRequest = new SigningRequest(
            "def456",
            hashAlgorithm: "SHA-256",
            purpose: "audit-ledger-record",
            keyId: "test-key",
            keyVersion: "v1");
        SigningResult signingResult = await service.SignAsync(signingRequest, TestContext.Current.CancellationToken);

        var verificationRequest = new SignatureVerificationRequest(
            "def456",
            signingResult.Metadata,
            purpose: "audit-ledger-record");
        SignatureVerificationResult verificationResult = await service.VerifyAsync(verificationRequest, TestContext.Current.CancellationToken);

        Assert.True(verificationResult.IsValid);
        Assert.Equal("Verified", verificationResult.Status);
        Assert.Null(verificationResult.FailureCode);
    }

    [Fact]
    public async Task VerifyAsyncFailsForHashMismatch()
    {
        using var service = new LocalDevelopmentSigningService();
        SigningResult signingResult = await service.SignAsync(
            new SigningRequest("expected-hash", hashAlgorithm: "SHA-256"),
            TestContext.Current.CancellationToken);

        SignatureVerificationResult verificationResult = await service.VerifyAsync(
            new SignatureVerificationRequest("changed-hash", signingResult.Metadata),
            TestContext.Current.CancellationToken);

        Assert.False(verificationResult.IsValid);
        Assert.Equal("localdev.signature.hash-mismatch", verificationResult.FailureCode);
    }

    [Fact]
    public async Task SignAsyncReturnsUnsignedFailureMetadataForUnsupportedAlgorithm()
    {
        using var service = new LocalDevelopmentSigningService();
        var request = new SigningRequest(
            "abc123",
            hashAlgorithm: "SHA-512",
            purpose: "audit-ledger-record");

        SigningResult result = await service.SignAsync(request, TestContext.Current.CancellationToken);

        Assert.False(result.IsSigned);
        Assert.Null(result.Metadata.Signature);
        Assert.Equal("failed", result.Metadata.Metadata["signing_status"]);
        Assert.Equal("localdev.signing.hash-algorithm-unsupported", result.Metadata.Metadata["failure_code"]);
    }

    [Fact]
    public async Task CanonicalAuditLedgerRecordCanBeSignedAndVerifiedEndToEnd()
    {
        IAsiBackboneActorContext actor = AsiBackboneActorContext.Service("system-1", "System");
        var residue = AuditResidue.Create(
            actor,
            "sample.governed-operation",
            "Allowed",
            reasonCodes: ["sample.allowed"],
            eventId: "event-1",
            occurredUtc: new DateTimeOffset(2026, 6, 16, 10, 0, 0, TimeSpan.Zero),
            correlationId: "correlation-1",
            policyVersion: "policy-v1",
            policyHash: "policy-hash");
        var record = AuditLedgerRecord.FromResidue(
            residue,
            recordId: "record-1",
            recordedUtc: new DateTimeOffset(2026, 6, 16, 10, 0, 1, TimeSpan.Zero));
        CanonicalPayload payload = CanonicalPayloadBuilder.ForAuditLedgerRecord(record);
        CanonicalPayloadHash hash = CanonicalPayloadHasher.ComputeHash(payload);

        using var service = new LocalDevelopmentSigningService(LocalDevelopmentSigningOptions.Create(
            keyId: "test-key",
            keyVersion: "v1"));
        SigningResult signingResult = await service.SignAsync(
            new SigningRequest(
                hash.HashValue,
                hash.HashAlgorithm,
                purpose: CanonicalArtifactTypes.AuditLedgerRecord,
                keyId: "test-key",
                keyVersion: "v1",
                metadata: hash.ToSigningMetadata().Metadata),
            TestContext.Current.CancellationToken);
        SignatureVerificationResult verificationResult = await service.VerifyAsync(
            new SignatureVerificationRequest(
                hash.HashValue,
                signingResult.Metadata,
                purpose: CanonicalArtifactTypes.AuditLedgerRecord),
            TestContext.Current.CancellationToken);

        Assert.True(signingResult.IsSigned);
        Assert.True(verificationResult.IsValid);
        Assert.Equal(hash.HashValue, signingResult.Metadata.SigningHash);
        Assert.Equal(CanonicalArtifactTypes.AuditLedgerRecord, signingResult.Metadata.Metadata["artifact_type"]);
    }
}
