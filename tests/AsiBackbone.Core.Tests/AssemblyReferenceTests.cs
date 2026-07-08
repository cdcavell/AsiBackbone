using System.Reflection;
using Xunit;

namespace AsiBackbone.Core.Tests;

/// <summary>
/// This class contains unit tests for the <see cref="AssemblyReference"/> class, which provides a reference to the assembly containing the core functionality of the AsiBackbone framework. The tests ensure that the assembly reference is correctly retrieved and matches the expected assembly, validating the integrity of the assembly reference mechanism within the framework.
/// </summary>
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
        Assert.Equal("AsiBackbone.Core", assembly.GetName().Name);
    }
}
