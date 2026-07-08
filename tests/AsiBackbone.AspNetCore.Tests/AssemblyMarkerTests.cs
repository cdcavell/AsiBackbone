using Xunit;

namespace AsiBackbone.AspNetCore.Tests;

/// <summary>
/// This class contains unit tests for the assembly marker type in the AsiBackbone.AspNetCore assembly.
/// </summary>
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
