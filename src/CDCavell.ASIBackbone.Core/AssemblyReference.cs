using System.Reflection;

namespace CDCavell.ASIBackbone.Core;

/// <summary>
/// Provides a stable marker for locating the ASI Backbone Core assembly.
/// </summary>
public static class AssemblyReference
{
    /// <summary>
    /// Gets the ASI Backbone Core assembly.
    /// </summary>
    public static Assembly Assembly => typeof(AssemblyReference).Assembly;
}