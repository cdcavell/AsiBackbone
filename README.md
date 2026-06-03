# CDCavell.ASIBackbone.Core

Core domain abstractions for the ASI Backbone framework.

`CDCavell.ASIBackbone.Core` is the dependency-light foundation package for the ASI Backbone package family. It is intended to define shared contracts, base abstractions, result primitives, and domain language that can be reused by future integration packages.

This package does **not** require ASP.NET Core, Entity Framework Core, or NetCoreApplicationTemplate.

## Current Status

Early alpha foundation package.

The repository is currently establishing the Core package boundary before adding EF Core integration, ASP.NET Core integration, samples, or stable release packaging.

## Project Direction

ASIBackbone is planned as a host-integrated .NET module ecosystem.

The Core package should answer the question:

> What are the shared ASI Backbone abstractions?

It should not answer:

> How is ASIBackbone persisted?
> How is ASIBackbone exposed through web middleware or endpoints?
> Which host application template must be used?

Those concerns belong in future integration packages.

## Package Boundary

`CDCavell.ASIBackbone.Core` is responsible for:

- Core domain abstractions
- Entity and identity contracts
- Concurrency-tracking contracts
- Operation/result primitives
- Shared options and value objects when needed
- Assembly markers or discovery-friendly primitives
- Framework-neutral domain language

`CDCavell.ASIBackbone.Core` should avoid:

- Entity Framework Core dependencies
- ASP.NET Core dependencies
- Web middleware
- Endpoint mapping
- Host application startup logic
- Database provider assumptions
- Direct dependencies on NetCoreApplicationTemplate

## Planned Package Family

The intended package family is:

```text
CDCavell.ASIBackbone.Core
CDCavell.ASIBackbone.EntityFrameworkCore
CDCavell.ASIBackbone.AspNetCore
CDCavell.ASIBackbone.Samples
```

## CDCavell.ASIBackbone.Core

Defines framework-neutral domain abstractions.

## CDCavell.ASIBackbone.EntityFrameworkCore

Planned future package for EF Core model contributions.

The preferred persistence model is host-owned data access:

> ASIBackbone owns its domain model.<br />
> The host application owns the DbContext.<br />
> ASIBackbone provides model configuration hooks.<br />

## CDCavell.ASIBackbone.AspNetCore

Planned future package for ASP.NET Core integration.

This may eventually provide service registration extensions, middleware, endpoint mapping, policy hooks, or current-user/current-actor integration seams.

## CDCavell.ASIBackbone.Samples

Planned future samples or validation hosts.

Samples may include a plain ASP.NET Core host and a NetCoreApplicationTemplate-based host.

## Relationship to NetCoreApplicationTemplate

NetCoreApplicationTemplate may be used as a preferred host baseline during development and validation, but ASIBackbone should not require it.

The intended relationship is:

```text
NetCoreApplicationTemplate
    = preferred host baseline

ASIBackbone
    = optional domain/module package family

Consumer application
    = chooses whether to use either or both
```

A consumer should be able to use ASIBackbone in:

- an application generated from NetCoreApplicationTemplate
- an existing ASP.NET Core application
- a future custom host that provides the required infrastructure

## Design Principles

- Keep Core small.
- Keep Core dependency-light.
- Avoid hidden host assumptions.
- Prefer explicit integration over magic.
- Let the host own infrastructure.
- Let future packages own persistence and web integration.
- Keep package boundaries clear before adding behavior.
- Treat NetCoreApplicationTemplate as a preferred host, not a parent framework.
