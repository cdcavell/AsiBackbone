using AsiBackbone.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AsiBackbone.Signing.LocalDevelopment.Tests.DependencyInjection;

/// <summary>
/// Covers validation performed by local-development signing registration.
/// </summary>
public sealed class LocalDevelopmentSigningValidationTests
{
    /// <summary>
    /// Verifies invalid key sizes fail during explicit provider registration.
    /// </summary>
    [Fact]
    public void UseLocalDevelopmentSigningRejectsInvalidKeySize()
    {
        ServiceCollection services = new();
        IAsiBackboneBuilder builder = new AsiBackboneBuilder(services);
        var options = LocalDevelopmentSigningOptions.Create(keySizeBits: 1024);

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => builder.UseLocalDevelopmentSigning(options));

        Assert.Contains("at least 2048 bits", exception.Message, StringComparison.Ordinal);
        Assert.Empty(services);
    }
}
