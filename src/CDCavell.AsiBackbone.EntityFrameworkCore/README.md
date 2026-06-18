# CDCavell.AsiBackbone.EntityFrameworkCore

Entity Framework Core model configuration and host-owned persistence helpers for Accountable Systems Infrastructure records.

This package contributes provider-neutral EF Core model configuration, persistence entities, EF Core-backed audit ledger storage, EF Core-backed audit residue lifecycle storage, and EF Core-backed durable governance outbox storage while preserving host ownership of the application database.

> [!IMPORTANT]
> This package does not provide or require a package-owned `DbContext`, database provider, connection string, migration set, schema deployment workflow, tamper-evident storage, signing provider, downstream emission provider, or compliance guarantee. The host application owns those concerns.

## What this package provides

- `ApplyAsiBackboneConfigurations(this ModelBuilder modelBuilder)` for applying ASI Backbone persistence mappings from a host-owned `DbContext`.
- Persistence entities for audit ledger records, audit reason codes, audit metadata, handshake requests, handshake acknowledgments, governance outbox entries, audit residue lifecycle events, and related metadata rows.
- `EfCoreAuditLedgerStore`, an append-oriented audit ledger implementation that uses a host-owned `DbContext`.
- `EfCoreAuditResidueLifecycleStore`, an append-oriented lifecycle event implementation for durable progress records.
- `EfCoreGovernanceOutboxStore`, a durable local outbox implementation for provider-neutral governance emission envelopes.
- Provider-neutral configuration where practical.

## Minimal host-owned DbContext setup

```csharp
using CDCavell.AsiBackbone.EntityFrameworkCore;
using CDCavell.AsiBackbone.EntityFrameworkCore.Persistence;
using Microsoft.EntityFrameworkCore;

public sealed class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<AsiBackboneGovernanceOutboxEntryEntity> GovernanceOutboxEntries =>
        Set<AsiBackboneGovernanceOutboxEntryEntity>();

    public DbSet<AsiBackboneAuditResidueLifecycleEventEntity> AuditResidueLifecycleEvents =>
        Set<AsiBackboneAuditResidueLifecycleEventEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyAsiBackboneConfigurations();
    }
}
```

The host application may expose `DbSet<T>` properties for convenience, but the package does not require a specific `DbContext` base type beyond EF Core's `DbContext`.

## Migration ownership

Migrations belong to the host application. After applying the ASI Backbone model configurations, create migrations in the same way the host creates the rest of its application schema.

```bash
dotnet ef migrations add AddAsiBackbonePersistence \
  --project src/MyApplication.Infrastructure \
  --startup-project src/MyApplication.Web

dotnet ef database update \
  --project src/MyApplication.Infrastructure \
  --startup-project src/MyApplication.Web
```

The project paths are examples only. The host application decides the provider, migration assembly, schema names, deployment workflow, retention policy, backup strategy, and environment-specific configuration.

## Durable governance outbox storage

`EfCoreGovernanceOutboxStore` persists provider-neutral `GovernanceEmissionEnvelope` records before optional downstream provider delivery is attempted. It stores the envelope, status, retry count, next retry UTC, delivered timestamp, provider name, provider record ID, last provider-neutral error fields, dead-letter reason, and safe metadata.

Use the outbox store when a host needs local durability across process restarts before a later OpenTelemetry, SIEM, Event Hubs, Azure Monitor, Purview, custom, or internal provider drains and delivers pending entries.

The EF Core adapter is durable storage only. It does not drain the outbox by itself and does not add a cloud-provider SDK dependency to Core.

`EfCoreGovernanceOutboxStore` participates in EF Core optimistic concurrency through the configured `ConcurrencyStamp` token. That protects state updates from stale writes, but it is not an atomic claim-before-emit mechanism. Multiple workers can still read the same pending row before either worker saves the final delivered/failed state.

For scaled-out deployments, run one active worker per durable outbox partition unless the host adds provider-specific claim/lease behavior or downstream idempotency. See the DocFX article `Outbox Multi-Worker Concurrency` for the detailed deployment guidance.

## Registering EF Core stores

A host can register its concrete `DbContext` and expose it as EF Core's base `DbContext` for the stores.

```csharp
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
});

builder.Services.AddScoped<DbContext>(provider =>
    provider.GetRequiredService<ApplicationDbContext>());

builder.Services.AddScoped<IAsiBackboneAuditLedgerStore, EfCoreAuditLedgerStore>();
builder.Services.AddScoped<IAsiBackboneAuditResidueLifecycleStore, EfCoreAuditResidueLifecycleStore>();
builder.Services.AddScoped<IAsiBackboneGovernanceOutboxStore, EfCoreGovernanceOutboxStore>();
```

Provider-specific behavior belongs in the host application. For example, SQLite hosts may need provider-specific `DateTimeOffset` conversions for ordering or range queries. Multi-worker claiming patterns such as skip-locked row selection, update locks, read-past hints, or claim-lease columns also belong in the host/provider layer unless a future provider-specific package explicitly implements them.

## Privacy boundary

Durable rows should contain minimized governance metadata, not raw prompts, protected content, raw secrets, raw tokens, or provider SDK payloads. Prefer stable identifiers, content hashes, schema versions, policy versions, policy hashes, trace IDs, correlation IDs, lifecycle stages, and safe diagnostic metadata.

The host owns access control, encryption, retention, archival, deletion, and legal/compliance policy for the database.

## Boundary

Use this package when you want ASI Backbone accountability records persisted through a host-owned EF Core database.

Do not use this package as an AI model host, ASP.NET Core middleware package, migration owner, signing system, tamper-evident ledger provider, compliance system, downstream emission provider, database provider abstraction, or distributed lock manager.
