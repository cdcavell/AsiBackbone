# ASI Backbone Documentation

Welcome to the ASI Backbone documentation site.

ASIBackbone is a .NET governance and policy-control framework inspired by the ASI Backbone concept. The project begins as a **governance spine**, not an intelligence engine. Its purpose is to define practical software patterns for policy evaluation, decision results, acknowledgment workflows, audit receipts, capability-gated execution, and host integration.

> [!IMPORTANT]
> ASIBackbone does not implement artificial superintelligence. It provides framework-neutral building blocks for governing consequential actions in software systems.

## Start here

* [Getting Started](articles/getting-started.md)
  Project orientation, local build instructions, and the current package direction.

* [Documentation Articles](articles/)
  Conceptual and implementation documentation for the ASI Backbone package family.

* [API Reference](api/)
  Generated API documentation for public types.

* [Test Coverage](coverage/)
  Generated test coverage report when available.

* [Repository](https://github.com/cdcavell/ASIBackbone)
  Source code, issues, pull requests, and release history.

## Concept and model pages

The ASI Backbone documentation will include a set of concept pages that explain the framework behind the package family. These pages should remain careful about the distinction between software implementation, structural analogy, and theoretical framework.

### ASI Backbone concept synopsis

Planned article: `articles/asi-backbone-concept.md`

This page should summarize the ASI Backbone concept in plain technical language:

* ASIBackbone as a governance spine
* Policy-controlled decision flow
* Controlled collapse through constraints
* Regional/local policy enforcement
* Auditability and responsibility trails
* Gateway patterns for external systems
* Clear boundary that this project is not an ASI implementation

### Equations and toy models

Planned article: `articles/equations-and-toy-models.md`

This page should explain the conceptual equations and toy models that inspired the software architecture.

Suggested sections:

* Original collapse accumulator: `Λ(t)`
* Relational-time reading: `Λ(τ)`
* Structure-conditioned collapse: `ΛS(x, τ)`
* Active constraint structure: `Sτ`
* Allowed-state set: `A(Sτ)`
* Decision outcomes as allowed software states
* Toy model examples for policy narrowing, acknowledgment, and gateway enforcement

The software interpretation is straightforward: a request does not become any possible action whatsoever. It becomes only what the active policy structure allows.

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

This page should describe how ASIBackbone handles externally consequential systems.

Suggested sections:

* No direct global-to-edge command pattern
* Regional/local policy evaluation
* Operational gateway validation
* Capability token enforcement
* Audit and revocation
* Robotics as a later integration example, not a first-release requirement

## Package documentation

The ASIBackbone package family should remain modular. Consumers should be able to adopt the pieces they need without inheriting unnecessary host assumptions.

## CDCavell.ASIBackbone.Core

Current package.

`CDCavell.ASIBackbone.Core` is the dependency-light foundation package. It defines shared contracts, domain abstractions, result primitives, and framework-neutral language used by the rest of the package family.

Core should remain free of direct ASP.NET Core, Entity Framework Core, database-provider, and host-template assumptions.

Primary responsibilities:

* Core domain abstractions
* Policy and constraint contracts
* Decision result primitives
* Acknowledgment and audit abstractions
* Capability-token abstractions
* Shared value objects
* Framework-neutral domain language

## CDCavell.ASIBackbone.Abstractions

Planned package or future split candidate.

If Core grows too large, shared interfaces and primitive contracts may be separated into an Abstractions package. Until that split is justified, these types may remain in Core.

Primary responsibilities:

* Minimal shared interfaces
* Decision result contracts
* Policy evaluation contracts
* Audit receipt contracts
* Capability token contracts
* No implementation dependencies

## CDCavell.ASIBackbone.AspNetCore

Planned package.

ASP.NET Core integration should live outside Core. This package may eventually provide service registration, middleware, endpoint mapping, authorization-policy integration, current-user/current-actor resolution, and request-level policy hooks.

Primary responsibilities:

* Dependency injection extensions
* Middleware integration
* Endpoint filters or route handlers
* Current actor resolution
* HTTP request policy context building
* Problem Details integration where appropriate

## CDCavell.ASIBackbone.Storage.InMemory

Planned package.

An in-memory storage provider can support samples, testing, and early integration validation without requiring a database.

Primary responsibilities:

* In-memory acknowledgment records
* In-memory audit receipts
* In-memory policy registry
* Test/demo storage behavior
* Non-production sample support

## CDCavell.ASIBackbone.Storage.EntityFrameworkCore

Planned package.

Entity Framework Core integration should remain host-owned. ASIBackbone should provide model configuration hooks and persistence abstractions while allowing the consumer application to own the `DbContext`.

Primary responsibilities:

* EF Core model configuration
* Entity mappings
* Audit receipt persistence
* Acknowledgment persistence
* Policy version persistence
* Host-owned DbContext integration

## CDCavell.ASIBackbone.Signing

Planned package.

Signing support can provide cryptographic integrity for decision receipts, policy versions, capability tokens, and audit records.

Primary responsibilities:

* Signed decision receipts
* Policy hash support
* Capability token signing helpers
* Signature verification abstractions
* Key-provider integration seams

## CDCavell.ASIBackbone.Samples

Planned package or folder.

Samples should demonstrate how the package family is used in realistic host applications.

Potential samples:

* Minimal console or worker sample
* Plain ASP.NET Core sample
* NetCoreApplicationTemplate-based sample
* Policy evaluation demo
* Acknowledgment workflow demo
* Simulated gateway validation demo

## CDCavell.ASIBackbone.Robotics

Later integration package.

Robotics should remain a later-stage integration example. The initial project should prove the policy, decision, acknowledgment, audit, capability-token, and gateway patterns before moving toward physical or robotic execution scenarios.

Primary responsibilities, if later added:

* Simulated robot command validation
* Operational gateway contracts
* Capability-scoped command authorization
* Safety-bound command envelopes
* Regional/local policy enforcement examples

## Project direction

The first implementation path should remain:

1. Abstractions
2. Policy pipeline
3. Decision result model
4. Acknowledgment/handshake workflow
5. Audit receipt
6. Capability token
7. ASP.NET Core integration
8. Sample app
9. Documentation

This gives ASIBackbone a practical software foundation while preserving the broader framework boundary.

## Design principle

ASIBackbone should make consequential software actions easier to govern, audit, constrain, acknowledge, and explain.

It should not pretend to be an intelligence engine.
