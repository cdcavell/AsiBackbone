# ASI Backbone Documentation

Welcome to the ASI Backbone documentation site.

AsiBackbone is a .NET governance and policy-control framework inspired by the ASI Backbone concept. The project begins as a **governance spine**, not an intelligence engine. Its purpose is to define practical software patterns for policy evaluation, decision results, acknowledgment workflows, audit receipts, capability-gated execution, and host integration.

> [!IMPORTANT]
> AsiBackbone does not implement artificial superintelligence. It provides framework-neutral building blocks for governing consequential actions in software systems.

## Start here

* [Getting Started](articles/getting-started.md)
  Project orientation, local build instructions, and the current package direction.

* [Core Domain Language](articles/core-domain-language.md)
  Initial terminology and `0.1.0-alpha.1` Core boundary for governance spine, constraints, collapse boundary, actor context, decision results, audit residue, acknowledgment, capability tokens, and gateway boundaries.

* [Equations and Toy Models](articles/equations-and-toy-models.md)
  Explains the conceptual progression from `Λ(t)` to `Λ(τ)` to `ΛS(x, τ)` and maps the Eden/ASI collapse notation into practical AsiBackbone software terms: active policy structure, allowed decision states, acknowledgment, audit residue, and gateway-safe execution.

* [Alpha Package Boundary](articles/alpha-package-boundary.md)
  Focused release-boundary guidance for `CDCavell.AsiBackbone.Core` before integration packages are added.

* [Documentation Articles](articles/)
  Conceptual and implementation documentation for the ASI Backbone package family.

* [API Reference](https://cdcavell.github.io/AsiBackbone/api/CDCavell.AsiBackbone.Core.html)
  Generated API documentation for public types.

* [Test Coverage](coverage/)
  Generated test coverage report when available.

* [Repository](https://github.com/cdcavell/AsiBackbone)
  Source code, issues, pull requests, and release history.

## Concept and model pages

The ASI Backbone documentation will include a set of concept pages that explain the framework behind the package family. These pages should remain careful about the distinction between software implementation, structural analogy, and theoretical framework.

### ASI Backbone concept synopsis

Planned article: `articles/asi-backbone-concept.md`

This page should summarize the ASI Backbone concept in plain technical language:

* AsiBackbone as a governance spine
* Policy-controlled decision flow
* Controlled collapse through constraints
* Regional/local policy enforcement
* Auditability and responsibility trails
* Gateway patterns for external systems
* Clear boundary that this project is not an ASI implementation

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

## CDCavell.AsiBackbone.Core

Current package.

`CDCavell.AsiBackbone.Core` is the dependency-light foundation package. It defines shared contracts, domain abstractions, result primitives, and framework-neutral language used by the rest of the package family.

Core should remain free of direct ASP.NET Core, Entity Framework Core, database-provider, host-template, robotics, and AI-model assumptions.

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

Planned package or future split candidate.

If Core grows too large, shared interfaces and primitive contracts may be separated into an Abstractions package. Until that split is justified, these types may remain in Core.

Primary responsibilities:

* Minimal shared interfaces
* Decision result contracts
* Policy evaluation contracts
* Audit receipt contracts
* Capability token contracts
* No implementation dependencies

## CDCavell.AsiBackbone.AspNetCore

Planned package.

ASP.NET Core integration should live outside Core. This package may eventually provide service registration, middleware, endpoint mapping, authorization-policy integration, current-user/current-actor resolution, and request-level policy hooks.

Primary responsibilities:

* Dependency injection extensions
* Middleware integration
* Endpoint filters or route handlers
* Current actor resolution
* HTTP request policy context building
* Problem Details integration where appropriate

## CDCavell.AsiBackbone.Storage.InMemory

Planned package.

An in-memory storage provider can support samples, testing, and early integration validation without requiring a database.

Primary responsibilities:

* In-memory acknowledgment records
* In-memory audit receipts
* In-memory policy registry
* Test/demo storage behavior
* Non-production sample support

## CDCavell.AsiBackbone.Storage.EntityFrameworkCore

Planned package.

Entity Framework Core integration should remain host-owned. AsiBackbone should provide model configuration hooks and persistence abstractions while allowing the consumer application to own the `DbContext`.

Primary responsibilities:

* EF Core model configuration
* Entity mappings
* Audit receipt persistence
* Acknowledgment persistence
* Policy version persistence
* Host-owned DbContext integration

## CDCavell.AsiBackbone.Signing

Planned package.

Signing support can provide cryptographic integrity for decision receipts, policy versions, capability tokens, and audit records.

Primary responsibilities:

* Signed decision receipts
* Policy hash support
* Capability token signing helpers
* Signature verification abstractions
* Key-provider integration seams

## CDCavell.AsiBackbone.Samples

Planned package or folder.

Samples should demonstrate how the package family is used in realistic host applications.

Potential samples:

* Minimal console or worker sample
* Plain ASP.NET Core sample
* NetCoreApplicationTemplate-based sample
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

This gives AsiBackbone a practical software foundation while preserving the broader framework boundary.

## Design principle

AsiBackbone should make consequential software actions easier to govern, audit, constrain, acknowledge, and explain.

It should not pretend to be an intelligence engine.
