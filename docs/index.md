# ASI Backbone Documentation

Welcome to the ASI Backbone documentation site.

In this software project, **ASI** means **Accountable Systems Infrastructure**. AsiBackbone is a .NET governance and policy-control framework inspired by broader Eden/Backbone governance concepts, but implemented as practical software infrastructure. The project begins as a **governance spine**, not an intelligence engine. Its purpose is to define practical software patterns for policy evaluation, decision results, acknowledgment workflows, audit receipts, capability-gated execution, and host integration.

> [!IMPORTANT]
> AsiBackbone does not implement artificial superintelligence. It provides framework-neutral building blocks for governing consequential actions in software systems.

## Start here

* [Getting Started](articles/getting-started.md)
  Project orientation, local build instructions, and the current package direction.

* [Core Domain Language](articles/core-domain-language.md)
  Initial terminology and Core boundary for Accountable Systems Infrastructure, governance spine, constraints, collapse boundary, actor context, decision results, audit residue, acknowledgment, capability tokens, and gateway boundaries.

* [Equations and Toy Models](articles/equations-and-toy-models.md)
  Explains the conceptual progression from `Λ(t)` to `Λ(τ)` to `ΛS(x, τ)` and maps the Eden/Backbone collapse notation into practical AsiBackbone software terms: active policy structure, allowed decision states, acknowledgment, audit residue, and gateway-safe execution.

* [Alpha Package Boundary](articles/alpha-package-boundary.md)
  Focused release-boundary guidance for the Core package and integration package boundaries.

* [Documentation Articles](articles/)
  Conceptual and implementation documentation for the ASI Backbone package family.

