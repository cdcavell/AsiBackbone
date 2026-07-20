#pragma warning disable CS1591

using System.Security.Claims;
using AsiBackbone.AspNetCore.Actors;
using AsiBackbone.Core.Actors;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Xunit;

namespace AsiBackbone.AspNetCore.Tests.Actors;

/// <summary>
/// Tests for <see cref="HttpContextAsiBackboneActorContextResolver"/>.
/// </summary>
public sealed class HttpContextAsiBackboneActorContextResolverTests
{
    [Fact]
    public void ResolveActorContextMapsAuthenticatedUserFromDefaultClaims()
    {
        DefaultHttpContext httpContext = CreateHttpContext(
            new Claim(ClaimTypes.NameIdentifier, "  user-123  "),
            new Claim(ClaimTypes.Name, "  Ada Lovelace  "));

        IAsiBackboneActorContext actor = CreateResolver(httpContext).ResolveActorContext();

        Assert.Equal("user-123", actor.ActorId);
        Assert.Equal(AsiBackboneActorType.Human, actor.ActorType);
        Assert.Equal("Ada Lovelace", actor.DisplayName);
        Assert.True(actor.IsAuthenticated);
    }

    [Fact]
    public void ResolveActorContextAcceptsHumanClaimByDefault()
    {
        DefaultHttpContext httpContext = CreateHttpContext(
            new Claim(ClaimTypes.NameIdentifier, "human-1"),
            new Claim("actor_type", "Human"));

        IAsiBackboneActorContext actor = CreateResolver(httpContext).ResolveActorContext();

        Assert.Equal(AsiBackboneActorType.Human, actor.ActorType);
        Assert.Equal("human-1", actor.ActorId);
    }

    [Theory]
    [InlineData("System")]
    [InlineData("Service")]
    [InlineData("Agent")]
    [InlineData("Unknown")]
    public void ResolveActorContextRejectsNonHumanClaimsByDefault(string claimedActorType)
    {
        DefaultHttpContext httpContext = CreateHttpContext(
            new Claim(ClaimTypes.NameIdentifier, "caller-1"),
            new Claim("actor_type", claimedActorType));

        IAsiBackboneActorContext actor = CreateResolver(httpContext).ResolveActorContext();

        Assert.Equal(AsiBackboneActorType.Human, actor.ActorType);
        Assert.Equal("caller-1", actor.ActorId);
        Assert.True(actor.IsAuthenticated);
    }

    [Fact]
    public void ResolveActorContextAcceptsServiceAfterExplicitOptIn()
    {
        DefaultHttpContext httpContext = CreateHttpContext(
            new Claim("custom_id", "service-42"),
            new Claim("custom_name", "Service Worker"),
            new Claim("custom_actor_type", "Service"));

        IAsiBackboneActorContext actor = CreateResolver(
            httpContext,
            options =>
            {
                options.ActorIdClaimTypes = ["custom_id"];
                options.DisplayNameClaimTypes = ["custom_name"];
                options.ActorTypeClaimType = "custom_actor_type";
                options.AllowedActorTypesFromClaims = [AsiBackboneActorType.Human, AsiBackboneActorType.Service];
            }).ResolveActorContext();

        Assert.Equal("service-42", actor.ActorId);
        Assert.Equal(AsiBackboneActorType.Service, actor.ActorType);
        Assert.Equal("Service Worker", actor.DisplayName);
    }

    [Fact]
    public void ResolveActorContextAcceptsSystemAfterExplicitOptIn()
    {
        DefaultHttpContext httpContext = CreateHttpContext(
            new Claim(ClaimTypes.NameIdentifier, "ignored-system-id"),
            new Claim("actor_type", "System"));

        IAsiBackboneActorContext actor = CreateResolver(
            httpContext,
            options => options.AllowedActorTypesFromClaims = [AsiBackboneActorType.System]).ResolveActorContext();

        Assert.Equal(AsiBackboneActorContext.SystemActorId, actor.ActorId);
        Assert.Equal(AsiBackboneActorType.System, actor.ActorType);
    }

