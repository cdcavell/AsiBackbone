using System.Globalization;
using CDCavell.AsiBackbone.Core.Audit;
using CDCavell.AsiBackbone.Core.Emissions;
using CDCavell.AsiBackbone.Core.Outbox;
using CDCavell.AsiBackbone.Core.Serialization;

namespace CDCavell.AsiBackbone.Core.Signing;

/// <summary>
/// Builds deterministic, provider-neutral signing payloads for AsiBackbone governance artifacts.
/// </summary>
public static class CanonicalPayloadBuilder
{
    /// <summary>
    /// Builds a canonical payload for audit residue.
    /// </summary>
    public static CanonicalPayload ForAuditResidue(IAsiBackboneAuditResidue residue, CanonicalPayloadOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(residue);
        CanonicalPayloadOptions effectiveOptions = options ?? CanonicalPayloadOptions.Default;

        return CanonicalPayload.Create(
            CanonicalArtifactTypes.AuditResidue,
            residue.AuditResidueId,
            residue.SchemaVersion,
            effectiveOptions.CanonicalizationVersion,
            BuildAuditResidueContent(residue, effectiveOptions));
    }

    /// <summary>
    /// Builds a canonical payload for a persistence-ready audit ledger record.
    /// </summary>
    public static CanonicalPayload ForAuditLedgerRecord(AuditLedgerRecord record, CanonicalPayloadOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(record);
        CanonicalPayloadOptions effectiveOptions = options ?? CanonicalPayloadOptions.Default;

        SortedDictionary<string, object?> content = BuildAuditResidueContent(record, effectiveOptions);
        content["acknowledgmentId"] = record.AcknowledgmentId;
        content["capabilityGrantId"] = record.CapabilityTokenId;
        content["handshakeId"] = record.HandshakeId;
        content["previousRecordHash"] = record.PreviousRecordHash;
        content["recordedUtc"] = FormatUtc(record.RecordedUtc);
        content["recordHash"] = record.RecordHash;
        content["recordId"] = record.RecordId;
        content["signatureAlgorithm"] = record.SignatureAlgorithm;
        content["signatureKeyId"] = record.SignatureKeyId;
        content["signatureKeyVersion"] = record.SignatureKeyVersion;
        content["signatureProvider"] = record.SignatureProvider;
        content["signatureValue"] = record.SignatureValue;
        content["signedUtc"] = FormatUtc(record.SignedUtc);
        content["signingHash"] = record.SigningHash;

        return CanonicalPayload.Create(
            CanonicalArtifactTypes.AuditLedgerRecord,
            record.RecordId,
            record.SchemaVersion,
            effectiveOptions.CanonicalizationVersion,
            content);
    }

    /// <summary>
    /// Builds a canonical payload for an audit residue lifecycle event.
    /// </summary>
    public static CanonicalPayload ForAuditResidueLifecycleEvent(AuditResidueLifecycleEvent lifecycleEvent, CanonicalPayloadOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(lifecycleEvent);
        CanonicalPayloadOptions effectiveOptions = options ?? CanonicalPayloadOptions.Default;

        SortedDictionary<string, object?> content = new(StringComparer.Ordinal)
        {
            ["auditResidueId"] = lifecycleEvent.AuditResidueId,
            ["correlationId"] = lifecycleEvent.CorrelationId,
            ["eventId"] = lifecycleEvent.EventId,
            ["metadata"] = FilterMetadata(lifecycleEvent.Metadata, effectiveOptions),
            ["occurredUtc"] = FormatUtc(lifecycleEvent.OccurredUtc),
            ["operationName"] = lifecycleEvent.OperationName,
            ["outcome"] = lifecycleEvent.Outcome,
            ["stage"] = lifecycleEvent.Stage.ToString(),
            ["stageSequence"] = lifecycleEvent.StageSequence,
            ["traceId"] = lifecycleEvent.TraceId
        };

        return CanonicalPayload.Create(
            CanonicalArtifactTypes.AuditResidueLifecycleEvent,
            lifecycleEvent.EventId,
            AsiBackboneSchemaVersions.StableArtifactsV1,
            effectiveOptions.CanonicalizationVersion,
            content);
    }

