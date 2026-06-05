using CDCavell.AsiBackbone.Core.Entities;

namespace CDCavell.AsiBackbone.EntityFrameworkCore.Persistence;

/// <summary>
/// Represents a normalized reason-code row associated with an AsiBackbone audit ledger record.
/// </summary>
public sealed class AsiBackboneAuditLedgerReasonCodeEntity : AsiBackboneEntity
{
    /// <summary>
    /// Gets or sets the identifier of the parent audit ledger record.
    /// </summary>
    public Guid AuditLedgerRecordId { get; set; }

    /// <summary>
    /// Gets or sets the zero-based display or persistence order for the reason code.
    /// </summary>
    public int Sequence { get; set; }

    /// <summary>
    /// Gets or sets the machine-readable reason code.
    /// </summary>
    public string ReasonCode { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the parent audit ledger record.
    /// </summary>
    public AsiBackboneAuditLedgerRecordEntity AuditLedgerRecord { get; set; } = null!;
}
