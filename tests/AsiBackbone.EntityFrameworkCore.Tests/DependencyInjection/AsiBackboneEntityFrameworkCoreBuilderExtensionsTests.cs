using AsiBackbone.Core.Audit;
using AsiBackbone.Core.Outbox;
using AsiBackbone.DependencyInjection;
using AsiBackbone.EntityFrameworkCore.Audit;
using AsiBackbone.EntityFrameworkCore.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AsiBackbone.EntityFrameworkCore.Tests.DependencyInjection;

/// <summary>
/// Unit tests for the <see cref="AsiBackboneEntityFrameworkCoreBuilderExtensions" /> class.
/// </summary>
public sealed class AsiBackboneEntityFrameworkCoreBuilderExtensionsTests
{
    /// <summary>
    /// Verifies that audit-ledger registration uses the host context, registers the EF Core store, and returns the same builder.
    /// </summary>
    [Fact]
    public void UseEfCoreAuditLedgerRegistersServicesAndReturnsSameBuilder()
    {
        ServiceCollection services = CreateServices();
        IAsiBackboneBuilder builder = new AsiBackboneBuilder(services);

        IAsiBackboneBuilder result = builder.UseEfCoreAuditLedger<TestDbContext>();

        Assert.Same(builder, result);
        AssertScopedRegistration<IAsiBackboneAuditLedgerStore, EfCoreAuditLedgerStore>(services);
        AssertHostDbContextResolution(services);
    }

    /// <summary>
    /// Verifies that audit-ledger registration rejects a null builder.
    /// </summary>
    [Fact]
    public void UseEfCoreAuditLedgerRejectsNullBuilder()
    {
        IAsiBackboneBuilder? builder = null;

        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(
            () => builder!.UseEfCoreAuditLedger<TestDbContext>());

        Assert.Equal("builder", exception.ParamName);
    }

    /// <summary>
    /// Verifies that audit-lifecycle registration uses the host context, registers the EF Core store, and returns the same builder.
    /// </summary>
    [Fact]
    public void UseEfCoreAuditLifecycleRegistersServicesAndReturnsSameBuilder()
    {
        ServiceCollection services = CreateServices();
        IAsiBackboneBuilder builder = new AsiBackboneBuilder(services);

        IAsiBackboneBuilder result = builder.UseEfCoreAuditLifecycle<TestDbContext>();

        Assert.Same(builder, result);
        AssertScopedRegistration<IAsiBackboneAuditResidueLifecycleStore, EfCoreAuditResidueLifecycleStore>(services);
        AssertHostDbContextResolution(services);
    }

    /// <summary>
    /// Verifies that audit-lifecycle registration rejects a null builder.
    /// </summary>
    [Fact]
    public void UseEfCoreAuditLifecycleRejectsNullBuilder()
    {
        IAsiBackboneBuilder? builder = null;

        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(
            () => builder!.UseEfCoreAuditLifecycle<TestDbContext>());

        Assert.Equal("builder", exception.ParamName);
    }

    /// <summary>
    /// Verifies that governance-outbox registration uses the host context, registers the EF Core store, and returns the same builder.
    /// </summary>
    [Fact]
    public void UseEfCoreGovernanceOutboxRegistersServicesAndReturnsSameBuilder()
    {
        ServiceCollection services = CreateServices();
        IAsiBackboneBuilder builder = new AsiBackboneBuilder(services);

        IAsiBackboneBuilder result = builder.UseEfCoreGovernanceOutbox<TestDbContext>();

        Assert.Same(builder, result);
        AssertScopedRegistration<IAsiBackboneGovernanceOutboxStore, EfCoreGovernanceOutboxStore>(services);
        AssertHostDbContextResolution(services);
    }

    /// <summary>
    /// Verifies that governance-outbox registration rejects a null builder.
    /// </summary>
    [Fact]
    public void UseEfCoreGovernanceOutboxRejectsNullBuilder()
    {
        IAsiBackboneBuilder? builder = null;

        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(
            () => builder!.UseEfCoreGovernanceOutbox<TestDbContext>());

        Assert.Equal("builder", exception.ParamName);
    }

    private static ServiceCollection CreateServices()
    {
        ServiceCollection services = new();
        _ = services.AddDbContext<TestDbContext>(options =>
            options.UseInMemoryDatabase(Guid.NewGuid().ToString("N")));

        return services;
    }

    private static void AssertScopedRegistration<TService, TImplementation>(IServiceCollection services)
    {
        Assert.Contains(
            services,
            descriptor => descriptor.ServiceType == typeof(TService)
                && descriptor.ImplementationType == typeof(TImplementation)
                && descriptor.Lifetime == ServiceLifetime.Scoped);
    }

    private static void AssertHostDbContextResolution(IServiceCollection services)
    {
        Assert.Contains(
            services,
            descriptor => descriptor.ServiceType == typeof(DbContext)
                && descriptor.Lifetime == ServiceLifetime.Scoped
                && descriptor.ImplementationFactory is not null);

        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();

        TestDbContext hostContext = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        DbContext registeredContext = scope.ServiceProvider.GetRequiredService<DbContext>();

        Assert.Same(hostContext, registeredContext);
    }

    private sealed class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options);
}
