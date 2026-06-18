namespace CDCavell.AsiBackbone.AspNetCore.Endpoints;

/// <summary>
/// Allows a selected ASP.NET Core endpoint to pass through endpoint governance middleware
/// when strict governance metadata enforcement is enabled.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public sealed class AllowMissingGovernanceMetadataAttribute : Attribute
{
}
