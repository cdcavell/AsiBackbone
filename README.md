# CDCavell.ASIBackbone.Core
[![CI](https://github.com/cdcavell/ASIBackbone/actions/workflows/ci.yml/badge.svg)](https://github.com/cdcavell/ASIBackbone/actions/workflows/ci.yml)
[![Coverage Report](https://img.shields.io/badge/coverage%20gate-75%25-brightgreen)](https://cdcavell.github.io/ASIBackbone/coverage/index.html)
[![Documentation](https://github.com/cdcavell/ASIBackbone/actions/workflows/publish-docs.yml/badge.svg)](https://github.com/cdcavell/ASIBackbone/actions/workflows/publish-docs.yml)
[![Docs](https://img.shields.io/badge/docs-GitHub%20Pages-blue)](https://cdcavell.github.io/ASIBackbone/)
[![.NET](https://img.shields.io/badge/.NET-10.0-purple)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/license-MIT-blue)](LICENSE.txt)
[![GitHub Release](https://img.shields.io/github/v/release/cdcavell/ASIBackbone?include_prerelease=true&sort=semver&display_name=tag&label=release)](https://github.com/cdcavell/ASIBackbone/releases)
[![NuGet](https://img.shields.io/nuget/v/CDCavell.ASIBackbone.Core?label=NuGet)](https://www.nuget.org/packages/CDCavell.ASIBackbone.Core)
[![NuGet Downloads](https://img.shields.io/nuget/dt/CDCavell.ASIBackbone.Core?label=downloads)](https://www.nuget.org/packages/CDCavell.ASIBackbone.Core)
[![Zenodo DOI](https://img.shields.io/badge/DOI-10.5281%2Fzenodo.20546032-blue)](https://doi.org/10.5281/zenodo.20546032)

Core domain abstractions for the ASI Backbone framework.

`CDCavell.ASIBackbone.Core` is the dependency-light foundation package for the ASIBackbone package family. It defines shared contracts, base abstractions, result primitives, and domain language for governance-oriented decision flow.

ASIBackbone should be understood as a **governance spine**, not an intelligence engine. It is infrastructure around intelligent or decision-producing systems: policy evaluation, constraints, acknowledgment workflows, audit receipts, capability-gated execution, and accountable host or gateway execution.

> [!IMPORTANT]
> This package does **not** implement artificial superintelligence, host AI models, train AI models, or prove the ASI Backbone concept. It provides framework-neutral building blocks for governing consequential actions in software systems.

This package does **not** require ASP.NET Core, Entity Framework Core, NetCoreApplicationTemplate, robotics packages, or AI model dependencies.

## Current Status

Early alpha foundation package.

The repository is currently establishing the Core package boundary before adding EF Core integration, ASP.NET Core integration, samples, signing support, gateway integrations, robotics examples, or stable release packaging.

## Project Direction

ASIBackbone is planned as a host-integrated .NET module ecosystem.

The Core package should answer the question:

> What are the shared ASIBackbone abstractions and domain primitives?

It should not answer:

> How is ASIBackbone persisted?
> How is ASIBackbone exposed through web middleware or endpoints?
> Which host application template must be used?
> Which AI model, if any, produced the request being governed?
> How are physical or robotic systems controlled?

Those concerns belong in future integration packages or host applications.

## Core Domain Language

The initial Core language is documented in:

* [Core Domain Language and Alpha Boundary](docs/articles/core-domain-language.md)
* [Equations and Toy Models](docs/articles/equations-and-toy-models.md)
* [Alpha Package Boundary](docs/articles/alpha-package-boundary.md)

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

## `0.1.0-alpha.1` Boundary

The intended `0.1.0-alpha.1` boundary is language and primitives only.

`CDCavell.ASIBackbone.Core` may include:

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

`CDCavell.ASIBackbone.Core` should avoid:

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

## Package Boundary

`CDCavell.ASIBackbone.Core` is responsible for:

- Core domain abstractions
- Policy and constraint contracts
- Decision/result primitives
- Acknowledgment and audit abstractions
- Capability-token abstractions
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
- AI model dependencies
- Robotics or physical execution dependencies

## Planned Package Family

The intended package family is:

```text
CDCavell.ASIBackbone.Core
CDCavell.ASIBackbone.AspNetCore
CDCavell.ASIBackbone.Storage.InMemory
CDCavell.ASIBackbone.Storage.EntityFrameworkCore
CDCavell.ASIBackbone.Signing
CDCavell.ASIBackbone.Samples
CDCavell.ASIBackbone.Robotics
```

Package names may be adjusted before stable release.

## CDCavell.ASIBackbone.Core

Current package.

Defines framework-neutral domain abstractions and primitives.

## CDCavell.ASIBackbone.AspNetCore

Planned future package for ASP.NET Core integration.

This may eventually provide service registration extensions, middleware, endpoint mapping, policy hooks, current-user/current-actor resolution, request-level policy context building, or Problem Details integration where appropriate.

## CDCavell.ASIBackbone.Storage.InMemory

Planned future package for samples, tests, and early validation hosts that need non-production storage behavior.

## CDCavell.ASIBackbone.Storage.EntityFrameworkCore

Planned future package for EF Core model contributions.

The preferred persistence model is host-owned data access:

> ASIBackbone owns its domain model.<br />
> The host application owns the DbContext.<br />
> ASIBackbone provides model configuration hooks.<br />

## CDCavell.ASIBackbone.Signing

Planned future package for signing and verification support around decision receipts, policy hashes, capability tokens, and audit records.

## CDCavell.ASIBackbone.Samples

Planned future samples or validation hosts.

Samples may include a console or worker host, a plain ASP.NET Core host, and a NetCoreApplicationTemplate-based host.

## CDCavell.ASIBackbone.Robotics

Later integration package.

Robotics should remain a later-stage gateway example. The first release path should prove the policy, decision, acknowledgment, audit, capability-token, and gateway patterns before moving toward physical execution scenarios.

## Relationship to NetCoreApplicationTemplate

NetCoreApplicationTemplate may be used as a preferred host baseline during development and validation, but ASIBackbone should not require it.

The intended relationship is:

```text
NetCoreApplicationTemplate
    = preferred host baseline

ASIBackbone
    = optional governance/module package family

Consumer application
    = chooses whether to use either or both
```

A consumer should be able to use ASIBackbone in:

- an application generated from NetCoreApplicationTemplate
- an existing ASP.NET Core application
- a future custom host that provides the required infrastructure

## Alignment Boundary

ASIBackbone documentation may reference the broader ASI Backbone concept and Eden/ASI framework as conceptual inspiration.

Safe language:

- ASIBackbone is inspired by the ASI Backbone framework.
- ASIBackbone implements governance-oriented software primitives.
- ASIBackbone helps structure consequential decision flow through constraints, acknowledgment, audit, and capability boundaries.
- ASIBackbone can surround intelligent or decision-producing systems with accountable execution infrastructure.

Avoid language such as:

- ASIBackbone implements ASI.
- ASIBackbone proves the Eden Hypothesis.
- ASIBackbone is an AI model.
- ASIBackbone replaces AI safety governance, legal review, or organizational accountability.

## Design Principles

- Keep Core small.
- Keep Core dependency-light.
- Avoid hidden host assumptions.
- Prefer explicit integration over magic.
- Let the host own infrastructure.
- Let future packages own persistence, web integration, signing, storage, samples, and external execution concerns.
- Keep package boundaries clear before adding behavior.
- Treat NetCoreApplicationTemplate as a preferred host, not a parent framework.
- Treat ASIBackbone as governance infrastructure, not an intelligence engine.
