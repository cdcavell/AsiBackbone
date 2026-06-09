namespace CDCavell.ASIBackbone.Core.Audit;

/// <summary>
/// Receives audit residue produced by the governance spine.
/// </summary>
/// <remarks>
/// Core defines the contract only. Storage providers, hosts, or future integration packages own how and where residue is recorded.
/// </remarks>
public interface IAsiBackboneAuditSink
{
    /// <summary>
    /// Records a single audit residue value.
    /// </summary>
    /// <param name="residue">The audit residue to record.</param>
    /// <param name="cancellationToken">A token that can cancel the write.</param>
    /// <returns>A task that completes when the residue has been recorded.</returns>
    ValueTask WriteAsync(
        IAsiBackboneAuditResidue residue,
        CancellationToken cancellationToken = default);
}
