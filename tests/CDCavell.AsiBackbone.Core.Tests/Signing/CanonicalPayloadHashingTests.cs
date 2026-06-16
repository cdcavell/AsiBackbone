using CDCavell.AsiBackbone.Core.Actors;
using CDCavell.AsiBackbone.Core.Audit;
using CDCavell.AsiBackbone.Core.Emissions;
using CDCavell.AsiBackbone.Core.Outbox;
using CDCavell.AsiBackbone.Core.Signing;
using Xunit;

namespace CDCavell.AsiBackbone.Core.Tests.Signing;

public sealed class CanonicalPayloadHashingTests
{
    [Fact]
    public void EquivalentAuditLedgerRecordsProduceStableCanonicalPayloadAndHash()
    {
        var options = CanonicalPayloadOptions.Create(["safe"]);
        AuditLedgerRecord first = CreateAuditLedgerRecord(["beta", "alpha"], new Dictionary<string, string>
        {
            ["ignored"] = "one",
            ["safe"] = "included"
        });
        AuditLedgerRecord second = CreateAuditLedgerRecord(["alpha", "beta"], new Dictionary<string, string>
        {
            ["safe"] = "included",
            ["ignored"] = "two"
        });

        CanonicalPayload firstPayload = CanonicalPayloadBuilder.ForAuditLedgerRecord(first, options);
        CanonicalPayload secondPayload = CanonicalPayloadBuilder.ForAuditLedgerRecord(second, options);
        CanonicalPayloadHash firstHash = CanonicalPayloadHasher.ComputeHash(firstPayload);
        CanonicalPayloadHash secondHash = CanonicalPayloadHasher.ComputeHash(secondPayload);

        Assert.Equal(firstPayload.CanonicalJson, secondPayload.CanonicalJson);
        Assert.Equal(firstHash.HashValue, secondHash.HashValue);
        Assert.Equal(CanonicalArtifactTypes.AuditLedgerRecord, firstHash.ArtifactType);
        Assert.Equal("record-1", firstHash.ArtifactId);
    }

    [Fact]
    public void MeaningfulAuditLedgerChangeChangesCanonicalHash()
    {
        var options = CanonicalPayloadOptions.Create(["safe"]);
        AuditLedgerRecord first = CreateAuditLedgerRecord(["alpha"], new Dictionary<string, string>
        {
            ["safe"] = "included"
        });
        AuditLedgerRecord second = CreateAuditLedgerRecord(["alpha", "policy.additional-review"], new Dictionary<string, string>
        {
            ["safe"] = "included"
        });

        CanonicalPayloadHash firstHash = CanonicalPayloadHasher.ComputeHash(CanonicalPayloadBuilder.ForAuditLedgerRecord(first, options));
        CanonicalPayloadHash secondHash = CanonicalPayloadHasher.ComputeHash(CanonicalPayloadBuilder.ForAuditLedgerRecord(second, options));

        Assert.NotEqual(firstHash.HashValue, secondHash.HashValue);
    }

    [Fact]
    public void AllowListedMetadataParticipatesInHashButOtherMetadataDoesNot()
    {
        var options = CanonicalPayloadOptions.Create(["safe"]);
        AuditLedgerRecord first = CreateAuditLedgerRecord(["alpha"], new Dictionary<string, string>
        {
            ["ignored"] = "one",
            ["safe"] = "included"
        });
        AuditLedgerRecord ignoredChange = CreateAuditLedgerRecord(["alpha"], new Dictionary<string, string>
        {
            ["ignored"] = "two",
            ["safe"] = "included"
        });
        AuditLedgerRecord allowedChange = CreateAuditLedgerRecord(["alpha"], new Dictionary<string, string>
        {
            ["ignored"] = "one",
            ["safe"] = "changed"
        });

        string firstHash = CanonicalPayloadHasher.ComputeHash(CanonicalPayloadBuilder.ForAuditLedgerRecord(first, options)).HashValue;
        string ignoredChangeHash = CanonicalPayloadHasher.ComputeHash(CanonicalPayloadBuilder.ForAuditLedgerRecord(ignoredChange, options)).HashValue;
        string allowedChangeHash = CanonicalPayloadHasher.ComputeHash(CanonicalPayloadBuilder.ForAuditLedgerRecord(allowedChange, options)).HashValue;

        Assert.Equal(firstHash, ignoredChangeHash);
        Assert.NotEqual(firstHash, allowedChangeHash);
    }

