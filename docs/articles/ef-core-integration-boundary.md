# EF Core Integration Package Boundary

This article defines the intended boundary for the future `CDCavell.AsiBackbone.Storage.EntityFrameworkCore` package.

The EF Core integration package should provide persistence configuration and storage helpers for AsiBackbone accountability records while preserving host ownership of the application database.

> [!IMPORTANT]
> AsiBackbone should not own the host application's `DbContext`, database provider, connection string, migrations, deployment process, or schema lifecycle. The host application remains responsible for those concerns.

## Boundary statement

The EF Core package should answer this question:

> How can a host-owned EF Core application persist AsiBackbone accountability records without surrendering database ownership to the package?

It should not answer:

> Which database provider must the host use?
> Which `DbContext` must the host inherit from?
> Which migration strategy must the host adopt?
> Which application template must the host use?

The package should contribute EF Core model configuration, persistence-facing entities or records, and storage contracts/implementations where appropriate. The host application should remain the composition root.

## Package responsibility

`CDCavell.AsiBackbone.Storage.EntityFrameworkCore` may provide:

* EF Core entity type configurations for AsiBackbone persistence models
* `ModelBuilder` extension methods for applying AsiBackbone configurations
* EF Core-backed implementations of Core storage contracts
* persistence models for audit records, decision receipts, handshake records, reason codes, metadata, and policy trace fields
* provider-neutral EF Core configuration where practical
* integration tests proving the package works inside a host-owned `DbContext`

The package should avoid:

* a required package-owned `AsiBackboneDbContext`
* provider-specific assumptions in the core configuration layer
* forced migrations
* forced connection-string ownership
* hidden dependency on NetCoreApplicationTemplate
* ASP.NET Core middleware or endpoint behavior
* AI model hosting, inference, training, or orchestration
* robotics or physical execution concerns

## Host-owned `DbContext` strategy

The host application owns the `DbContext`.

A consuming application should be able to integrate AsiBackbone persistence using a pattern similar to:

```csharp
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

This keeps the database boundary clear:

```text
Host application
  owns DbContext
  owns provider
  owns connection string
  owns migrations
  owns deployment

AsiBackbone EF Core package
  contributes model configuration
  contributes persistence contracts/implementations
  does not own the database
```

## ModelBuilder extension location

The preferred extension method should live in the EF Core integration package, likely under a namespace such as:

```csharp
namespace CDCavell.AsiBackbone.Storage.EntityFrameworkCore;
```

A future extension may look like:

```csharp
public static class AsiBackboneModelBuilderExtensions
{
    public static ModelBuilder ApplyAsiBackboneConfigurations(this ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AsiBackboneModelBuilderExtensions).Assembly);

        return modelBuilder;
    }
}
```

The exact implementation may change, but the public contract should remain simple: the host calls one extension method from `OnModelCreating`.

## Migration ownership

Migrations belong to the host application.

The EF Core integration package should not ship required migrations for the host database. This avoids forcing the package to choose a database provider, schema naming convention, migration assembly, deployment model, or operational process.

The host application should decide:

* database provider
* schema name
* table naming conventions where configurable
* migration assembly
* migration cadence
* deployment workflow
* data retention and archival rules
* encryption and key-management policy
* backup and restore strategy

AsiBackbone can document recommended schema shapes, but the host should own the actual migration files.

## Provider-neutral design

The initial EF Core integration should favor provider-neutral configuration.

Provider-specific features should be avoided unless there is a clear extension point. For example, the first milestone should avoid relying on SQL Server-specific, PostgreSQL-specific, or SQLite-specific behavior in the package core.

Provider-specific examples may appear later in samples or documentation, but the package boundary should remain host-neutral.

## Relationship to Core

`CDCavell.AsiBackbone.Core` should remain free of EF Core references.

Dependency direction should be:

```text
CDCavell.AsiBackbone.Core
        ▲
        │
CDCavell.AsiBackbone.Storage.EntityFrameworkCore
```

Core defines the domain contracts and governance primitives. The EF Core package adapts those contracts into persistence behavior.

The Core package should not know that EF Core exists.

## Relationship to NetCoreApplicationTemplate

NetCoreApplicationTemplate may be a preferred validation host, but it must not be required.

The EF Core package should support:

* plain ASP.NET Core applications
* worker services
* console validation hosts
* NetCoreApplicationTemplate-based applications
* other host-owned EF Core applications

The integration should be documented in a way that works without assuming NetCoreApplicationTemplate conventions.

## Initial persistence focus

The first EF Core milestone should focus on accountability records, not broad application persistence.

Candidate persistence areas include:

* audit residue or audit receipts
* governance decision receipts
* liability/responsibility handshake requests
* liability/responsibility handshake acknowledgments
* reason codes
* actor identifiers and actor type
* policy version and policy hash
* correlation ID and trace ID
* metadata snapshots

This aligns persistence with the AsiBackbone governance spine: decisions, acknowledgments, and audit trails should be durable and queryable.

## Non-goals for the first EF Core milestone

The first EF Core integration should not include:

* a package-owned application `DbContext`
* package-owned migrations
* provider-specific schema assumptions
* ASP.NET Core middleware
* endpoint mapping
* authentication or claims translation
* signing or cryptographic verification
* robotics command persistence
* AI model output storage
* event sourcing infrastructure beyond simple audit/accountability persistence
* distributed ledger implementation

Those may be considered later if justified by follow-up issues.

## Acceptance guidance for future implementation issues

Follow-up implementation issues should preserve these rules:

1. Host applications own `DbContext` and migrations.
2. AsiBackbone contributes model configuration through extension methods.
3. EF Core integration depends on Core, but Core does not depend on EF Core.
4. The package remains provider-neutral unless a provider-specific extension is explicitly introduced.
5. NetCoreApplicationTemplate is supported as a validation host, not required as a dependency.
6. Persistence focuses first on accountable governance records.
7. Durable storage should preserve normalized, immutable snapshots of decisions, audit residue, acknowledgments, reason codes, and metadata.
