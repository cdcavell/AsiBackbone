using Xunit;

namespace AsiBackbone.EntityFrameworkCore.Tests;

/// <summary>
/// Tests for the assembly reference marker in the Entity Framework Core integration package.
/// </summary>
public sealed class AssemblyReferenceTests
{
    /// <summary>
    /// Verifies that the Entity Framework Core integration package assembly marker can be created.
    /// </summary>
    [Fact]
    public void AssemblyReferenceCanBeCreated()
    {
        AssemblyReference marker = new();

        Assert.NotNull(marker);
        Assert.Same(typeof(AssemblyReference).Assembly, marker.GetType().Assembly);
    }
}
