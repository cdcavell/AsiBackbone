using AsiBackbone.Core.Actors;

namespace AsiBackbone.AspNetCore.Actors;

/// <summary>
/// Resolves the current ASP.NET Core request actor into the framework-neutral AsiBackbone actor context model.
/// </summary>
public interface IAsiBackboneHttpActorContextResolver
{
    /// <summary>
    /// Resolves the current HTTP or host actor context.
    /// </summary>
    /// <returns>A framework-neutral actor context.</returns>
    IAsiBackboneActorContext ResolveActorContext();
}
