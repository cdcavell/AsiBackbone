using CDCavell.AsiBackbone.Core.Actors;
using CDCavell.AsiBackbone.Core.Audit;
using CDCavell.AsiBackbone.Core.Decisions;

namespace CDCavell.AsiBackbone.AspNetCore.Correlation;

/// <summary>
/// Provides helpers for applying ASP.NET Core request correlation data to framework-neutral audit residue.
/// </summary>
public static class AsiBackboneHttpRequestCorrelationAuditExtensions
{
    /// <summary>
    /// Creates audit residue from a governance decision and enriches it with safe ASP.NET Core request correlation data.
    /// </summary>
    /// <param name="correlation">The resolved ASP.NET Core request correlation data.</param>
    /// <param name="actor">The actor associated with the operation.</param>
    /// <param name="operationName">The operation name.</param>
    /// <param name="decision">The governance decision to audit.</param>
    /// <param name="eventId">Optional audit event identifier. When omitted, a new identifier is generated.</param>
    /// <param name="occurredUtc">Optional event timestamp. When omitted, the current UTC timestamp is used.</param>
    /// <param name="metadata">Optional host-provided audit metadata to merge with safe request metadata.</param>
    /// <returns>An enriched audit residue value.</returns>
    public static AuditResidue CreateAuditResidue(
        this AsiBackboneHttpRequestCorrelation correlation,
        IAsiBackboneActorContext actor,
        string operationName,
        GovernanceDecision decision,
        string? eventId = null,
        DateTimeOffset? occurredUtc = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        ArgumentNullException.ThrowIfNull(correlation);
        ArgumentNullException.ThrowIfNull(decision);

        return AuditResidue.Create(
            actor,
            operationName,
            decision.Outcome.ToString(),
            decision.ReasonCodes,
            eventId,
            occurredUtc,
            correlation.CorrelationId ?? decision.CorrelationId,
            correlation.TraceId ?? decision.TraceId,
            decision.PolicyVersion,
            decision.PolicyHash,
            correlation.MergeMetadata(metadata));
    }
}
