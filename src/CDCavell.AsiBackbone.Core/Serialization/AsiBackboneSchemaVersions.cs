namespace CDCavell.AsiBackbone.Core.Serialization;

/// <summary>
/// Defines schema version constants for persisted or exported AsiBackbone governance artifacts.
/// </summary>
public static class AsiBackboneSchemaVersions
{
    /// <summary>
    /// Gets the initial stable schema version for persisted or exported AsiBackbone governance artifacts.
    /// </summary>
    public const string StableArtifactsV1 = "1.0.0";

    /// <summary>
    /// Normalizes a caller-supplied schema version to the current stable artifact schema version when none is supplied.
    /// </summary>
    /// <param name="schemaVersion">The caller-supplied schema version.</param>
    /// <returns>The normalized schema version.</returns>
    public static string Normalize(string? schemaVersion)
    {
        return string.IsNullOrWhiteSpace(schemaVersion)
            ? StableArtifactsV1
            : schemaVersion.Trim();
    }
}