    [Fact]
    public void OutboxEntryHashBindsArtifactTypeAndIdentifier()
    {
        var options = CanonicalPayloadOptions.Create(["safe"]);
        var payload = GovernanceEmissionPayload.Create(
            "audit-summary",
            schemaVersion: "v1",
            contentType: "application/json",
            contentHash: "payload-hash",
            sizeBytes: 128,
            metadata: new Dictionary<string, string>
            {
                ["safe"] = "payload-metadata"
            });
        var envelope = GovernanceEmissionEnvelope.Create(
            GovernanceEmissionEventType.AuditResidue,
            eventId: "event-1",
            occurredUtc: new DateTimeOffset(2026, 6, 16, 8, 0, 0, TimeSpan.FromHours(-5)),
            envelopeId: "envelope-1",
            createdUtc: new DateTimeOffset(2026, 6, 16, 13, 0, 1, TimeSpan.Zero),
            correlationId: "correlation-1",
            auditResidueId: "residue-1",
            policyVersion: "policy-v1",
            policyHash: "policy-hash",
            payload: payload,
            metadata: new Dictionary<string, string>
            {
                ["safe"] = "envelope-metadata"
            });
        var entry = GovernanceOutboxEntry.Create(
            envelope,
            outboxEntryId: "outbox-1",
            createdUtc: new DateTimeOffset(2026, 6, 16, 13, 0, 2, TimeSpan.Zero),
            metadata: new Dictionary<string, string>
            {
                ["safe"] = "outbox-metadata"
            });

        CanonicalPayload payloadToHash = CanonicalPayloadBuilder.ForGovernanceOutboxEntry(entry, options);
        CanonicalPayloadHash hash = CanonicalPayloadHasher.ComputeHash(payloadToHash);

        Assert.Equal(CanonicalArtifactTypes.GovernanceOutboxEntry, payloadToHash.ArtifactType);
        Assert.Equal("outbox-1", payloadToHash.ArtifactId);
        Assert.Equal(CanonicalPayloadOptions.DefaultHashAlgorithm, hash.HashAlgorithm);
        Assert.Equal(CanonicalPayloadOptions.DefaultCanonicalizationVersion, hash.CanonicalizationVersion);
        Assert.Contains("\"artifactType\":\"asibackbone.governance-outbox-entry\"", payloadToHash.CanonicalJson, StringComparison.Ordinal);
    }

    [Fact]
    public void HashMetadataCarriesSigningHashWithoutImplyingSignature()
    {
        AuditLedgerRecord record = CreateAuditLedgerRecord(["alpha"], new Dictionary<string, string>());
        CanonicalPayload payload = CanonicalPayloadBuilder.ForAuditLedgerRecord(record);
        CanonicalPayloadHash hash = CanonicalPayloadHasher.ComputeHash(payload);

        var signingMetadata = hash.ToSigningMetadata();

        Assert.Equal(hash.HashValue, signingMetadata.SigningHash);
        Assert.Equal(CanonicalPayloadOptions.DefaultHashAlgorithm, signingMetadata.HashAlgorithm);
        Assert.False(signingMetadata.HasSignature);
        Assert.False(signingMetadata.IsSigned);
        Assert.Equal(CanonicalArtifactTypes.AuditLedgerRecord, signingMetadata.Metadata["artifact_type"]);
        Assert.Equal("record-1", signingMetadata.Metadata["artifact_id"]);
    }

    private static AuditLedgerRecord CreateAuditLedgerRecord(IEnumerable<string> reasonCodes, IReadOnlyDictionary<string, string> metadata)
    {
        IAsiBackboneActorContext actor = AsiBackboneActorContext.Service("system-1", "System");
        var residue = AuditResidue.Create(
            actor,
            "gateway.execute",
            "Allowed",
            reasonCodes,
            eventId: "event-1",
            occurredUtc: new DateTimeOffset(2026, 6, 16, 8, 0, 0, TimeSpan.FromHours(-5)),
            correlationId: "correlation-1",
            traceId: "trace-1",
            policyVersion: "policy-v1",
            policyHash: "policy-hash",
            metadata: metadata,
            auditResidueId: "residue-1",
            spanId: "span-1",
            parentSpanId: "parent-span-1",
            decisionLatencyMs: 42,
            constraintSetHash: "constraint-hash",
            constraintCount: 2,
            riskScore: 0.25,
            policyScope: "regional",
            tenantHash: "tenant-hash",
            organizationHash: "organization-hash",
            emitterStatus: "queued",
            emitterProvider: "provider-neutral",
            outboxSequence: 7,
            gatewayExecutionId: "gateway-1",
            decisionStage: "policy-evaluated");

        return AuditLedgerRecord.FromResidue(
            residue,
            recordId: "record-1",
            recordedUtc: new DateTimeOffset(2026, 6, 16, 13, 0, 1, TimeSpan.Zero),
            handshakeId: "handshake-1",
            acknowledgmentId: "ack-1",
            capabilityTokenId: "capability-grant-1",
            previousRecordHash: "previous-hash");
    }
}
