using CDCavell.ASIBackbone.Core.Actors;
using CDCavell.ASIBackbone.Core.Handshakes;
using Xunit;

namespace CDCavell.ASIBackbone.Core.Tests.Handshakes;

/// <summary>
/// Unit tests for the <see cref="LiabilityHandshakeAcknowledgment"/> class, which represents the acknowledgment response to a liability handshake request.
/// </summary>
public sealed class LiabilityHandshakeAcknowledgmentTests
{
    /// <summary>
    /// Verifies that the <see cref="LiabilityHandshakeAcknowledgment.Accept"/> method creates an acknowledgment with the expected properties when accepting a handshake request.
    /// </summary>
    [Fact]
    public void AcceptCreatesAcknowledgedResponse()
    {
        var actor = AsiBackboneActorContext.Human("user-123", "Chris");
        LiabilityHandshakeRequest request = CreateRequest(actor);

        var acknowledgment = LiabilityHandshakeAcknowledgment.Accept(
            request,
            actor,
            acknowledgmentId: "acknowledgment-123",
            occurredUtc: new DateTimeOffset(2026, 6, 4, 12, 0, 0, TimeSpan.Zero));

        Assert.Equal("acknowledgment-123", acknowledgment.AcknowledgmentId);
        Assert.Equal("handshake-123", acknowledgment.HandshakeId);
        Assert.Equal("user-123", acknowledgment.ActorId);
        Assert.Equal(AsiBackboneActorType.Human, acknowledgment.ActorType);
        Assert.Equal("Chris", acknowledgment.ActorDisplayName);
        Assert.Equal("ACK-001", acknowledgment.AcknowledgmentCode);
        Assert.True(acknowledgment.Acknowledged);
        Assert.False(acknowledgment.Rejected);
        Assert.Equal(new DateTimeOffset(2026, 6, 4, 12, 0, 0, TimeSpan.Zero), acknowledgment.OccurredUtc);
        Assert.Equal("correlation-123", acknowledgment.CorrelationId);
        Assert.Equal("trace-456", acknowledgment.TraceId);
    }

    /// <summary>
    /// Verifies that the <see cref="LiabilityHandshakeAcknowledgment.Reject"/> method creates an acknowledgment with the expected properties when rejecting a handshake request.
    /// </summary>
    [Fact]
    public void RejectCreatesRejectedResponse()
    {
        var actor = AsiBackboneActorContext.Human("user-123", "Chris");
        LiabilityHandshakeRequest request = CreateRequest(actor);

        var acknowledgment = LiabilityHandshakeAcknowledgment.Reject(
            request,
            actor,
            acknowledgmentId: "acknowledgment-123");

        Assert.False(acknowledgment.Acknowledged);
        Assert.True(acknowledgment.Rejected);
        Assert.Equal("ACK-001", acknowledgment.AcknowledgmentCode);
    }

    /// <summary>
    /// Verifies that the <see cref="LiabilityHandshakeAcknowledgment.Accept"/> method generates a non-empty acknowledgment ID when one is not provided.
    /// </summary>
    [Fact]
    public void CreateGeneratesAcknowledgmentIdWhenMissing()
    {
        AsiBackboneActorContext actor = AsiBackboneActorContext.System;
        LiabilityHandshakeRequest request = CreateRequest(actor);

        var acknowledgment = LiabilityHandshakeAcknowledgment.Accept(
            request,
            actor);

        Assert.False(string.IsNullOrWhiteSpace(acknowledgment.AcknowledgmentId));
    }

    /// <summary>
    /// Verifies that the <see cref="LiabilityHandshakeAcknowledgment.Accept"/> method normalizes the provided timestamp to UTC.
    /// </summary>
    [Fact]
    public void CreateNormalizesTimestampToUtc()
    {
        AsiBackboneActorContext actor = AsiBackboneActorContext.System;
        LiabilityHandshakeRequest request = CreateRequest(actor);

        var acknowledgment = LiabilityHandshakeAcknowledgment.Accept(
            request,
            actor,
            occurredUtc: new DateTimeOffset(2026, 6, 4, 7, 0, 0, TimeSpan.FromHours(-5)));

        Assert.Equal(new DateTimeOffset(2026, 6, 4, 12, 0, 0, TimeSpan.Zero), acknowledgment.OccurredUtc);
    }

    /// <summary>
    /// Verifies that the <see cref="LiabilityHandshakeAcknowledgment.Accept"/> method normalizes metadata keys and values by trimming whitespace and ignoring empty keys.
    /// </summary>
    [Fact]
    public void CreateNormalizesMetadata()
    {
        var actor = AsiBackboneActorContext.Service("service-123");
        LiabilityHandshakeRequest request = CreateRequest(actor);

        var acknowledgment = LiabilityHandshakeAcknowledgment.Accept(
            request,
            actor,
            metadata: new Dictionary<string, string>
            {
                [" source "] = " unit-test ",
                [" "] = "ignored"
            });

        Assert.True(acknowledgment.HasMetadata);
        Assert.Equal("unit-test", acknowledgment.Metadata["source"]);
        Assert.False(acknowledgment.Metadata.ContainsKey(" "));
    }

    /// <summary>
    /// Verifies that the <see cref="LiabilityHandshakeAcknowledgment.Accept"/> method throws an <see cref="ArgumentNullException"/> when the request parameter is null.
    /// </summary>
    [Fact]
    public void CreateThrowsForMissingRequest()
    {
        _ = Assert.Throws<ArgumentNullException>(() =>
            LiabilityHandshakeAcknowledgment.Accept(
                request: null!,
                actor: AsiBackboneActorContext.System));
    }

    /// <summary>
    /// Verifies that the <see cref="LiabilityHandshakeAcknowledgment.Accept"/> method throws an <see cref="ArgumentNullException"/> when the actor parameter is null.
    /// </summary>
    [Fact]
    public void CreateThrowsForMissingActor()
    {
        LiabilityHandshakeRequest request = CreateRequest(AsiBackboneActorContext.System);

        _ = Assert.Throws<ArgumentNullException>(() =>
            LiabilityHandshakeAcknowledgment.Accept(
                request,
                actor: null!));
    }

    private static LiabilityHandshakeRequest CreateRequest(IAsiBackboneActorContext actor)
    {
        return LiabilityHandshakeRequest.Create(
            actor,
            "document.approve",
            "ack.required",
            "Acknowledgment is required.",
            "ACK-001",
            "I understand this action is consequential.",
            LiabilityHandshakeRiskLevel.High,
            handshakeId: "handshake-123",
            correlationId: "correlation-123",
            traceId: "trace-456");
    }
}
