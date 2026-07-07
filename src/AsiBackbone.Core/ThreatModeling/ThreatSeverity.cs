namespace AsiBackbone.Core.ThreatModeling;

/// <summary>
/// Represents the severity reported by a threat model contributor.
/// </summary>
public enum ThreatSeverity
{
    /// <summary>
    /// No threat indicators were observed.
    /// </summary>
    None = 0,

    /// <summary>
    /// Low-severity threat indicators were observed.
    /// </summary>
    Low = 1,

    /// <summary>
    /// Medium-severity threat indicators were observed.
    /// </summary>
    Medium = 2,

    /// <summary>
    /// High-severity threat indicators were observed.
    /// </summary>
    High = 3,

    /// <summary>
    /// Critical threat indicators were observed.
    /// </summary>
    Critical = 4
}
