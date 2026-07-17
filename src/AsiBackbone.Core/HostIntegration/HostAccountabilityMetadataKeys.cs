namespace AsiBackbone.Core.HostIntegration;

/// <summary>
/// Provides stable metadata keys for linking governed execution lifecycle records to host-owned mutation evidence.
/// </summary>
/// <remarks>
/// Values stored under these keys should remain minimized and privacy-safe. Hosts should use opaque identifiers,
/// counts, hashes, and provider names rather than raw application values.
/// </remarks>
public static class HostAccountabilityMetadataKeys
{
    /// <summary>
    /// Identifies the logical governed operation execution across one or more attempts.
    /// </summary>
    public const string OperationExecutionId = "operationExecutionId";

    /// <summary>
    /// Identifies one execution attempt for the logical governed operation.
    /// </summary>
    public const string ExecutionAttemptId = "executionAttemptId";

    /// <summary>
    /// Identifies the host-owned persisted mutation batch, when one exists.
    /// </summary>
    public const string MutationBatchId = "mutationBatchId";

    /// <summary>
    /// Records the number of host-owned mutation records associated with the batch.
    /// </summary>
    public const string MutationRecordCount = "mutationRecordCount";

    /// <summary>
    /// Records the canonical hash of the privacy-safe host mutation manifest.
    /// </summary>
    public const string MutationManifestHash = "mutationManifestHash";

    /// <summary>
    /// Records the algorithm used to hash the privacy-safe host mutation manifest.
    /// </summary>
    public const string MutationManifestAlgorithm = "mutationManifestAlgorithm";

    /// <summary>
    /// Records the host persistence outcome represented by the execution receipt.
    /// </summary>
    public const string PersistenceOutcome = "persistenceOutcome";

    /// <summary>
    /// Records the host persistence provider or provider family, when supplied.
    /// </summary>
    public const string PersistenceProvider = "persistenceProvider";

    /// <summary>
    /// Indicates that the governed operation completed successfully without persisted mutation.
    /// </summary>
    public const string CompletedWithoutMutation = "completedWithoutMutation";

    /// <summary>
    /// Identifies the persisted AsiBackbone decision audit record associated with execution.
    /// </summary>
    public const string DecisionAuditRecordId = "decisionAuditRecordId";
}
