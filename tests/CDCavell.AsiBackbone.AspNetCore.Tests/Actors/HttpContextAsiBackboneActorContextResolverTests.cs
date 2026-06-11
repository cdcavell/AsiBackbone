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
    public void ResolveActorContextMapsSystemActorTypeToSystemContext()
    {
        DefaultHttpContext httpContext = new()
        {
            User = CreatePrincipal(
                new Claim(ClaimTypes.NameIdentifier, "ignored-system-id"),
                new Claim("actor_type", "System")),
        };

        HttpContextAsiBackboneActorContextResolver resolver = CreateResolver(httpContext);

        IAsiBackboneActorContext actorContext = resolver.ResolveActorContext();

        Assert.Equal(AsiBackboneActorContext.SystemActorId, actorContext.ActorId);
        Assert.Equal(AsiBackboneActorType.System, actorContext.ActorType);
        Assert.Equal("System", actorContext.DisplayName);
        Assert.True(actorContext.IsKnown);
        Assert.True(actorContext.IsAuthenticated);
    }

    [Fact]
    public void ResolveActorContextMapsAgentActorType()
    {
        DefaultHttpContext httpContext = new()
        {
            User = CreatePrincipal(
                new Claim(ClaimTypes.NameIdentifier, "agent-007"),
                new Claim(ClaimTypes.Name, "Agent Runner"),
                new Claim("actor_type", "Agent")),
        };

        HttpContextAsiBackboneActorContextResolver resolver = CreateResolver(httpContext);

        IAsiBackboneActorContext actorContext = resolver.ResolveActorContext();

        Assert.Equal("agent-007", actorContext.ActorId);
        Assert.Equal(AsiBackboneActorType.Agent, actorContext.ActorType);
        Assert.Equal("Agent Runner", actorContext.DisplayName);
        Assert.True(actorContext.IsKnown);
        Assert.True(actorContext.IsAuthenticated);
    }

    [Fact]
    public void ResolveActorContextMapsUnknownActorTypeClaimToUnknownContext()
    {
        DefaultHttpContext httpContext = new()
        {
            User = CreatePrincipal(
                new Claim(ClaimTypes.NameIdentifier, "unknown-actor"),
                new Claim("actor_type", "Unknown")),
        };

        HttpContextAsiBackboneActorContextResolver resolver = CreateResolver(httpContext);

        IAsiBackboneActorContext actorContext = resolver.ResolveActorContext();

        Assert.Equal(AsiBackboneActorContext.UnknownActorId, actorContext.ActorId);
        Assert.Equal(AsiBackboneActorType.Unknown, actorContext.ActorType);
        Assert.False(actorContext.IsKnown);
        Assert.False(actorContext.IsAuthenticated);
    }

    [Fact]
    public void ResolveActorContextFallsBackToConfiguredDefaultActorTypeWhenClaimIsInvalid()
    {
        DefaultHttpContext httpContext = new()
        {
            User = CreatePrincipal(
                new Claim(ClaimTypes.NameIdentifier, "agent-default"),
                new Claim("actor_type", "not-a-valid-type")),
        };

        HttpContextAsiBackboneActorContextResolver resolver = CreateResolver(
            httpContext,
            options => options.DefaultAuthenticatedActorType = AsiBackboneActorType.Agent);

        IAsiBackboneActorContext actorContext = resolver.ResolveActorContext();

        Assert.Equal("agent-default", actorContext.ActorId);
        Assert.Equal(AsiBackboneActorType.Agent, actorContext.ActorType);
        Assert.True(actorContext.IsKnown);
        Assert.True(actorContext.IsAuthenticated);
    }

    [Fact]
    public void ResolveActorContextFallsBackToHumanForUndefinedParsedActorType()
    {
        DefaultHttpContext httpContext = new()
        {
            User = CreatePrincipal(
                new Claim(ClaimTypes.NameIdentifier, "numeric-actor"),
                new Claim("actor_type", "999")),
        };

        HttpContextAsiBackboneActorContextResolver resolver = CreateResolver(httpContext);

        IAsiBackboneActorContext actorContext = resolver.ResolveActorContext();

        Assert.Equal("numeric-actor", actorContext.ActorId);
        Assert.Equal(AsiBackboneActorType.Human, actorContext.ActorType);
        Assert.True(actorContext.IsKnown);
        Assert.True(actorContext.IsAuthenticated);
    }

    [Fact]
    public void ResolveActorContextSkipsBlankConfiguredClaimTypesAndBlankClaimValues()
    {
        DefaultHttpContext httpContext = new()
        {
            User = CreatePrincipal(
                new Claim(ClaimTypes.NameIdentifier, "   "),
                new Claim("sub", "subject-456"),
                new Claim(ClaimTypes.Email, "display@example.test")),
        };

        HttpContextAsiBackboneActorContextResolver resolver = CreateResolver(
            httpContext,
            options =>
            {
                options.ActorIdClaimTypes = [" ", ClaimTypes.NameIdentifier, "sub"];
                options.DisplayNameClaimTypes = [" ", "missing_display", ClaimTypes.Email];
            });

        IAsiBackboneActorContext actorContext = resolver.ResolveActorContext();

        Assert.Equal("subject-456", actorContext.ActorId);
        Assert.Equal("display@example.test", actorContext.DisplayName);
        Assert.Equal(AsiBackboneActorType.Human, actorContext.ActorType);
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
    public void ResolveActorContextUsesConfiguredUnauthenticatedDisplayName()
    {
        DefaultHttpContext httpContext = new()
        {
            User = new ClaimsPrincipal(new ClaimsIdentity()),
        };

        HttpContextAsiBackboneActorContextResolver resolver = CreateResolver(
            httpContext,
            options => options.UnauthenticatedDisplayName = " Guest Actor ");

        IAsiBackboneActorContext actorContext = resolver.ResolveActorContext();

        Assert.Equal(AsiBackboneActorContext.UnknownActorId, actorContext.ActorId);
        Assert.Equal(AsiBackboneActorType.Human, actorContext.ActorType);
        Assert.Equal("Guest Actor", actorContext.DisplayName);
        Assert.True(actorContext.IsKnown);
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

    [Fact]
    public void ConstructorRejectsNullHttpContextAccessor()
    {
        _ = Assert.Throws<ArgumentNullException>(() =>
            new HttpContextAsiBackboneActorContextResolver(null!, Options.Create(new AsiBackboneHttpActorContextOptions())));
    }

    [Fact]
    public void ConstructorRejectsNullOptionsWrapper()
    {
        _ = Assert.Throws<ArgumentNullException>(() =>
            new HttpContextAsiBackboneActorContextResolver(new HttpContextAccessor(), null!));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("empty")]
    [InlineData("blank")]
    public void ActorOptionsRejectInvalidActorIdentifierClaimTypes(string? mode)
    {
        AsiBackboneHttpActorContextOptions options = new()
        {
            ActorIdClaimTypes = mode is null ? null! : mode == "empty" ? [] : [" ", ""]
        };

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(options.Validate);

        Assert.Contains("actor identifier claim type", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ActorOptionsRejectNullDisplayNameClaimTypes()
    {
        AsiBackboneHttpActorContextOptions options = new()
        {
            DisplayNameClaimTypes = null!,
        };

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(options.Validate);

        Assert.Contains(nameof(AsiBackboneHttpActorContextOptions.DisplayNameClaimTypes), exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ActorOptionsRejectBlankActorTypeClaimType(string? claimType)
    {
        AsiBackboneHttpActorContextOptions options = new()
        {
            ActorTypeClaimType = claimType!,
        };

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(options.Validate);

        Assert.Contains(nameof(AsiBackboneHttpActorContextOptions.ActorTypeClaimType), exception.Message, StringComparison.Ordinal);
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
