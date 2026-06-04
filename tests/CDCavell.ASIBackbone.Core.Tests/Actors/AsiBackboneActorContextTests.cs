using CDCavell.ASIBackbone.Core.Actors;
using CDCavell.ASIBackbone.Core.Constraints;
using Xunit;

namespace CDCavell.ASIBackbone.Core.Tests.Actors;

public sealed class AsiBackboneActorContextTests
{
    /// <summary>
    /// Verifies that the unknown actor context represents an unauthenticated unknown actor.
    /// </summary>
    [Fact]
    public void UnknownRepresentsUnauthenticatedUnknownActor()
    {
        AsiBackboneActorContext actorContext = AsiBackboneActorContext.Unknown;

        Assert.Equal(AsiBackboneActorContext.UnknownActorId, actorContext.ActorId);
        Assert.Equal(AsiBackboneActorType.Unknown, actorContext.ActorType);
        Assert.Null(actorContext.DisplayName);
        Assert.False(actorContext.IsKnown);
        Assert.False(actorContext.IsAuthenticated);
        _ = Assert.IsAssignableFrom<IAsiBackboneActorContext>(actorContext);
    }

    /// <summary>
    /// Verifies that the system actor context represents a known authenticated system actor.
    /// </summary>
    [Fact]
    public void SystemRepresentsKnownAuthenticatedSystemActor()
    {
        AsiBackboneActorContext actorContext = AsiBackboneActorContext.System;

        Assert.Equal(AsiBackboneActorContext.SystemActorId, actorContext.ActorId);
        Assert.Equal(AsiBackboneActorType.System, actorContext.ActorType);
        Assert.Equal("System", actorContext.DisplayName);
        Assert.True(actorContext.IsKnown);
        Assert.True(actorContext.IsAuthenticated);
    }

    /// <summary>
    /// Verifies that the human factory creates a normalized human actor context.
    /// </summary>
    [Fact]
    public void HumanCreatesKnownHumanActorContext()
    {
        var actorContext = AsiBackboneActorContext.Human(
            " user-123 ",
            " Test User ");

        Assert.Equal("user-123", actorContext.ActorId);
        Assert.Equal(AsiBackboneActorType.Human, actorContext.ActorType);
        Assert.Equal("Test User", actorContext.DisplayName);
        Assert.True(actorContext.IsKnown);
        Assert.True(actorContext.IsAuthenticated);
    }

    /// <summary>
    /// Verifies that human actor context can represent a known but unauthenticated actor.
    /// </summary>
    [Fact]
    public void HumanCanRepresentKnownUnauthenticatedActor()
    {
        var actorContext = AsiBackboneActorContext.Human(
            "user-123",
            isAuthenticated: false);

        Assert.Equal("user-123", actorContext.ActorId);
        Assert.Equal(AsiBackboneActorType.Human, actorContext.ActorType);
        Assert.True(actorContext.IsKnown);
        Assert.False(actorContext.IsAuthenticated);
    }

    /// <summary>
    /// Verifies that the service factory creates a known authenticated service actor context.
    /// </summary>
    [Fact]
    public void ServiceCreatesKnownAuthenticatedServiceActorContext()
    {
        var actorContext = AsiBackboneActorContext.Service(
            " service-worker ",
            " Background Worker ");

        Assert.Equal("service-worker", actorContext.ActorId);
        Assert.Equal(AsiBackboneActorType.Service, actorContext.ActorType);
        Assert.Equal("Background Worker", actorContext.DisplayName);
        Assert.True(actorContext.IsKnown);
        Assert.True(actorContext.IsAuthenticated);
    }

    /// <summary>
    /// Verifies that the agent factory creates a known agent actor context.
    /// </summary>
    [Fact]
    public void AgentCreatesKnownAgentActorContext()
    {
        var actorContext = AsiBackboneActorContext.Agent("agent-001");

        Assert.Equal("agent-001", actorContext.ActorId);
        Assert.Equal(AsiBackboneActorType.Agent, actorContext.ActorType);
        Assert.Null(actorContext.DisplayName);
        Assert.True(actorContext.IsKnown);
        Assert.True(actorContext.IsAuthenticated);
    }

    /// <summary>
    /// Verifies that blank actor identifiers are rejected.
    /// </summary>
    [Fact]
    public void FactoryThrowsForBlankActorId()
    {
        _ = Assert.Throws<ArgumentException>(() => AsiBackboneActorContext.Human(" "));
        _ = Assert.Throws<ArgumentException>(() => AsiBackboneActorContext.Service(string.Empty));
        _ = Assert.Throws<ArgumentException>(() => AsiBackboneActorContext.Agent("\t"));
    }

    /// <summary>
    /// Verifies that blank display names are normalized to null.
    /// </summary>
    [Fact]
    public void BlankDisplayNameNormalizesToNull()
    {
        var actorContext = AsiBackboneActorContext.Human(
            "user-123",
            " ");

        Assert.Null(actorContext.DisplayName);
    }
}
