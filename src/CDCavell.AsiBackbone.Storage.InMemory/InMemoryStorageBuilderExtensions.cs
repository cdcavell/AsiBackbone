using CDCavell.AsiBackbone.Core.Audit;
using CDCavell.AsiBackbone.Core.Outbox;
using CDCavell.AsiBackbone.DependencyInjection;
using CDCavell.AsiBackbone.Storage.InMemory.Audit;
using CDCavell.AsiBackbone.Storage.InMemory.Outbox;
using Microsoft.Extensions.DependencyInjection;

namespace CDCavell.AsiBackbone.Storage.InMemory;

/// <summary>
/// Provides explicit builder facade extension methods for non-durable in-memory storage.
/// </summary>
public static class InMemoryStorageBuilderExtensions
{
    /// <summary>
    /// Adds the non-durable in-memory audit sink through the AsiBackbone builder facade.
    /// </summary>
    public static IAsiBackboneBuilder UseInMemoryAuditLedger(this IAsiBackboneBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        _ = builder.Services.AddSingleton<InMemoryAuditLedger>();
        _ = builder.Services.AddSingleton<IAsiBackboneAuditSink>(serviceProvider =>
            serviceProvider.GetRequiredService<InMemoryAuditLedger>());

        return builder;
    }

    /// <summary>
    /// Adds the non-durable in-memory audit residue lifecycle store through the AsiBackbone builder facade.
    /// </summary>
    public static IAsiBackboneBuilder UseInMemoryAuditLifecycle(this IAsiBackboneBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        _ = builder.Services.AddSingleton<IAsiBackboneAuditResidueLifecycleStore, InMemoryAuditResidueLifecycleStore>();
        return builder;
    }

    /// <summary>
    /// Adds the non-durable in-memory governance outbox store through the AsiBackbone builder facade.
    /// </summary>
    public static IAsiBackboneBuilder UseInMemoryGovernanceOutbox(this IAsiBackboneBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        _ = builder.Services.AddSingleton<IAsiBackboneGovernanceOutboxStore, InMemoryGovernanceOutboxStore>();
        return builder;
    }
}
