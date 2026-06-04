namespace CDCavell.ASIBackbone.Core.Entities;

/// <summary>
/// Defines a contract for entities that expose an optimistic concurrency token.
/// </summary>
public interface IConcurrencyTrackedEntity
{
    /// <summary>
    /// Gets or sets the concurrency token used to detect conflicting updates.
    /// </summary>
    string ConcurrencyStamp { get; set; }
}
