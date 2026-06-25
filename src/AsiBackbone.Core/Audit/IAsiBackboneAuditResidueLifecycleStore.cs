namespace AsiBackbone.Core.Audit;

/// <summary>
/// Defines a provider-neutral durable store for audit residue lifecycle events.
/// </summary>
public interface IAsiBackboneAuditResidueLifecycleStore
{
    /// <summary>
    /// Appends an audit residue lifecycle event before optional downstream provider delivery is attempted.
    /// </summary>
    ValueTask<AuditResidueLifecycleEvent> AppendAsync(
        AuditResidueLifecycleEvent lifecycleEvent,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds a lifecycle event by its stable event identifier.
    /// </summary>
    ValueTask<AuditResidueLifecycleEvent?> FindByEventIdAsync(
        string eventId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds lifecycle events by correlation identifier.
    /// </summary>
    ValueTask<IReadOnlyList<AuditResidueLifecycleEvent>> FindByCorrelationIdAsync(
        string correlationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds lifecycle events by audit residue identifier.
    /// </summary>
    ValueTask<IReadOnlyList<AuditResidueLifecycleEvent>> FindByAuditResidueIdAsync(
        string auditResidueId,
        CancellationToken cancellationToken = default);
}
