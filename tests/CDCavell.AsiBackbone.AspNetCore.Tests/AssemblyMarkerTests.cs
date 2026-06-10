using Xunit;

namespace CDCavell.AsiBackbone.AspNetCore.Tests;

public sealed class AssemblyMarkerTests
{
    /// <summary>
    /// Verifies that the ASP.NET Core scaffold exposes a public assembly marker type.
    /// </summary>
    [Fact]
    public void AssemblyMarkerTypeIsAvailable()
    {
        Assert.Equal("AssemblyMarker", nameof(AssemblyMarker));
    }
}