    /// <summary>
    /// Builds a canonical payload for a governance emission envelope.
    /// </summary>
    public static CanonicalPayload ForGovernanceEmissionEnvelope(GovernanceEmissionEnvelope envelope, CanonicalPayloadOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        CanonicalPayloadOptions effectiveOptions = options ?? CanonicalPayloadOptions.Default;

        return CanonicalPayload.Create(
            CanonicalArtifactTypes.GovernanceEmissionEnvelope,
            envelope.EnvelopeId,
            envelope.SchemaVersion,
            effectiveOptions.CanonicalizationVersion,
            BuildGovernanceEmissionEnvelopeContent(envelope, effectiveOptions));
    }

    /// <summary>
    /// Builds a canonical payload for a durable governance outbox entry.
    /// </summary>
    public static CanonicalPayload ForGovernanceOutboxEntry(GovernanceOutboxEntry entry, CanonicalPayloadOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(entry);
        CanonicalPayloadOptions effectiveOptions = options ?? CanonicalPayloadOptions.Default;

        SortedDictionary<string, object?> content = new(StringComparer.Ordinal)
        {
            ["createdUtc"] = FormatUtc(entry.CreatedUtc),
            ["deadLetterReason"] = entry.DeadLetterReason,
            ["envelope"] = BuildGovernanceEmissionEnvelopeContent(entry.Envelope, effectiveOptions),
            ["lastError"] = BuildGovernanceEmissionErrorContent(entry.LastError),
            ["maxRetryCount"] = entry.MaxRetryCount,
            ["metadata"] = FilterMetadata(entry.Metadata, effectiveOptions),
            ["nextRetryUtc"] = FormatUtc(entry.NextRetryUtc),
            ["outboxEntryId"] = entry.OutboxEntryId,
            ["providerName"] = entry.ProviderName,
            ["providerRecordId"] = entry.ProviderRecordId,
            ["retryCount"] = entry.RetryCount,
            ["status"] = entry.Status.ToString(),
            ["updatedUtc"] = FormatUtc(entry.UpdatedUtc)
        };

        return CanonicalPayload.Create(
            CanonicalArtifactTypes.GovernanceOutboxEntry,
            entry.OutboxEntryId,
            entry.Envelope.SchemaVersion,
            effectiveOptions.CanonicalizationVersion,
            content);
    }

    private static SortedDictionary<string, object?> BuildAuditResidueContent(IAsiBackboneAuditResidue residue, CanonicalPayloadOptions options)
    {
        return new SortedDictionary<string, object?>(StringComparer.Ordinal)
        {
            ["actorDisplayName"] = residue.ActorDisplayName,
            ["actorId"] = residue.ActorId,
            ["actorType"] = residue.ActorType.ToString(),
            ["auditResidueId"] = residue.AuditResidueId,
            ["constraintCount"] = residue.ConstraintCount,
            ["constraintSetHash"] = residue.ConstraintSetHash,
            ["correlationId"] = residue.CorrelationId,
            ["decisionLatencyMs"] = residue.DecisionLatencyMs,
            ["decisionStage"] = residue.DecisionStage,
            ["emitterProvider"] = residue.EmitterProvider,
            ["emitterStatus"] = residue.EmitterStatus,
            ["eventId"] = residue.EventId,
            ["gatewayExecutionId"] = residue.GatewayExecutionId,
            ["metadata"] = FilterMetadata(residue.Metadata, options),
            ["occurredUtc"] = FormatUtc(residue.OccurredUtc),
            ["operationName"] = residue.OperationName,
            ["organizationHash"] = residue.OrganizationHash,
            ["outboxSequence"] = residue.OutboxSequence,
            ["outcome"] = residue.Outcome,
            ["parentSpanId"] = residue.ParentSpanId,
            ["policyHash"] = residue.PolicyHash,
            ["policyScope"] = residue.PolicyScope,
            ["policyVersion"] = residue.PolicyVersion,
            ["reasonCodes"] = NormalizeStringSet(residue.ReasonCodes),
            ["riskScore"] = residue.RiskScore,
            ["schemaVersion"] = residue.SchemaVersion,
            ["spanId"] = residue.SpanId,
            ["tenantHash"] = residue.TenantHash,
            ["traceId"] = residue.TraceId
        };
    }