* [API Reference](https://cdcavell.github.io/AsiBackbone/api/CDCavell.AsiBackbone.Core.html)
  Generated API documentation for public types.

* [Quality Reports](quality/)
  Landing page for coverage and mutation-analysis reports when generated.

* [Repository](https://github.com/cdcavell/AsiBackbone)
  Source code, issues, pull requests, and release history.

## Concept and model pages

The ASI Backbone documentation will include a set of concept pages that explain the framework behind the package family. These pages should remain careful about the distinction between software implementation, structural analogy, and theoretical framework.

### ASI Backbone concept synopsis

Planned article: `articles/asi-backbone-concept.md`

This page should summarize the ASI Backbone concept in plain technical language:

* ASI as Accountable Systems Infrastructure
* AsiBackbone as a governance spine
* Policy-controlled decision flow
* Controlled collapse through constraints
* Regional/local policy enforcement
* Auditability and responsibility trails
* Gateway patterns for external systems
* Clear boundary that this project is not an artificial superintelligence implementation

### Dynamic Liability Handshake

Planned article: `articles/dynamic-liability-handshake.md`

This page should document the acknowledgment workflow that occurs before consequential action.

Suggested sections:

* Intent capture
* Risk and policy evaluation
* Required acknowledgment
* Consent or responsibility record
* Audit receipt
* Human/system responsibility boundary

The public API may use safer implementation terms such as acknowledgment, responsibility handshake, or reflexive acknowledgment while the documentation explains the broader Dynamic Liability Handshake concept.

### Gateway and regional policy flow

Planned article: `articles/gateway-and-regional-policy-flow.md`

This page should describe how AsiBackbone handles externally consequential systems.

Suggested sections:

* No direct global-to-edge command pattern
* Regional/local policy evaluation
* Operational gateway validation
* Capability token enforcement
* Audit and revocation
* Robotics as a later integration example, not a first-release requirement

## Package documentation

The AsiBackbone package family should remain modular. Consumers should be able to adopt the pieces they need without inheriting unnecessary host assumptions.

The current implemented alpha package lineup is:

```text
CDCavell.AsiBackbone.Core
CDCavell.AsiBackbone.Storage.InMemory
CDCavell.AsiBackbone.EntityFrameworkCore
CDCavell.AsiBackbone.AspNetCore
```

Planned or later package areas remain separate from the current implemented lineup:

```text
CDCavell.AsiBackbone.Signing
CDCavell.AsiBackbone.Samples
CDCavell.AsiBackbone.Robotics
```

Package names may still be adjusted before stable release, but documentation should use the public alpha package IDs shown above unless a future release changes them.

## CDCavell.AsiBackbone.Core

Current package.

`CDCavell.AsiBackbone.Core` is the dependency-light foundation package. It defines shared contracts, domain abstractions, result primitives, and framework-neutral language used by the rest of the package family.

Core remains free of direct ASP.NET Core, Entity Framework Core, database-provider, host-template, robotics, and AI-model assumptions.

Primary responsibilities:

* Core domain abstractions
* Policy and constraint contracts
* Decision result primitives
* Operation result primitives
* Acknowledgment and audit abstractions
* Capability-token abstractions
* Policy version and policy hash fields
* Shared value objects
* Framework-neutral domain language

## CDCavell.AsiBackbone.Abstractions

Future split candidate.

If Core grows too large, shared interfaces and primitive contracts may be separated into an Abstractions package. Until that split is justified, these types remain in Core and `CDCavell.AsiBackbone.Abstractions` should not be described as part of the current package lineup.

Potential responsibilities if later added:

* Minimal shared interfaces
* Decision result contracts
* Policy evaluation contracts
* Audit receipt contracts
* Capability token contracts
* No implementation dependencies

## CDCavell.AsiBackbone.AspNetCore

Current package.

`CDCavell.AsiBackbone.AspNetCore` provides ASP.NET Core host integration seams while keeping Core framework-neutral. It adapts HTTP request context into AsiBackbone governance language and helps hosts map governance outcomes to HTTP-friendly responses when explicitly used by the application.

Primary responsibilities:

* Dependency injection extensions
* ASP.NET Core options and startup validation
* HTTP actor context resolution
* Request correlation and audit enrichment
* HTTP result mapping helpers
* Acknowledgment challenge models and response handling
* Host-boundary validation seams

The host application remains responsible for authentication, authorization, endpoint exposure, persistence registration, UI rendering, and operational execution.

## CDCavell.AsiBackbone.Storage.InMemory

Current package.

`CDCavell.AsiBackbone.Storage.InMemory` provides non-durable in-memory storage helpers for local validation, samples, and tests. It supports early integration validation without requiring a database and should not be used as durable production storage.

Primary responsibilities:

* In-memory acknowledgment records
* In-memory audit receipts
* In-memory policy or test/demo state where appropriate
* Local validation behavior
* Non-production sample support

## CDCavell.AsiBackbone.EntityFrameworkCore

Current package.

`CDCavell.AsiBackbone.EntityFrameworkCore` provides EF Core model configuration and persistence integration through a host-owned `DbContext`. AsiBackbone contributes model configuration and storage helpers while the consuming application owns the database provider, connection string, migrations, deployment, and schema lifecycle.

Primary responsibilities:

* EF Core model configuration
* Entity mappings
* Audit ledger persistence
* Acknowledgment persistence
* Policy version persistence
* Host-owned DbContext integration

## CDCavell.AsiBackbone.Signing

Planned future package.

Signing support can provide cryptographic integrity for decision receipts, policy versions, capability tokens, and audit records.

Primary responsibilities:

* Signed decision receipts
* Policy hash support
* Capability token signing helpers
* Signature verification abstractions
* Key-provider integration seams

## CDCavell.AsiBackbone.Samples

Planned future package or continuing samples folder.

The repository currently includes `samples/PlainAspNetCoreHost` as the canonical in-repository ASP.NET Core validation sample. A future samples package or expanded samples folder may demonstrate additional host scenarios.

Potential samples:

* Minimal console or worker sample
* Plain ASP.NET Core sample
* NetCoreApplicationTemplate-based external validation guidance
* Policy evaluation demo
* Acknowledgment workflow demo
* Simulated gateway validation demo

## CDCavell.AsiBackbone.Robotics

Later integration package.

Robotics should remain a later-stage integration example. The initial project should prove the policy, decision, acknowledgment, audit, capability-token, and gateway patterns before moving toward physical or robotic execution scenarios.

Primary responsibilities, if later added:

* Simulated robot command validation
* Operational gateway contracts
* Capability-scoped command authorization
* Safety-bound command envelopes
* Regional/local policy enforcement examples

## Project direction

The current alpha implementation path is:

1. Core governance primitives
2. Policy evaluator pipeline
3. Decision result model
4. Acknowledgment/handshake workflow
5. Audit residue and audit ledger contracts
6. Capability token abstractions
7. In-memory local validation storage
8. EF Core host-owned persistence integration
9. ASP.NET Core host integration
10. Plain ASP.NET Core sample host
11. Documentation and host-validation guidance

Future work may add signing, gateway integrations, additional samples, robotics examples, and stable release packaging.

This gives AsiBackbone a practical software foundation while preserving the broader framework boundary.

## Design principle

AsiBackbone should make consequential software actions easier to govern, audit, constrain, acknowledge, and explain.

It should be understood as Accountable Systems Infrastructure, not an intelligence engine.
