using AsiBackbone.Core.Audit;

namespace AsiBackbone.Core.HostIntegration;

/// <summary>
/// Creates append-only audit lifecycle events that bind a governed decision to host execution evidence.
/// </summary>
public static class HostAccountabilityLifecycleEvent
{
    /// <summary>
    /// Creates a gateway-execution-started lifecycle event for one logical operation and attempt.
    /// </summary>
    public static AuditResidueLifecycleEvent ExecutionStarted(
        IAsiBackboneAuditResidue residue,
        string operationExecutionId,
        string? executionAttemptId = null,
        string? decisionAuditRecordId = null,
        string? eventId = null,
        DateTimeOffset? occurredUtc = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        ArgumentNullException.ThrowIfNull(residue);
        GovernedOperationExecutionReceipt identity = GovernedOperationExecutionReceipt.Create(
            operationExecutionId,
            GovernedOperationPersistenceOutcome.CompletedWithoutMutation,
            executionAttemptId: executionAttemptId,
            completedUtc: occurredUtc,
            decisionAuditRecordId: decisionAuditRecordId);

        return AuditResidueLifecycleEvent.FromResidue(
            AuditResidueLifecycleStage.GatewayExecutionStarted,
            residue,
            eventId: eventId,
            occurredUtc: occurredUtc,
            outcome: "Started",
            metadata: identity.ToLifecycleMetadata(metadata));
    }

    /// <summary>
    /// Creates a gateway-execution-completed lifecycle event from a typed host execution receipt.
    /// </summary>
    /// <remarks>
    /// The existing completion stage remains stable. Committed, failed, rolled-back, and no-mutation
    /// distinctions are carried by the typed receipt outcome and stable metadata keys rather than by
    /// expanding the lifecycle-stage enum.
    /// </remarks>
    public static AuditResidueLifecycleEvent FromExecutionReceipt(
        IAsiBackboneAuditResidue residue,
        GovernedOperationExecutionReceipt receipt,
        string? eventId = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        ArgumentNullException.ThrowIfNull(residue);
        ArgumentNullException.ThrowIfNull(receipt);

        return AuditResidueLifecycleEvent.FromResidue(
            AuditResidueLifecycleStage.GatewayExecutionCompleted,
            residue,
            eventId: eventId,
            occurredUtc: receipt.CompletedUtc,
            outcome: receipt.PersistenceOutcome.ToString(),
            metadata: receipt.ToLifecycleMetadata(metadata));
    }
}
