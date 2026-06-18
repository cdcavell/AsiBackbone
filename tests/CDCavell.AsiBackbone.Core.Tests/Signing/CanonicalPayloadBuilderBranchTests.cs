using CDCavell.AsiBackbone.Core.Actors;
using CDCavell.AsiBackbone.Core.Audit;
using CDCavell.AsiBackbone.Core.Emissions;
using CDCavell.AsiBackbone.Core.Outbox;
using CDCavell.AsiBackbone.Core.Signing;
using Xunit;

namespace CDCavell.AsiBackbone.Core.Tests.Signing;

public sealed class CanonicalPayloadBuilderBranchTests
{
    private static readonly DateTimeOffset OccurredLocal = new(2026, 6, 18, 7, 0, 0, TimeSpan.FromHours(-5));
    private static readonly DateTimeOffset CreatedUtc = new(2026, 6, 18, 12, 0, 1, TimeSpan.Zero);
    private static readonly DateTimeOffset RetryUtc = new(2026, 6, 18, 12, 10, 0, TimeSpan.Zero);

    [Fact]
    public void ForAuditResidueUsesEventIdentifierWhenResidueIdIsMissingAndFiltersMetadata()
    {
        var options = CanonicalPayloadOptions.Create(["safe"]);
        var residue = AuditResidue.Create(
            AsiBackboneActorContext.Service(" system-1 ", " System "),
            " gateway.execute ",
            " Allowed ",
            reasonCodes: [" reason.beta ", "", "reason.alpha", "reason.beta"],
            eventId: " event-1 ",
            occurredUtc: OccurredLocal,
            correlationId: " correlation-1 ",
            traceId: " trace-1 ",
            metadata: new Dictionary<string, string>
            {
                ["safe"] = " included ",
                ["ignored"] = " excluded "
            },
            auditResidueId: " ");

        CanonicalPayload payload = CanonicalPayloadBuilder.ForAuditResidue(residue, options);

        Assert.Equal(CanonicalArtifactTypes.AuditResidue, payload.ArtifactType);
        Assert.Equal("event-1", payload.ArtifactId);
        Assert.Contains("\"auditResidueId\":\"event-1\"", payload.CanonicalJson, StringComparison.Ordinal);
        Assert.Contains("\"metadata\":{\"safe\":\"included\"}", payload.CanonicalJson, StringComparison.Ordinal);
        Assert.Contains("\"reasonCodes\":[\"reason.alpha\",\"reason.beta\"]", payload.CanonicalJson, StringComparison.Ordinal);
        Assert.DoesNotContain("ignored", payload.CanonicalJson, StringComparison.Ordinal);
    }

    [Fact]
    public void ForAuditResidueLifecycleEventIncludesFilteredMetadataAndStageSequence()
    {
        var options = CanonicalPayloadOptions.Create(["safe"]);
        var lifecycleEvent = AuditResidueLifecycleEvent.Create(
            AuditResidueLifecycleStage.ExternalEmissionQueued,
            " correlation-1 ",
            auditResidueId: " residue-1 ",
            eventId: " lifecycle-1 ",
            occurredUtc: OccurredLocal,
            traceId: " trace-1 ",
            operationName: " gateway.execute ",
            outcome: " Queued ",
            metadata: new Dictionary<string, string>
            {
                ["safe"] = " included ",
                ["ignored"] = " excluded "
            });

        CanonicalPayload payload = CanonicalPayloadBuilder.ForAuditResidueLifecycleEvent(lifecycleEvent, options);

        Assert.Equal(CanonicalArtifactTypes.AuditResidueLifecycleEvent, payload.ArtifactType);
        Assert.Equal("lifecycle-1", payload.ArtifactId);
        Assert.Contains("\"metadata\":{\"safe\":\"included\"}", payload.CanonicalJson, StringComparison.Ordinal);
        Assert.Contains($"\"stageSequence\":{(int)AuditResidueLifecycleStage.ExternalEmissionQueued}", payload.CanonicalJson, StringComparison.Ordinal);
        Assert.DoesNotContain("ignored", payload.CanonicalJson, StringComparison.Ordinal);
    }

    [Fact]
    public void ForGovernanceEmissionEnvelopeWritesNullPayloadWhenEnvelopeHasNoPayload()
    {
        GovernanceEmissionEnvelope envelope = CreateEnvelope(payload: null, metadata: null);

        CanonicalPayload payload = CanonicalPayloadBuilder.ForGovernanceEmissionEnvelope(envelope);

        Assert.Equal(CanonicalArtifactTypes.GovernanceEmissionEnvelope, payload.ArtifactType);
        Assert.Equal("envelope-event-1", payload.ArtifactId);
        Assert.Contains("\"metadata\":{}", payload.CanonicalJson, StringComparison.Ordinal);
        Assert.Contains("\"payload\":null", payload.CanonicalJson, StringComparison.Ordinal);
    }

