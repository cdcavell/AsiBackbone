# CDCavell.AsiBackbone
[![CI](https://github.com/cdcavell/AsiBackbone/actions/workflows/ci.yml/badge.svg)](https://github.com/cdcavell/AsiBackbone/actions/workflows/ci.yml)
[![Coverage Report](https://img.shields.io/badge/coverage%20gate-75%25-brightgreen)](https://cdcavell.github.io/AsiBackbone/coverage/index.html)
[![Documentation](https://github.com/cdcavell/AsiBackbone/actions/workflows/publish-docs.yml/badge.svg)](https://github.com/cdcavell/AsiBackbone/actions/workflows/publish-docs.yml)
[![Docs](https://img.shields.io/badge/docs-GitHub%20Pages-blue)](https://cdcavell.github.io/AsiBackbone/)
[![.NET](https://img.shields.io/badge/.NET-10.0-purple)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/license-MIT-blue)](LICENSE.txt)
[![GitHub Release](https://img.shields.io/github/v/release/cdcavell/AsiBackbone?include_prerelease=true&sort=semver&display_name=tag&label=release)](https://github.com/cdcavell/AsiBackbone/releases)
[![Zenodo DOI](https://img.shields.io/badge/DOI-10.5281%2Fzenodo.20546032-blue)](https://doi.org/10.5281/zenodo.20546032)

### NuGet Packages:
[![NuGet Core](https://img.shields.io/nuget/v/CDCavell.AsiBackbone.Core?label=Core)](https://www.nuget.org/packages/CDCavell.AsiBackbone.Core)
[![NuGet Core Downloads](https://img.shields.io/nuget/dt/CDCavell.AsiBackbone.Core?label=Core%20downloads)](https://www.nuget.org/packages/CDCavell.AsiBackbone.Core)

[![NuGet AspNetCore](https://img.shields.io/nuget/v/CDCavell.AsiBackbone.AspNetCore?label=AspNetCore)](https://www.nuget.org/packages/CDCavell.AsiBackbone.AspNetCore)
[![NuGet AspNetCore Downloads](https://img.shields.io/nuget/dt/CDCavell.AsiBackbone.AspNetCore?label=AspNetCore%20downloads)](https://www.nuget.org/packages/CDCavell.AsiBackbone.AspNetCore)

[![NuGet Storage.InMemory](https://img.shields.io/nuget/v/CDCavell.AsiBackbone.Storage.InMemory?label=Storage.InMemory)](https://www.nuget.org/packages/CDCavell.AsiBackbone.Storage.InMemory)
[![NuGet Storage.InMemory Downloads](https://img.shields.io/nuget/dt/CDCavell.AsiBackbone.Storage.InMemory?label=Storage.InMemory%20downloads)](https://www.nuget.org/packages/CDCavell.AsiBackbone.Storage.InMemory)

[![NuGet EntityFrameworkCore](https://img.shields.io/nuget/v/CDCavell.AsiBackbone.EntityFrameworkCore?label=EntityFrameworkCore)](https://www.nuget.org/packages/CDCavell.AsiBackbone.EntityFrameworkCore)
[![NuGet EntityFrameworkCore Downloads](https://img.shields.io/nuget/dt/CDCavell.AsiBackbone.EntityFrameworkCore?label=EntityFrameworkCore%20downloads)](https://www.nuget.org/packages/CDCavell.AsiBackbone.EntityFrameworkCore)

Governance-oriented domain abstractions and host integration packages for the ASI Backbone framework.

`CDCavell.AsiBackbone` is a package family for building a dependency-light governance spine around consequential software actions. The current package set includes Core primitives, in-memory storage for local validation, and Entity Framework Core integration for host-owned persistence.

AsiBackbone should be understood as a **governance spine**, not an intelligence engine. It is infrastructure around intelligent or decision-producing systems: policy evaluation, constraints, acknowledgment workflows, audit receipts, capability-gated execution, and accountable host or gateway execution.

> [!IMPORTANT]
> These packages do **not** implement artificial superintelligence, host AI models, train AI models, or prove the ASI Backbone concept. They provide framework-neutral building blocks and host integration seams for governing consequential actions in software systems.

The Core package does **not** require ASP.NET Core, Entity Framework Core, NetCoreApplicationTemplate, robotics packages, or AI model dependencies. Integration packages may add optional dependencies for their specific host boundary.

## Current Status

Early alpha package family.

The repository has completed the initial Core foundation work and now includes optional persistence packages for in-memory validation, EF Core host-owned persistence and ASP.NET Core integration. Planned follow-up milestones include samples and host validation, signing support, gateway integrations, robotics examples, and stable release packaging.

## Project Direction

AsiBackbone is planned as a host-integrated .NET module ecosystem.

The Core package should answer the question:

> What are the shared AsiBackbone abstractions and domain primitives?

Integration packages answer narrower host-boundary questions, such as:

> How can AsiBackbone accountability records be persisted in a host-owned database?
> How can AsiBackbone be exposed through web middleware or endpoints?
> How can a sample host wire policy evaluation, acknowledgment, audit, and capability boundaries together?

The package family should not answer:

> Which host application template must be used?
> Which AI model, if any, produced the request being governed?
> How are physical or robotic systems controlled?
> Which database provider or migration process must the host use?

Those concerns belong to consuming applications, future provider-specific packages, or later gateway samples.

## Core Domain Language

The initial Core language is documented in:

* [Core Domain Language and Alpha Boundary](docs/articles/core-domain-language.md)
* [Equations and Toy Models](docs/articles/equations-and-toy-models.md)
* [Alpha Package Boundary](docs/articles/alpha-package-boundary.md)
* [EF Core Integration Boundary](docs/articles/ef-core-integration-boundary.md)
* [EF Core Host Ownership and Migration Guidance](docs/articles/ef-core-host-ownership-and-migrations.md)

The central technical lane is:

```text
Intent or request
  -> Policy context
  -> Constraint evaluation
  -> Decision result
  -> Optional acknowledgment
  -> Audit residue
  -> Optional capability token
  -> Host or gateway execution
```

Important terms:

| Term | Meaning |
| --- | --- |
| Governance spine | The software path that every consequential action follows before execution. |
| Intent or request | The proposed action that needs evaluation. |
| Constraint | A rule, policy, condition, or boundary that narrows available outcomes. |
| Active policy structure | The set of constraints in force for a decision at a specific moment. |
| Collapse boundary | The practical software point where open possibility becomes a concrete decision or action. |
| Actor context | The framework-neutral description of who or what is requesting the action. |
| Policy context | The evaluation input assembled from actor, intent, resource, risk, policy, and correlation data. |
| Decision result | The governance output, such as allow, warn, deny, defer, require acknowledgment, or escalate. |
| Operation result | The package execution result, separate from the governance decision outcome. |
| Audit residue | The trace left by decision flow: reason codes, policy version, hash, correlation ID, timestamp, and related metadata. |
| Acknowledgment workflow | A responsibility or reflexive acknowledgment step before consequential action. |
| Capability token | A short-lived, scoped, traceable permission grant. |
| Gateway boundary | The validation boundary before a decision reaches an external or consequential system. |

## `0.1.0-alpha.1` Core Boundary

The original `0.1.0-alpha.1` boundary is language and primitives only.

`CDCavell.AsiBackbone.Core` may include:

- Framework-neutral domain abstractions
- Actor context primitives
- Policy context primitives
- Constraint evaluation contracts
- Decision result primitives
- Operation result primitives
- Reason code primitives
- Acknowledgment or responsibility-handshake abstractions
- Audit residue abstractions
- Capability-token abstractions
- Policy version and policy hash fields
- Correlation ID support
- Shared value objects
- XML documentation for public types

`CDCavell.AsiBackbone.Core` should avoid:

- Entity Framework Core dependencies
- ASP.NET Core dependencies
- Web middleware
- Endpoint mapping
- Host application startup logic
- Database provider assumptions
- Direct dependencies on NetCoreApplicationTemplate
- Concrete ledger or database persistence
- Concrete signing or key-management implementation
- Robotics control implementation
- AI model hosting, training, inference, or orchestration

## `0.2.0-alpha.1` Persistence Boundary

The `0.2.0-alpha.1` persistence milestone adds optional storage packages without changing Core's host-neutral boundary.

The persistence package line includes:

- `CDCavell.AsiBackbone.Storage.InMemory` for local validation, tests, and samples.
- `CDCavell.AsiBackbone.EntityFrameworkCore` for EF Core model configuration and audit ledger persistence through a host-owned `DbContext`.

The EF Core package supports host-owned persistence. AsiBackbone contributes model configuration and storage helpers, while the consuming application owns the `DbContext`, database provider, connection string, migrations, deployment, and schema lifecycle.

The preferred persistence model is host-owned data access:

> AsiBackbone owns its domain model.<br />
> The host application owns the DbContext.<br />
> AsiBackbone provides model configuration hooks.<br />

See [EF Core Integration Boundary](docs/articles/ef-core-integration-boundary.md) and [EF Core Host Ownership and Migration Guidance](docs/articles/ef-core-host-ownership-and-migrations.md) for details.

## Package Boundary

`CDCavell.AsiBackbone.Core` is responsible for:

- Core domain abstractions
- Policy and constraint contracts
- Decision/result primitives
- Acknowledgment and audit abstractions
- Capability-token abstractions
- Shared options and value objects when needed
- Assembly markers or discovery-friendly primitives
- Framework-neutral domain language

`CDCavell.AsiBackbone.Core` should avoid:

- Entity Framework Core dependencies
- ASP.NET Core dependencies
- Web middleware
- Endpoint mapping
- Host application startup logic
- Database provider assumptions
- Direct dependencies on NetCoreApplicationTemplate
- AI model dependencies
- Robotics or physical execution dependencies

## Package Family

The current and planned package family is:

```text
CDCavell.AsiBackbone.Core
CDCavell.AsiBackbone.Storage.InMemory
CDCavell.AsiBackbone.EntityFrameworkCore
CDCavell.AsiBackbone.AspNetCore
CDCavell.AsiBackbone.Signing
CDCavell.AsiBackbone.Samples
CDCavell.AsiBackbone.Robotics
```

Package names may be adjusted before stable release.

## CDCavell.AsiBackbone.Core

Current package.

Defines framework-neutral domain abstractions and primitives.

## CDCavell.AsiBackbone.Storage.InMemory

Current package.

Provides non-durable in-memory storage helpers for local validation, samples, and tests. It should not be used as durable production storage.

## CDCavell.AsiBackbone.EntityFrameworkCore

Current package.

Provides EF Core model contributions and audit ledger persistence integration. The host application owns the `DbContext`, provider, connection string, migrations, deployment, and schema lifecycle.

## CDCavell.AsiBackbone.AspNetCore

Planned future package for ASP.NET Core integration.

This may eventually provide service registration extensions, middleware, endpoint mapping, policy hooks, current-user/current-actor resolution, request-level policy context building, or Problem Details integration where appropriate.

## CDCavell.AsiBackbone.Signing

Planned future package for signing and verification support around decision receipts, policy hashes, capability tokens, and audit records.

## CDCavell.AsiBackbone.Samples

Planned future samples or validation hosts.

Samples may include a console or worker host, a plain ASP.NET Core host, and a NetCoreApplicationTemplate-based host.

## CDCavell.AsiBackbone.Robotics

Later integration package.

Robotics should remain a later-stage gateway example. The first release path should prove the policy, decision, acknowledgment, audit, capability-token, and gateway patterns before moving toward physical execution scenarios.

## Relationship to NetCoreApplicationTemplate

NetCoreApplicationTemplate may be used as a preferred host baseline during development and validation, but AsiBackbone should not require it.

The intended relationship is:

```text
NetCoreApplicationTemplate
    = preferred host baseline

AsiBackbone
    = optional governance/module package family

Consumer application
    = chooses whether to use either or both
```

A consumer should be able to use AsiBackbone in:

- an application generated from NetCoreApplicationTemplate
- an existing ASP.NET Core application
- a future custom host that provides the required infrastructure

## Alignment Boundary

AsiBackbone documentation may reference the broader ASI Backbone concept and Eden/ASI framework as conceptual inspiration.

Safe language:

- AsiBackbone is inspired by the ASI Backbone framework.
- AsiBackbone implements governance-oriented software primitives.
- AsiBackbone helps structure consequential decision flow through constraints, acknowledgment, audit, and capability boundaries.
- AsiBackbone can surround intelligent or decision-producing systems with accountable execution infrastructure.

Avoid language such as:

- AsiBackbone implements ASI.
- AsiBackbone proves the Eden Hypothesis.
- AsiBackbone is an AI model.
- AsiBackbone replaces AI safety governance, legal review, or organizational accountability.

## Design Principles

- Keep Core small.
- Keep Core dependency-light.
- Avoid hidden host assumptions.
- Prefer explicit integration over magic.
- Let the host own infrastructure.
- Let integration packages own persistence, web integration, signing, storage, samples, and external execution concerns.
- Keep package boundaries clear before adding behavior.
- Treat NetCoreApplicationTemplate as a preferred host, not a parent framework.
- Treat AsiBackbone as governance infrastructure, not an intelligence engine.
