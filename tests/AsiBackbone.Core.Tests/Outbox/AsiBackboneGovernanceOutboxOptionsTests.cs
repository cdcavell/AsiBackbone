using AsiBackbone.Core.Outbox;
using Xunit;

namespace AsiBackbone.Core.Tests.Outbox;

public sealed class AsiBackboneGovernanceOutboxOptionsTests
{
    [Fact]
    public void ValidateAcceptsDefaultOptions()
    {
        var options = new AsiBackboneGovernanceOutboxOptions();

        options.Validate();

        Assert.False(options.UseClaimLeases);
        Assert.Null(options.ClaimWorkerId);
        Assert.True(options.ClaimLeaseDuration > TimeSpan.Zero);
    }

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