    [Fact]
    public void ForGovernanceEmissionEnvelopeIncludesPayloadAndFilteredMetadataWhenPresent()
    {
        var options = CanonicalPayloadOptions.Create(["safe"]);
        GovernanceEmissionPayload emissionPayload = GovernanceEmissionPayload.Create(
            " audit-summary ",
            schemaVersion: " v1 ",
            contentType: " application/json ",
            contentHash: " payload-hash ",
            sizeBytes: 128,
            metadata: new Dictionary<string, string>
            {
                ["safe"] = " payload-included ",
                ["ignored"] = " payload-excluded "
            });
        GovernanceEmissionEnvelope envelope = CreateEnvelope(
            emissionPayload,
            new Dictionary<string, string>
            {
                ["safe"] = " envelope-included ",
                ["ignored"] = " envelope-excluded "
            });

        CanonicalPayload payload = CanonicalPayloadBuilder.ForGovernanceEmissionEnvelope(envelope, options);

        Assert.Contains("\"metadata\":{\"safe\":\"envelope-included\"}", payload.CanonicalJson, StringComparison.Ordinal);
        Assert.Contains("\"payload\":{", payload.CanonicalJson, StringComparison.Ordinal);
        Assert.Contains("\"metadata\":{\"safe\":\"payload-included\"}", payload.CanonicalJson, StringComparison.Ordinal);
        Assert.Contains("\"sizeBytes\":128", payload.CanonicalJson, StringComparison.Ordinal);
        Assert.DoesNotContain("excluded", payload.CanonicalJson, StringComparison.Ordinal);
    }

    [Fact]
    public void ForGovernanceOutboxEntryIncludesErrorContentAndRetryTimestamp()
    {
        GovernanceEmissionError error = GovernanceEmissionError.Create(
            " provider.timeout ",
            " Provider timed out. ",
            isRetryable: true,
            providerName: " sink ",
            providerErrorCode: " timeout ");
        GovernanceOutboxEntry entry = GovernanceOutboxEntry.Create(
            CreateEnvelope(payload: null, metadata: null),
            outboxEntryId: " outbox-1 ",
            createdUtc: CreatedUtc)
            .MarkFailed(error, RetryUtc, RetryUtc.AddMinutes(-1));

        CanonicalPayload payload = CanonicalPayloadBuilder.ForGovernanceOutboxEntry(entry);

        Assert.Equal(CanonicalArtifactTypes.GovernanceOutboxEntry, payload.ArtifactType);
        Assert.Equal("outbox-1", payload.ArtifactId);
        Assert.Contains("\"lastError\":{", payload.CanonicalJson, StringComparison.Ordinal);
        Assert.Contains("\"code\":\"provider.timeout\"", payload.CanonicalJson, StringComparison.Ordinal);
        Assert.Contains("\"isRetryable\":true", payload.CanonicalJson, StringComparison.Ordinal);
        Assert.Contains("\"nextRetryUtc\":\"2026-06-18T12:10:00.0000000Z\"", payload.CanonicalJson, StringComparison.Ordinal);
    }

    [Fact]
    public void ForGovernanceOutboxEntryWritesNullOptionalErrorAndRetryFields()
    {
        GovernanceOutboxEntry entry = GovernanceOutboxEntry.Create(
            CreateEnvelope(payload: null, metadata: null),
            outboxEntryId: " outbox-1 ",
            createdUtc: CreatedUtc);

        CanonicalPayload payload = CanonicalPayloadBuilder.ForGovernanceOutboxEntry(entry);

        Assert.Contains("\"deadLetterReason\":null", payload.CanonicalJson, StringComparison.Ordinal);
        Assert.Contains("\"lastError\":null", payload.CanonicalJson, StringComparison.Ordinal);
        Assert.Contains("\"nextRetryUtc\":null", payload.CanonicalJson, StringComparison.Ordinal);
        Assert.Contains("\"providerName\":null", payload.CanonicalJson, StringComparison.Ordinal);
        Assert.Contains("\"providerRecordId\":null", payload.CanonicalJson, StringComparison.Ordinal);
    }

    private static GovernanceEmissionEnvelope CreateEnvelope(
        GovernanceEmissionPayload? payload,
        IReadOnlyDictionary<string, string>? metadata)
    {
        return GovernanceEmissionEnvelope.Create(
            GovernanceEmissionEventType.AuditLifecycle,
            eventId: " event-1 ",
            occurredUtc: OccurredLocal,
            envelopeId: " envelope-event-1 ",
            createdUtc: CreatedUtc,
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
            payload: payload,
            metadata: metadata);
    }
}
