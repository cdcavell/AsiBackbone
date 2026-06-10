# CDCavell.AsiBackbone.EntityFrameworkCore

Entity Framework Core integration for ASI Backbone accountability persistence.

This package contributes provider-neutral EF Core model configuration, persistence entities, and an EF Core-backed audit ledger store while preserving host ownership of the application database.

> [!IMPORTANT]
> This package does not provide or require a package-owned `DbContext`, database provider, connection string, migration set, or schema deployment workflow. The host application owns those concerns.

## What this package provides

- `ApplyAsiBackboneConfigurations(this ModelBuilder modelBuilder)` for applying ASI Backbone persistence mappings from a host-owned `DbContext`.
- Persistence entities for audit ledger records, audit reason codes, audit metadata, handshake requests, handshake acknowledgments, and related metadata rows.
- `EfCoreAuditLedgerStore`, an append-oriented audit ledger implementation that uses a host-owned `DbContext`.
- Provider-neutral configuration where practical.

## Minimal host-owned DbContext setup

```csharp
using CDCavell.AsiBackbone.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

public sealed class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

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

## Registering the EF Core audit ledger store

A host can register its concrete `DbContext` and expose it as EF Core's base `DbContext` for the audit ledger store.

```csharp
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
});

builder.Services.AddScoped<DbContext>(provider =>
    provider.GetRequiredService<ApplicationDbContext>());

builder.Services.AddScoped<IAsiBackboneAuditLedgerStore, EfCoreAuditLedgerStore>();
```

Provider-specific behavior belongs in the host application. For example, SQLite hosts may need provider-specific `DateTimeOffset` conversions for ordering or range queries.

## Boundary

Use this package when you want durable ASI Backbone accountability records in a host-owned EF Core database.

Do not use this package as an AI model host, ASP.NET Core middleware package, migration owner, signing system, or database provider abstraction.
