using AsiBackbone.Core.Emissions;
using AsiBackbone.Core.Outbox;
using Xunit;

namespace AsiBackbone.Core.Tests.Outbox;

/// <summary>
/// Covers the additive claimed-transition result contract.
/// </summary>
public sealed class GovernanceOutboxClaimTransitionResultTests
{
    /// <summary>
    /// Verifies applied outcomes are surfaced explicitly.
    /// </summary>
    [Fact]
    public void CreateExposesAppliedOutcome()
    {
        GovernanceOutboxEntry entry = CreateEntry();

        var result = GovernanceOutboxClaimTransitionResult.Create(
            entry,
            GovernanceOutboxClaimTransitionOutcome.Applied);

        Assert.Same(entry, result.Entry);
        Assert.Equal(GovernanceOutboxClaimTransitionOutcome.Applied, result.Outcome);
        Assert.True(result.IsApplied);
    }

    /// <summary>
    /// Verifies non-applied outcomes do not report caller-owned persistence.
    /// </summary>
    [Theory]
    [InlineData(GovernanceOutboxClaimTransitionOutcome.StaleClaim)]
    [InlineData(GovernanceOutboxClaimTransitionOutcome.Terminal)]
    [InlineData(GovernanceOutboxClaimTransitionOutcome.ConcurrencyLost)]
    [InlineData(GovernanceOutboxClaimTransitionOutcome.Missing)]
    public void CreateExposesNonAppliedOutcome(GovernanceOutboxClaimTransitionOutcome outcome)
    {
        var result = GovernanceOutboxClaimTransitionResult.Create(CreateEntry(), outcome);

        Assert.Equal(outcome, result.Outcome);
        Assert.False(result.IsApplied);
    }

    /// <summary>
    /// Verifies null entries are rejected.
    /// </summary>
    [Fact]
    public void CreateRejectsNullEntry()
    {
        _ = Assert.Throws<ArgumentNullException>(() => GovernanceOutboxClaimTransitionResult.Create(
            null!,
            GovernanceOutboxClaimTransitionOutcome.Applied));
    }

    /// <summary>
    /// Verifies undefined outcomes are rejected.
    /// </summary>
    [Fact]
    public void CreateRejectsUndefinedOutcome()
    {
        _ = Assert.Throws<ArgumentOutOfRangeException>(() => GovernanceOutboxClaimTransitionResult.Create(
            CreateEntry(),
            (GovernanceOutboxClaimTransitionOutcome)int.MaxValue));
    }

    private static GovernanceOutboxEntry CreateEntry()
    {
        var envelope = GovernanceEmissionEnvelope.Create(
            GovernanceEmissionEventType.Outbox,
            "claim-transition-result",
            new DateTimeOffset(2026, 7, 13, 12, 0, 0, TimeSpan.Zero),
            envelopeId: "claim-transition-result-envelope");

        return GovernanceOutboxEntry.Create(
            envelope,
            "claim-transition-result-entry",
            new DateTimeOffset(2026, 7, 13, 12, 0, 1, TimeSpan.Zero));
    }
}
