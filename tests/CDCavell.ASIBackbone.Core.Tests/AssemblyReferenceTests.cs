using System.Reflection;
using Xunit;

namespace CDCavell.AsiBackbone.Core.Tests;

public sealed class AssemblyReferenceTests
{
    /// <summary>
    /// Verifies that the <see cref="AssemblyReference.Assembly"/> property returns the expected assembly, which is the assembly containing the <see cref="AssemblyReference"/> class itself. This ensures that the reference to the assembly is correct and can be used reliably throughout the application.
    /// </summary>
    [Fact]
    public void Assembly_ReturnsCoreAssembly()
    {
        Assembly assembly = AssemblyReference.Assembly;

        Assert.Same(typeof(AssemblyReference).Assembly, assembly);
        Assert.Equal("CDCavell.AsiBackbone.Core", assembly.GetName().Name);
    }
}