    private static SortedDictionary<string, object?> BuildGovernanceEmissionEnvelopeContent(GovernanceEmissionEnvelope envelope, CanonicalPayloadOptions options)
    {
        return new SortedDictionary<string, object?>(StringComparer.Ordinal)
        {
            ["actorId"] = envelope.ActorId,
            ["auditResidueId"] = envelope.AuditResidueId,
            ["correlationId"] = envelope.CorrelationId,
            ["createdUtc"] = FormatUtc(envelope.CreatedUtc),
            ["decisionStage"] = envelope.DecisionStage,
            ["emitterProvider"] = envelope.EmitterProvider,
            ["emitterStatus"] = envelope.EmitterStatus,
            ["envelopeId"] = envelope.EnvelopeId,
            ["eventId"] = envelope.EventId,
            ["eventType"] = envelope.EventType.ToString(),
            ["gatewayExecutionId"] = envelope.GatewayExecutionId,
            ["lifecycleStage"] = envelope.LifecycleStage?.ToString(),
            ["lifecycleStageSequence"] = envelope.LifecycleStageSequence,
            ["metadata"] = FilterMetadata(envelope.Metadata, options),
            ["occurredUtc"] = FormatUtc(envelope.OccurredUtc),
            ["operationName"] = envelope.OperationName,
            ["outboxSequence"] = envelope.OutboxSequence,
            ["outcome"] = envelope.Outcome,
            ["parentSpanId"] = envelope.ParentSpanId,
            ["payload"] = BuildGovernanceEmissionPayloadContent(envelope.Payload, options),
            ["policyHash"] = envelope.PolicyHash,
            ["policyVersion"] = envelope.PolicyVersion,
            ["schemaVersion"] = envelope.SchemaVersion,
            ["spanId"] = envelope.SpanId,
            ["traceId"] = envelope.TraceId
        };
    }

    private static SortedDictionary<string, object?>? BuildGovernanceEmissionPayloadContent(GovernanceEmissionPayload? payload, CanonicalPayloadOptions options)
    {
        if (payload is null)
        {
            return null;
        }

        return new SortedDictionary<string, object?>(StringComparer.Ordinal)
        {
            ["contentHash"] = payload.ContentHash,
            ["contentType"] = payload.ContentType,
            ["metadata"] = FilterMetadata(payload.Metadata, options),
            ["payloadType"] = payload.PayloadType,
            ["schemaVersion"] = payload.SchemaVersion,
            ["sizeBytes"] = payload.SizeBytes
        };
    }

    private static SortedDictionary<string, object?>? BuildGovernanceEmissionErrorContent(GovernanceEmissionError? error)
    {
        if (error is null)
        {
            return null;
        }

        return new SortedDictionary<string, object?>(StringComparer.Ordinal)
        {
            ["code"] = error.Code,
            ["isRetryable"] = error.IsRetryable,
            ["message"] = error.Message,
            ["providerErrorCode"] = error.ProviderErrorCode,
            ["providerName"] = error.ProviderName
        };
    }

    private static SortedDictionary<string, object?> FilterMetadata(IReadOnlyDictionary<string, string>? metadata, CanonicalPayloadOptions options)
    {
        SortedDictionary<string, object?> filteredMetadata = new(StringComparer.Ordinal);

        if (metadata is null || metadata.Count == 0)
        {
            return filteredMetadata;
        }

        foreach (KeyValuePair<string, string> item in metadata)
        {
            if (!options.AllowsMetadataKey(item.Key))
            {
                continue;
            }

            filteredMetadata[item.Key.Trim()] = item.Value?.Trim() ?? string.Empty;
        }

        return filteredMetadata;
    }

    private static string[] NormalizeStringSet(IEnumerable<string> values)
    {
        return values
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.Ordinal)
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();
    }

    private static string? FormatUtc(DateTimeOffset? timestamp)
    {
        return timestamp.HasValue ? FormatUtc(timestamp.Value) : null;
    }

    private static string FormatUtc(DateTimeOffset timestamp)
    {
        return timestamp.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss.fffffff'Z'", CultureInfo.InvariantCulture);
    }
}
