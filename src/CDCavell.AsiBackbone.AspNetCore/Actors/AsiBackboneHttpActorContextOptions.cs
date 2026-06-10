using System.Security.Claims;
using CDCavell.AsiBackbone.Core.Actors;

namespace CDCavell.AsiBackbone.AspNetCore.Actors;

/// <summary>
/// Configures how ASP.NET Core claims are mapped into framework-neutral actor contexts.
/// </summary>
public sealed class AsiBackboneHttpActorContextOptions
{
    /// <summary>
    /// Gets the default stable identifier claim types checked for authenticated actors.
    /// </summary>
    public static IReadOnlyList<string> DefaultActorIdClaimTypes { get; } =
    [
        ClaimTypes.NameIdentifier,
        "sub",
        "oid",
        "client_id",
        "azp",
        ClaimTypes.Email,
    ];

    /// <summary>
    /// Gets the default display-name claim types checked for authenticated actors.
    /// </summary>
    public static IReadOnlyList<string> DefaultDisplayNameClaimTypes { get; } =
    [
        ClaimTypes.Name,
        "name",
        "preferred_username",
        ClaimTypes.Email,
        "email",
    ];

    /// <summary>
    /// Gets or sets the claim types used to resolve a stable actor identifier.
    /// </summary>
    public IList<string> ActorIdClaimTypes { get; set; } = [.. DefaultActorIdClaimTypes];

    /// <summary>
    /// Gets or sets the claim types used to resolve an optional actor display name.
    /// </summary>
    public IList<string> DisplayNameClaimTypes { get; set; } = [.. DefaultDisplayNameClaimTypes];

    /// <summary>
    /// Gets or sets the claim type used to resolve an actor type.
    /// </summary>
    public string ActorTypeClaimType { get; set; } = "actor_type";

    /// <summary>
    /// Gets or sets the actor type used when an authenticated principal does not provide a valid actor type claim.
    /// </summary>
    public AsiBackboneActorType DefaultAuthenticatedActorType { get; set; } = AsiBackboneActorType.Human;

    /// <summary>
    /// Gets or sets the display name used for unauthenticated actors.
    /// </summary>
    public string? UnauthenticatedDisplayName { get; set; }

    /// <summary>
    /// Validates the options.
    /// </summary>
    public void Validate()
    {
        if (ActorIdClaimTypes is null || ActorIdClaimTypes.Count == 0 || ActorIdClaimTypes.All(string.IsNullOrWhiteSpace))
        {
            throw new InvalidOperationException("At least one actor identifier claim type must be configured.");
        }

        if (DisplayNameClaimTypes is null)
        {
            throw new InvalidOperationException("DisplayNameClaimTypes must be configured.");
        }

        if (string.IsNullOrWhiteSpace(ActorTypeClaimType))
        {
            throw new InvalidOperationException("ActorTypeClaimType must be configured.");
        }
    }
}
