using AsiBackbone.Core.Signing;
using AsiBackbone.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AsiBackbone.Signing.LocalDevelopment.Tests.DependencyInjection;

/// <summary>
/// Unit tests for the <see cref="LocalDevelopmentSigningBuilderExtensions" /> class.
/// </summary>
public sealed class LocalDevelopmentSigningBuilderExtensionsTests
{
    /// <summary>
    /// Verifies that default local-development signing registration adds signing and verification services.
    /// </summary>
    [Fact]
    public void UseLocalDevelopmentSigningRegistersDefaultsAndReturnsSameBuilder()
    {
        ServiceCollection services = new();
        IAsiBackboneBuilder builder = new AsiBackboneBuilder(services);

        IAsiBackboneBuilder result = builder.UseLocalDevelopmentSigning();

        Assert.Same(builder, result);
        AssertSingletonRegistrations(services);

        using ServiceProvider provider = services.BuildServiceProvider();
        LocalDevelopmentSigningOptions options = provider.GetRequiredService<LocalDevelopmentSigningOptions>();
        LocalDevelopmentSigningService concrete = provider.GetRequiredService<LocalDevelopmentSigningService>();
        IAsiBackboneSigningService signing = provider.GetRequiredService<IAsiBackboneSigningService>();
        IAsiBackboneSignatureVerificationService verification =
            provider.GetRequiredService<IAsiBackboneSignatureVerificationService>();

        Assert.Equal(LocalDevelopmentSigningOptions.DefaultProviderName, options.ProviderName);
        Assert.Equal(LocalDevelopmentSigningOptions.DefaultKeyId, options.KeyId);
        Assert.Same(concrete, signing);
        Assert.Same(concrete, verification);
    }

    /// <summary>
    /// Verifies that configured local-development options flow into dependency injection.
    /// </summary>
    [Fact]
    public void UseLocalDevelopmentSigningAppliesOptionsAndReturnsSameBuilder()
    {
        ServiceCollection services = new();
        IAsiBackboneBuilder builder = new AsiBackboneBuilder(services);
        LocalDevelopmentSigningOptions configured