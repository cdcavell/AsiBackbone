namespace AsiBackbone.Samples.NcatAuditCompletionAdapter;

/// <summary>
/// Carries the minimized fields required to hand an NCAT mutation-audit completion into AsiBackbone.
/// </summary>
/// <remarks>
/// This source-neutral contract is intentionally limited to opaque identifiers, counts, hashes, and
/// completion state. It must not contain entity keys, original values, current values, request bodies,
/// secrets, or unrestricted exception details.
/// </remarks>
public sealed record NcatAuditCompletionHandoff(
    string CompletionEntryId,
    string PersistenceOutcome,
    DateTimeOffset CompletedUtc,
    string? OperationExecutionId = null,
    string? ExecutionAttemptId = null,
    string? DecisionAuditRecordId = null,
    string? CorrelationId = null,
    string? TraceId = null,
    string? MutationBatchId = null,
    int AuditRecordCount = 0,
    string? MutationManifestHash = null,
    string? MutationManifestAlgorithm = null,
    int DeliveryAttempt = 0);
