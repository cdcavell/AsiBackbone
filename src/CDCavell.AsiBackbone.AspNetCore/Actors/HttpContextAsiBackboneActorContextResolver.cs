using System.Security.Claims;
using CDCavell.AsiBackbone.Core.Actors;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace CDCavell.AsiBackbone.AspNetCore.Actors;

/// <summary>
/// Maps the current ASP.NET Core HTTP context into a framework-neutral AsiBackbone actor context.
/// </summary>
public sealed class HttpContextAsiBackboneActorContextResolver : IAsiBackboneHttpActorContextResolver
{
    private readonly IHttpContextAccessor httpContextAccessor;
    private readonly AsiBackboneHttpActorContextOptions options;

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpContextAsiBackboneActorContextResolver" /> class.
    /// </summary>
    /// <param name="httpContextAccessor">The ASP.NET Core HTTP context accessor.</param>
    /// <param name="options">The actor mapping options.</param>
    public HttpContextAsiBackboneActorContextResolver(
        IHttpContextAccessor httpContextAccessor,
        IOptions<AsiBackboneHttpActorContextOptions> options)
    {
        this.httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        this.options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        this.options.Validate();
    }

    /// <inheritdoc />
    public IAsiBackboneActorContext ResolveActorContext()
    {
        ClaimsPrincipal? user = httpContextAccessor.HttpContext?.User;

        if (user?.Identity?.IsAuthenticated != true)
        {
            return ResolveUnauthenticatedActor;
        }

        string? actorId = FindFirstNonEmptyClaimValue(user, options.ActorIdClaimTypes);
        if (string.IsNullOrWhiteSpace(actorId))
        {
            return ResolveUnauthenticatedActor;
        }

        string? displayName = FindFirstNonEmptyClaimValue(user, options.DisplayNameClaimTypes);
        AsiBackboneActorType actorType = ResolveActorType(user);

        return actorType switch
        {
            AsiBackboneActorType.Service => AsiBackboneActorContext.Service(actorId, displayName),
            AsiBackboneActorType.System => AsiBackboneActorContext.System,
            AsiBackboneActorType.Agent => AsiBackboneActorContext.Agent(actorId, displayName),
            AsiBackboneActorType.Human => AsiBackboneActorContext.Human(actorId, displayName),
            AsiBackboneActorType.Unknown => AsiBackboneActorContext.Unknown,
            _ => AsiBackboneActorContext.Human(actorId, displayName),
        };
    }

    private IAsiBackboneActorContext ResolveUnauthenticatedActor => string.IsNullOrWhiteSpace(options.UnauthenticatedDisplayName)
            ? AsiBackboneActorContext.Unknown
            : AsiBackboneActorContext.Human(
                AsiBackboneActorContext.UnknownActorId,
                options.UnauthenticatedDisplayName,
                isAuthenticated: false);

    private AsiBackboneActorType ResolveActorType(ClaimsPrincipal user)
    {
        string? actorTypeValue = FindFirstNonEmptyClaimValue(user, new[] { options.ActorTypeClaimType });

        return Enum.TryParse(actorTypeValue, ignoreCase: true, out AsiBackboneActorType actorType)
            ? actorType
            : options.DefaultAuthenticatedActorType;
    }

    private static string? FindFirstNonEmptyClaimValue(ClaimsPrincipal user, IEnumerable<string> claimTypes)
    {
        foreach (string claimType in claimTypes.Where(static claimType => !string.IsNullOrWhiteSpace(claimType)))
        {
            Claim? claim = user.FindFirst(claimType);
            if (!string.IsNullOrWhiteSpace(claim?.Value))
            {
                return claim.Value.Trim();
            }
        }

        return null;
    }
}
