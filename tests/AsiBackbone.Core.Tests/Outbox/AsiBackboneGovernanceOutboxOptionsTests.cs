using AsiBackbone.Core.Outbox;
using Xunit;

namespace AsiBackbone.Core.Tests.Outbox;

/// <summary>
/// Unit tests for the <see cref="AsiBackboneGovernanceOutboxOptions"/> class.
/// </summary>
public sealed class AsiBackboneGovernanceOutboxOptionsTests
{
    /// <summary>
    /// Validates that the default options are accepted without throwing exceptions.
    /// </summary>
    [Fact]
    public void ValidateAcceptsDefaultOptions()
    {
        var options = new AsiBackboneGovernanceOutboxOptions();

        options.Validate();

        Assert.False(options.UseClaimLeases);
        Assert.Null(options.ClaimWorkerId);
        Assert.True(options.ClaimLeaseDuration > TimeSpan.Zero);
    }

    /// <summary>
    /// Validates that the options with claim leases and a valid worker ID are accepted without throwing exceptions.
    /// </summary>
    [Fact]
    public void ValidateAcceptsClaimLeaseOptionsWithWorkerId()
    {
        var options = new AsiBackboneGovernanceOutboxOptions
        {
            UseClaimLeases = true,
            ClaimWorkerId = "worker-1",
            ClaimLeaseDuration = TimeSpan.FromMinutes(2)
        };

        options.Validate();
    }

    /// <summary>
    /// Validates that the options with invalid timing values are rejected.
    /// </summary>
    [Fact]
    public void ValidateRejectsInvalidTimingOptions()
    {
        _ = Assert.Throws<InvalidOperationException>(() => new AsiBackboneGovernanceOutboxOptions
        {
            RetryDelay = TimeSpan.FromTicks(-1)
        }.Validate());

        _ = Assert.Throws<InvalidOperationException>(() => new AsiBackboneGovernanceOutboxOptions
        {
            DeferredDelay = TimeSpan.FromTicks(-1)
        }.Validate());

        _ = Assert.Throws<InvalidOperationException>(() => new AsiBackboneGovernanceOutboxOptions
        {
            ClaimLeaseDuration = TimeSpan.Zero
        }.Validate());
    }

    /// <summary>
    /// Validates that the options require a worker ID when claim leases are enabled.
    /// </summary>
    [Fact]
    public void ValidateRequiresWorkerIdWhenClaimLeasesAreEnabled()
    {
        var options = new AsiBackboneGovernanceOutboxOptions
        {
            UseClaimLeases = true,
            ClaimWorkerId = " "
        };

        _ = Assert.Throws<InvalidOperationException>(options.Validate);
    }
}