    [Fact]
    public void ResolveActorContextAcceptsAgentAfterExplicitOptIn()
    {
        DefaultHttpContext httpContext = CreateHttpContext(
            new Claim(ClaimTypes.NameIdentifier, "agent-007"),
            new Claim(ClaimTypes.Name, "Agent Runner"),
            new Claim("actor_type", "Agent"));

        IAsiBackboneActorContext actor = CreateResolver(
            httpContext,
            options => options.AllowedActorTypesFromClaims = [AsiBackboneActorType.Agent]).ResolveActorContext();

        Assert.Equal("agent-007", actor.ActorId);
        Assert.Equal(AsiBackboneActorType.Agent, actor.ActorType);
    }

    [Theory]
    [InlineData("not-a-valid-type")]
    [InlineData("999")]
    public void ResolveActorContextFallsBackForUnrecognizedOrUndefinedClaim(string claimedActorType)
    {
        DefaultHttpContext httpContext = CreateHttpContext(
            new Claim(ClaimTypes.NameIdentifier, "caller-1"),
            new Claim("actor_type", claimedActorType));

        IAsiBackboneActorContext actor = CreateResolver(
            httpContext,
            options => options.DefaultAuthenticatedActorType = AsiBackboneActorType.Agent).ResolveActorContext();

        Assert.Equal(AsiBackboneActorType.Agent, actor.ActorType);
        Assert.Equal("caller-1", actor.ActorId);
    }

    [Fact]
    public void ResolveActorContextDisablesClaimMappingWhenAllowedListIsEmpty()
    {
        DefaultHttpContext httpContext = CreateHttpContext(
            new Claim(ClaimTypes.NameIdentifier, "caller-1"),
            new Claim("actor_type", "Human"));

        IAsiBackboneActorContext actor = CreateResolver(
            httpContext,
            options =>
            {
                options.AllowedActorTypesFromClaims = [];
                options.DefaultAuthenticatedActorType = AsiBackboneActorType.Service;
            }).ResolveActorContext();

        Assert.Equal(AsiBackboneActorType.Service, actor.ActorType);
    }

    [Fact]
    public void ResolveActorContextRepresentsUnauthenticatedRequest()
    {
        DefaultHttpContext httpContext = new()
        {
            User = new ClaimsPrincipal(new ClaimsIdentity()),
        };

        IAsiBackboneActorContext actor = CreateResolver(httpContext).ResolveActorContext();

        Assert.Equal(AsiBackboneActorType.Unknown, actor.ActorType);
        Assert.False(actor.IsAuthenticated);
    }

    [Fact]
    public void ResolveActorContextRepresentsMissingActorIdentifierAsUnknown()
    {
        DefaultHttpContext httpContext = CreateHttpContext(new Claim(ClaimTypes.Name, "No Identifier"));

        IAsiBackboneActorContext actor = CreateResolver(httpContext).ResolveActorContext();

        Assert.Equal(AsiBackboneActorType.Unknown, actor.ActorType);
        Assert.False(actor.IsAuthenticated);
    }

    [Fact]
    public void ActorOptionsRejectNullAllowedActorTypes()
    {
        AsiBackboneHttpActorContextOptions options = new()
        {
            AllowedActorTypesFromClaims = null!,
        };

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(options.Validate);

        Assert.Contains(nameof(AsiBackboneHttpActorContextOptions.AllowedActorTypesFromClaims), exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ActorOptionsRejectUndefinedAllowedActorType()
    {
        AsiBackboneHttpActorContextOptions options = new()
        {
            AllowedActorTypesFromClaims = [(AsiBackboneActorType)999],
        };

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(options.Validate);

        Assert.Contains(nameof(AsiBackboneHttpActorContextOptions.AllowedActorTypesFromClaims), exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ActorOptionsRejectUndefinedDefaultActorType()
    {
        AsiBackboneHttpActorContextOptions options = new()
        {
            DefaultAuthenticatedActorType = (AsiBackboneActorType)999,
        };

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(options.Validate);

        Assert.Contains(nameof(AsiBackboneHttpActorContextOptions.DefaultAuthenticatedActorType), exception.Message, StringComparison.Ordinal);
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

        _ = Assert.Throws<InvalidOperationException>(options.Validate);
    }

    private static DefaultHttpContext CreateHttpContext(params Claim[] claims)
    {
        return new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType: "Test")),
        };
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
}
