using System.Security.Claims;
using CDCavell.AsiBackbone.AspNetCore.Actors;
using CDCavell.AsiBackbone.Core.Actors;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Xunit;

namespace CDCavell.AsiBackbone.AspNetCore.Tests.Actors;

public sealed class HttpContextAsiBackboneActorContextResolverTests
{
    [Fact]
    public void ResolveActorContextMapsAuthenticatedUserFromDefaultClaims()
    {
        DefaultHttpContext httpContext = new()
        {
            User = CreatePrincipal(
                new Claim(ClaimTypes.NameIdentifier, "  user-123  "),
                new Claim(ClaimTypes.Name, "  Ada Lovelace  ")),
        };

        HttpContextAsiBackboneActorContextResolver resolver = CreateResolver(httpContext);

        IAsiBackboneActorContext actorContext = resolver.ResolveActorContext();

        Assert.Equal("user-123", actorContext.ActorId);
        Assert.Equal(AsiBackboneActorType.Human, actorContext.ActorType);
        Assert.Equal("Ada Lovelace", actorContext.DisplayName);
        Assert.True(actorContext.IsKnown);
        Assert.True(actorContext.IsAuthenticated);
    }

    [Fact]
    public void ResolveActorContextUsesConfiguredClaimTypesAndActorType()
    {
        DefaultHttpContext httpContext = new()
        {
            User = CreatePrincipal(
                new Claim("custom_id", "service-42"),
                new Claim("custom_name", "Service Worker"),
                new Claim("custom_actor_type", "Service")),
        };

        HttpContextAsiBackboneActorContextResolver resolver = CreateResolver(
            httpContext,
            options =>
            {
                options.ActorIdClaimTypes = ["custom_id"];
                options.DisplayNameClaimTypes = ["custom_name"];
                options.ActorTypeClaimType = "custom_actor_type";
            });

        IAsiBackboneActorContext actorContext = resolver.ResolveActorContext();

        Assert.Equal("service-42", actorContext.ActorId);
        Assert.Equal(AsiBackboneActorType.Service, actorContext.ActorType);
        Assert.Equal("Service Worker", actorContext.DisplayName);
        Assert.True(actorContext.IsKnown);
        Assert.True(actorContext.IsAuthenticated);
    }

    [Fact]
    public void ResolveActorContextRepresentsUnauthenticatedRequestWithoutThrowing()
    {
        DefaultHttpContext httpContext = new()
        {
            User = new ClaimsPrincipal(new ClaimsIdentity()),
        };

        HttpContextAsiBackboneActorContextResolver resolver = CreateResolver(httpContext);

        IAsiBackboneActorContext actorContext = resolver.ResolveActorContext();

        Assert.Equal(AsiBackboneActorContext.UnknownActorId, actorContext.ActorId);
        Assert.Equal(AsiBackboneActorType.Unknown, actorContext.ActorType);
        Assert.Null(actorContext.DisplayName);
        Assert.False(actorContext.IsKnown);
        Assert.False(actorContext.IsAuthenticated);
    }

    [Fact]
    public void ResolveActorContextRepresentsMissingActorIdClaimWithoutThrowing()
    {
        DefaultHttpContext httpContext = new()
        {
            User = CreatePrincipal(new Claim(ClaimTypes.Name, "No Identifier")),
        };

        HttpContextAsiBackboneActorContextResolver resolver = CreateResolver(httpContext);

        IAsiBackboneActorContext actorContext = resolver.ResolveActorContext();

        Assert.Equal(AsiBackboneActorContext.UnknownActorId, actorContext.ActorId);
        Assert.Equal(AsiBackboneActorType.Unknown, actorContext.ActorType);
        Assert.False(actorContext.IsKnown);
        Assert.False(actorContext.IsAuthenticated);
    }

    [Fact]
    public void ResolveActorContextRepresentsBackgroundScenarioWithoutThrowing()
    {
        HttpContextAccessor httpContextAccessor = new();
        HttpContextAsiBackboneActorContextResolver resolver = new(
            httpContextAccessor,
            Options.Create(new AsiBackboneHttpActorContextOptions()));

        IAsiBackboneActorContext actorContext = resolver.ResolveActorContext();

        Assert.Equal(AsiBackboneActorContext.UnknownActorId, actorContext.ActorId);
        Assert.Equal(AsiBackboneActorType.Unknown, actorContext.ActorType);
        Assert.False(actorContext.IsKnown);
        Assert.False(actorContext.IsAuthenticated);
    }

    private static HttpContextAsiBackboneActorContextResolver CreateResolver(
        HttpContext httpContext,
        Action<AsiBackboneHttpActorContextOptions>? configure = null)
    {
        AsiBackboneHttpActorContextOptions options = new();
        configure?.Invoke(options);

        return new HttpContextAsiBackboneActorContextResolver(
            new HttpContextAccessor { HttpContext = httpContext },
            Options.Create(options));
    }

    private static ClaimsPrincipal CreatePrincipal(params Claim[] claims)
    {
        return new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType: "Test"));
    }
}
