using System.Text.Json;
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
    public void AuditResiduePayloadUsesEventIdWhenResidueIdIsBlankAndNormalizesReasonCodesAndMetadata()
    {
        var options = CanonicalPayloadOptions.Create(["allowed", "safe"]);
        var residue = new TestAuditResidue
        {
            EventId = "event-fallback",
            AuditResidueId = "   ",
            OccurredUtc = new DateTimeOffset(2026, 6, 16, 8, 0, 0, TimeSpan.FromHours(-5)),
            ReasonCodes = [" beta ", "", "alpha", "beta", " alpha "],
            Metadata = new Dictionary<string, string>
            {
                [" unsafe "] = " ignored ",
                [" safe "] = " included ",
                ["allowed"] = " second "
            }
        };

        CanonicalPayload payload = CanonicalPayloadBuilder.ForAuditResidue(residue, options);

        Assert.Equal(CanonicalArtifactTypes.AuditResidue, payload.ArtifactType);
        Assert.Equal("event-fallback", payload.ArtifactId);

        using JsonDocument document = Parse(payload);
        JsonElement content = document.RootElement.GetProperty("content");
        JsonElement metadata = content.GetProperty("metadata");

        Assert.Equal("event-fallback", document.RootElement.GetProperty("artifactId").GetString());
        Assert.Equal("event-fallback", content.GetProperty("auditResidueId").GetString());
        Assert.Equal("2026-06-16T13:00:00.0000000Z", content.GetProperty("occurredUtc").GetString());
        Assert.Equal(["alpha", "beta"], ReadStringArray(content.GetProperty("reasonCodes")));
        Assert.Equal("included", metadata.GetProperty("safe").GetString());
        Assert.Equal("second", metadata.GetProperty("allowed").GetString());
        Assert.False(metadata.TryGetProperty("unsafe", out _));
    }

    [Fact]
    public void AuditResidueLifecyclePayloadPreservesStageCorrelationUtcAndFilteredMetadata()
    {
        var options = CanonicalPayloadOptions.Create(["safe"]);
        AuditResidueLifecycleEvent lifecycleEvent = AuditResidueLifecycleEvent.Create(
            AuditResidueLifecycleStage.ExternalEmissionQueued,
            correlationId: " correlation-1 ",
            auditResidueId: " residue-1 ",
            eventId: " lifecycle-event-1 ",
            occurredUtc: new DateTimeOffset(2026, 6, 16, 8, 15, 16, 123, TimeSpan.FromHours(-5)),
            traceId: " trace-1 ",
            operationName: " gateway.execute ",
            outcome: " Queued ",
            metadata: new Dictionary<string, string>
            {
                [" safe "] = " lifecycle metadata ",
                ["unsafe"] = "ignored"
            });

        CanonicalPayload payload = CanonicalPayloadBuilder.ForAuditResidueLifecycleEvent(lifecycleEvent, options);

        Assert.Equal(CanonicalArtifactTypes.AuditResidueLifecycleEvent, payload.ArtifactType);
        Assert.Equal("lifecycle-event-1", payload.ArtifactId);

        using JsonDocument document = Parse(payload);
        JsonElement content = document.RootElement.GetProperty("content");
        JsonElement metadata = content.GetProperty("metadata");

        Assert.Equal("residue-1", content.GetProperty("auditResidueId").GetString());
        Assert.Equal("correlation-1", content.GetProperty("correlationId").GetString());
        Assert.Equal("lifecycle-event-1", content.GetProperty("eventId").GetString());
        Assert.Equal("2026-06-16T13:15:16.1230000Z", content.GetProperty("occurredUtc").GetString());
        Assert.Equal("gateway.execute", content.GetProperty("operationName").GetString());
        Assert.Equal("Queued", content.GetProperty("outcome").GetString());
        Assert.Equal("ExternalEmissionQueued", content.GetProperty("stage").GetString());
        Assert.Equal(500, content.GetProperty("stageSequence").GetInt32());
        Assert.Equal("trace-1", content.GetProperty("traceId").GetString());
        Assert.Equal("lifecycle metadata", metadata.GetProperty("safe").GetString());
        Assert.False(metadata.TryGetProperty("unsafe", out _));
    }

    [Fact]
    public void GovernanceEmissionEnvelopePayloadHandlesLifecycleFieldsPayloadPresenceAndMetadataFiltering()
    {
        var options = CanonicalPayloadOptions.Create(["safe"]);
        GovernanceEmissionPayload emissionPayload = GovernanceEmissionPayload.Create(
            " audit-summary ",
            schemaVersion: " payload-v1 ",
            contentType: " application/json ",
            contentHash: " payload-hash ",
            sizeBytes: 128,
            metadata: new Dictionary<string, string>
            {
                [" safe "] = " payload metadata ",
                ["unsafe"] = "ignored"
            });
        GovernanceEmissionEnvelope envelope = GovernanceEmissionEnvelope.Create(
            GovernanceEmissionEventType.AuditLifecycle,
            eventId: " lifecycle-event-1 ",
            occurredUtc: new DateTimeOffset(2026, 6, 16, 8, 0, 0, TimeSpan.FromHours(-5)),
            envelopeId: " envelope-1 ",
            createdUtc: new DateTimeOffset(2026, 6, 16, 13, 0, 1, TimeSpan.Zero),
            correlationId: " correlation-1 ",
            auditResidueId: " residue-1 ",
            lifecycleStage: AuditResidueLifecycleStage.ExternalEmissionQueued,
            policyVersion: " policy-v1 ",
            policyHash: " policy-hash ",
            traceId: " trace-1 ",
            spanId: " span-1 ",
            parentSpanId: " parent-span-1 ",
            operationName: " gateway.execute ",
            outcome: " Queued ",
            actorId: " actor-1 ",
            emitterStatus: " queued ",
            emitterProvider: " provider-neutral ",
            outboxSequence: 7,
            gatewayExecutionId: " gateway-1 ",
            decisionStage: " outbox-queued ",
            payload: emissionPayload,
            metadata: new Dictionary<string, string>
            {
                [" safe "] = " envelope metadata ",
                ["unsafe"] = "ignored"
            });

        CanonicalPayload canonicalPayload = CanonicalPayloadBuilder.ForGovernanceEmissionEnvelope(envelope, options);

        Assert.Equal(CanonicalArtifactTypes.GovernanceEmissionEnvelope, canonicalPayload.ArtifactType);
        Assert.Equal("envelope-1", canonicalPayload.ArtifactId);

        using JsonDocument document = Parse(canonicalPayload);
        JsonElement content = document.RootElement.GetProperty("content");
        JsonElement payloadContent = content.GetProperty("payload");
        JsonElement metadata = content.GetProperty("metadata");
        JsonElement payloadMetadata = payloadContent.GetProperty("metadata");

        Assert.Equal("AuditLifecycle", content.GetProperty("eventType").GetString());
        Assert.Equal("ExternalEmissionQueued", content.GetProperty("lifecycleStage").GetString());
        Assert.Equal(500, content.GetProperty("lifecycleStageSequence").GetInt32());
        Assert.Equal("2026-06-16T13:00:00.0000000Z", content.GetProperty("occurredUtc").GetString());
        Assert.Equal("2026-06-16T13:00:01.0000000Z", content.GetProperty("createdUtc").GetString());
        Assert.Equal("payload metadata", payloadMetadata.GetProperty("safe").GetString());
        Assert.False(payloadMetadata.TryGetProperty("unsafe", out _));
        Assert.Equal("envelope metadata", metadata.GetProperty("safe").GetString());
        Assert.False(metadata.TryGetProperty("unsafe", out _));
        Assert.Equal("audit-summary", payloadContent.GetProperty("payloadType").GetString());
        Assert.Equal("payload-v1", payloadContent.GetProperty("schemaVersion").GetString());
        Assert.Equal("application/json", payloadContent.GetProperty("contentType").GetString());
        Assert.Equal("payload-hash", payloadContent.GetProperty("contentHash").GetString());
        Assert.Equal(128, payloadContent.GetProperty("sizeBytes").GetInt64());
    }

    [Fact]
    public void GovernanceEmissionEnvelopePayloadWritesNullPayloadAndLifecycleFieldsWhenOmitted()
    {
        GovernanceEmissionEnvelope envelope = GovernanceEmissionEnvelope.Create(
            GovernanceEmissionEventType.AuditResidue,
            eventId: "event-1",
            occurredUtc: new DateTimeOffset(2026, 6, 16, 8, 0, 0, TimeSpan.FromHours(-5)),
            envelopeId: "envelope-without-payload",
            createdUtc: new DateTimeOffset(2026, 6, 16, 13, 0, 1, TimeSpan.Zero));

        CanonicalPayload payload = CanonicalPayloadBuilder.ForGovernanceEmissionEnvelope(envelope);

        using JsonDocument document = Parse(payload);
        JsonElement content = document.RootElement.GetProperty("content");

        Assert.Equal(JsonValueKind.Null, content.GetProperty("payload").ValueKind);
        Assert.Equal(JsonValueKind.Null, content.GetProperty("lifecycleStage").ValueKind);
        Assert.Equal(JsonValueKind.Null, content.GetProperty("lifecycleStageSequence").ValueKind);
    }

    [Fact]
    public void GovernanceOutboxEntryPayloadHandlesErrorRetryProviderDeadLetterAndMetadataFields()
    {
        var options = CanonicalPayloadOptions.Create(["safe"]);
        GovernanceEmissionEnvelope envelope = CreateEmissionEnvelope("envelope-outbox", "payload-hash");
        GovernanceEmissionError error = GovernanceEmissionError.Create(
            " provider.failure ",
            " Provider failed ",
            isRetryable: true,
            providerName: " provider-neutral ",
            providerErrorCode: " timeout ");
        GovernanceOutboxEntry entry = GovernanceOutboxEntry.Restore(
            envelope,
            GovernanceEmissionStatus.RetryableFailure,
            outboxEntryId: " outbox-1 ",
            createdUtc: new DateTimeOffset(2026, 6, 16, 8, 0, 0, TimeSpan.FromHours(-5)),
            updatedUtc: new DateTimeOffset(2026, 6, 16, 8, 30, 0, TimeSpan.FromHours(-5)),
            retryCount: 2,
            maxRetryCount: 5,
            nextRetryUtc: new DateTimeOffset(2026, 6, 16, 9, 0, 0, TimeSpan.FromHours(-5)),
            lastError: error,
            providerName: " provider-neutral ",
            providerRecordId: " provider-record-1 ",
            deadLetterReason: " dead letter ",
            metadata: new Dictionary<string, string>
            {
                [" safe "] = " outbox metadata ",
                ["unsafe"] = "ignored"
            });

        CanonicalPayload payload = CanonicalPayloadBuilder.ForGovernanceOutboxEntry(entry, options);

        Assert.Equal(CanonicalArtifactTypes.GovernanceOutboxEntry, payload.ArtifactType);
        Assert.Equal("outbox-1", payload.ArtifactId);

        using JsonDocument document = Parse(payload);
        JsonElement content = document.RootElement.GetProperty("content");
        JsonElement lastError = content.GetProperty("lastError");
        JsonElement metadata = content.GetProperty("metadata");

        Assert.Equal("2026-06-16T13:00:00.0000000Z", content.GetProperty("createdUtc").GetString());
        Assert.Equal("2026-06-16T13:30:00.0000000Z", content.GetProperty("updatedUtc").GetString());
        Assert.Equal("2026-06-16T14:00:00.0000000Z", content.GetProperty("nextRetryUtc").GetString());
        Assert.Equal("dead letter", content.GetProperty("deadLetterReason").GetString());
        Assert.Equal("provider-neutral", content.GetProperty("providerName").GetString());
        Assert.Equal("provider-record-1", content.GetProperty("providerRecordId").GetString());
        Assert.Equal(2, content.GetProperty("retryCount").GetInt32());
        Assert.Equal(5, content.GetProperty("maxRetryCount").GetInt32());
        Assert.Equal("RetryableFailure", content.GetProperty("status").GetString());
        Assert.Equal("provider.failure", lastError.GetProperty("code").GetString());
        Assert.True(lastError.GetProperty("isRetryable").GetBoolean());
        Assert.Equal("Provider failed", lastError.GetProperty("message").GetString());
        Assert.Equal("provider-neutral", lastError.GetProperty("providerName").GetString());
        Assert.Equal("timeout", lastError.GetProperty("providerErrorCode").GetString());
        Assert.Equal("outbox metadata", metadata.GetProperty("safe").GetString());
        Assert.False(metadata.TryGetProperty("unsafe", out _));
    }

    [Fact]
    public void GovernanceOutboxEntryPayloadWritesNullOptionalFieldsWhenAbsent()
    {
        GovernanceEmissionEnvelope envelope = CreateEmissionEnvelope("envelope-with-null-outbox-fields", "payload-hash");
        GovernanceOutboxEntry entry = GovernanceOutboxEntry.Create(
            envelope,
            outboxEntryId: "outbox-with-null-fields",
            createdUtc: new DateTimeOffset(2026, 6, 16, 13, 0, 0, TimeSpan.Zero));

        CanonicalPayload payload = CanonicalPayloadBuilder.ForGovernanceOutboxEntry(entry);

        using JsonDocument document = Parse(payload);
        JsonElement content = document.RootElement.GetProperty("content");

        Assert.Equal(JsonValueKind.Null, content.GetProperty("deadLetterReason").ValueKind);
        Assert.Equal(JsonValueKind.Null, content.GetProperty("lastError").ValueKind);
        Assert.Equal(JsonValueKind.Null, content.GetProperty("nextRetryUtc").ValueKind);
        Assert.Equal(JsonValueKind.Null, content.GetProperty("providerName").ValueKind);
        Assert.Equal(JsonValueKind.Null, content.GetProperty("providerRecordId").ValueKind);
    }

    [Fact]
    public void EquivalentGovernanceEmissionPayloadsProduceStableHashAndMeaningfulPayloadChangeChangesHash()
    {
        var options = CanonicalPayloadOptions.Create(["safe"]);
        GovernanceEmissionEnvelope first = CreateEmissionEnvelope(
            "envelope-hash-stability",
            "payload-hash",
            payloadMetadata: new Dictionary<string, string>
            {
                ["safe"] = "included",
                ["ignored"] = "one"
            });
        GovernanceEmissionEnvelope ignoredChange = CreateEmissionEnvelope(
            "envelope-hash-stability",
            "payload-hash",
            payloadMetadata: new Dictionary<string, string>
            {
                ["ignored"] = "two",
                ["safe"] = "included"
            });
        GovernanceEmissionEnvelope meaningfulChange = CreateEmissionEnvelope(
            "envelope-hash-stability",
            "payload-hash-changed",
            payloadMetadata: new Dictionary<string, string>
            {
                ["safe"] = "included",
                ["ignored"] = "one"
            });

        string firstHash = CanonicalPayloadHasher.ComputeHash(CanonicalPayloadBuilder.ForGovernanceEmissionEnvelope(first, options)).HashValue;
        string ignoredChangeHash = CanonicalPayloadHasher.ComputeHash(CanonicalPayloadBuilder.ForGovernanceEmissionEnvelope(ignoredChange, options)).HashValue;
        string meaningfulChangeHash = CanonicalPayloadHasher.ComputeHash(CanonicalPayloadBuilder.ForGovernanceEmissionEnvelope(meaningfulChange, options)).HashValue;

        Assert.Equal(firstHash, ignoredChangeHash);
        Assert.NotEqual(firstHash, meaningfulChangeHash);
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

    private static GovernanceEmissionEnvelope CreateEmissionEnvelope(
        string envelopeId,
        string contentHash,
        IReadOnlyDictionary<string, string>? payloadMetadata = null)
    {
        GovernanceEmissionPayload payload = GovernanceEmissionPayload.Create(
            "audit-summary",
            schemaVersion: "v1",
            contentType: "application/json",
            contentHash: contentHash,
            sizeBytes: 128,
            metadata: payloadMetadata ?? new Dictionary<string, string>());

        return GovernanceEmissionEnvelope.Create(
            GovernanceEmissionEventType.AuditResidue,
            eventId: "event-1",
            occurredUtc: new DateTimeOffset(2026, 6, 16, 8, 0, 0, TimeSpan.FromHours(-5)),
            envelopeId: envelopeId,
            createdUtc: new DateTimeOffset(2026, 6, 16, 13, 0, 1, TimeSpan.Zero),
            correlationId: "correlation-1",
            auditResidueId: "residue-1",
            policyVersion: "policy-v1",
            policyHash: "policy-hash",
            payload: payload);
    }

    private static JsonDocument Parse(CanonicalPayload payload)
    {
        return JsonDocument.Parse(payload.CanonicalJson);
    }

    private static string?[] ReadStringArray(JsonElement arrayElement)
    {
        return [.. arrayElement.EnumerateArray().Select(item => item.GetString())];
    }

    private sealed class TestAuditResidue : IAsiBackboneAuditResidue
    {
        public string EventId { get; init; } = "event-1";

        public string? AuditResidueId { get; init; } = "residue-1";

        public string SchemaVersion { get; init; } = "asibackbone.stable-artifacts.v1";

        public DateTimeOffset OccurredUtc { get; init; } = new(2026, 6, 16, 13, 0, 0, TimeSpan.Zero);

        public string ActorId { get; init; } = "actor-1";

        public AsiBackboneActorType ActorType { get; init; } = AsiBackboneActorType.Service;

        public string? ActorDisplayName { get; init; } = "System";

        public string OperationName { get; init; } = "gateway.execute";

        public string Outcome { get; init; } = "Allowed";

        public IReadOnlyList<string> ReasonCodes { get; init; } = [];

        public string? CorrelationId { get; init; } = "correlation-1";

        public string? TraceId { get; init; } = "trace-1";

        public string? SpanId { get; init; } = "span-1";

        public string? ParentSpanId { get; init; } = "parent-span-1";

        public long? DecisionLatencyMs { get; init; } = 42;

        public string? ConstraintSetHash { get; init; } = "constraint-hash";

        public int? ConstraintCount { get; init; } = 2;

        public double? RiskScore { get; init; } = 0.25;

        public string? PolicyScope { get; init; } = "regional";

        public string? TenantHash { get; init; } = "tenant-hash";

        public string? OrganizationHash { get; init; } = "organization-hash";

        public string? EmitterStatus { get; init; } = "queued";

        public string? EmitterProvider { get; init; } = "provider-neutral";

        public long? OutboxSequence { get; init; } = 7;

        public string? GatewayExecutionId { get; init; } = "gateway-1";

        public string? DecisionStage { get; init; } = "policy-evaluated";

        public string? PolicyVersion { get; init; } = "policy-v1";

        public string? PolicyHash { get; init; } = "policy-hash";

        public IReadOnlyDictionary<string, string> Metadata { get; init; } = new Dictionary<string, string>();
    }
}
