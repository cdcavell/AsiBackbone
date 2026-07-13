using AsiBackbone.Core.Audit;
using AsiBackbone.Core.Outbox;
using AsiBackbone.DependencyInjection;
using AsiBackbone.EntityFrameworkCore.Audit;
using AsiBackbone.EntityFrameworkCore.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AsiBackbone.EntityFrameworkCore;

/// <summary>
/// Provides explicit builder facade extension methods for EF Core host-owned persistence.
/// </summary>
public static class AsiBackboneEntityFrameworkCoreBuilderExtensions
{
    /// <summary>
    /// Adds EF Core audit ledger storage through the AsiBackbone builder facade.
    /// </summary>
    public static IAsiBackboneBuilder UseEfCoreAuditLedger<TDbContext>(this IAsiBackboneBuilder builder)
        where TDbContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(builder);

        _ = builder.Services.AddScoped<DbContext>(serviceProvider =>
            serviceProvider.GetRequiredService<TDbContext>());
        _ = builder.Services.AddScoped<IAsiBackboneAuditLedgerStore, EfCoreAuditLedgerStore>();

        return builder;
    }

    /// <summary>
    /// Adds EF Core audit residue lifecycle storage through the AsiBackbone builder facade.
    /// </summary>
    public static IAsiBackboneBuilder UseEfCoreAuditLifecycle<TDbContext>(this IAsiBackboneBuilder builder)
        where TDbContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(builder);

        _ = builder.Services.AddScoped<DbContext>(serviceProvider =>
            serviceProvider.GetRequiredService<TDbContext>());
        _ = builder.Services.AddScoped<IAsiBackboneAuditResidueLifecycleStore, EfCoreAuditResidueLifecycleStore>();

        return builder;
    }

    /// <summary>
    /// Adds outcome-aware EF Core durable governance outbox storage through the AsiBackbone builder facade.
    /// </summary>
    public static IAsiBackboneBuilder UseEfCoreGovernanceOutbox<TDbContext>(this IAsiBackboneBuilder builder)
        where TDbContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(builder);

        _ = builder.Services.AddScoped<DbContext>(serviceProvider =>
            serviceProvider.GetRequiredService<TDbContext>());
        _ = builder.Services.AddScoped<EfCoreGovernanceOutboxOutcomeStore>();
        _ = builder.Services.AddScoped<IAsiBackboneGovernanceOutboxClaimOutcomeStore>(serviceProvider =>
            serviceProvider.GetRequiredService<EfCoreGovernanceOutboxOutcomeStore>());
        _ = builder.Services.AddScoped<IAsiBackboneGovernanceOutboxClaimStore>(serviceProvider =>
            serviceProvider.GetRequiredService<EfCoreGovernanceOutboxOutcomeStore>());
        _ = builder.Services.AddScoped<IAsiBackboneGovernanceOutboxStore>(serviceProvider =>
            serviceProvider.GetRequiredService<EfCoreGovernanceOutboxOutcomeStore>());

        return builder;
    }
}