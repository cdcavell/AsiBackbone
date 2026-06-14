# Getting Started

This guide explains the current direction of the AsiBackbone repository and how to begin working with the project.

In this software project, **ASI** means **Accountable Systems Infrastructure**. AsiBackbone is a stable `1.0.0` .NET package family for governance-oriented decision flow. The foundation package is `CDCavell.AsiBackbone.Core`, with optional integration packages for in-memory validation, EF Core host-owned persistence, and ASP.NET Core host integration.

> [!IMPORTANT]
> This project does not implement artificial superintelligence. It provides Accountable Systems Infrastructure: governance-oriented software building blocks inspired by broader Backbone framework concepts.

## Current status

The repository has completed the initial Core foundation work and now includes optional packages for in-memory validation, EF Core host-owned persistence, and ASP.NET Core host integration.

The implemented stable `1.0.0` package lineup is:

```text
CDCavell.AsiBackbone.Core
CDCavell.AsiBackbone.Storage.InMemory
CDCavell.AsiBackbone.EntityFrameworkCore
CDCavell.AsiBackbone.AspNetCore
```

Planned or later package areas remain separate from the implemented stable lineup:

```text
CDCavell.AsiBackbone.Signing
CDCavell.AsiBackbone.Samples
CDCavell.AsiBackbone.Robotics
```

The current implementation direction is:

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

## Prerequisites

Install the following before working with the repository:

* .NET SDK
* Git
* A code editor such as Visual Studio, Visual Studio Code, or JetBrains Rider

The repository uses local .NET tools for documentation and reporting. Restore them after cloning the repository.

```bash
dotnet tool restore
```

## Clone the repository

```bash
git clone https://github.com/cdcavell/AsiBackbone.git
cd AsiBackbone
```

## Restore, build, and test

```bash
dotnet restore AsiBackbone.slnx
dotnet build AsiBackbone.slnx --configuration Release
dotnet test AsiBackbone.slnx --configuration Release --no-build --no-restore
```

## Build the documentation site locally

The documentation site uses DocFX.

```bash
dotnet tool restore
dotnet tool run docfx docs/docfx.json --serve
```

DocFX will build the documentation and serve it locally. Use the local URL printed in the terminal to preview changes before opening a pull request.

## Project orientation

AsiBackbone should be understood as a governance spine for Accountable Systems Infrastructure.

A typical AsiBackbone-style flow is:

```text
Intent or request
  -> Policy context
  -> Policy evaluation
  -> Decision result
  -> Optional acknowledgment
  -> Audit receipt
  -> Optional capability token
  -> Gateway or host execution
```

The goal is controlled, auditable decision flow.

A request should not move directly into action. It should pass through explicit policy, constraint, acknowledgment, and audit boundaries first.

## Core concepts

## Intent or request

An intent represents the action being requested.

Examples:

* Approve a document
* Call an external API
* Start an administrative workflow
* Execute a simulated gateway command
* Request access to a protected operation

## Policy context

A policy context gathers the information needed to evaluate the request.

Possible context values include:

* Actor or user
* Requested action
* Target resource
* Region or jurisdiction
* Risk level
* Current policy version
* Correlation ID
* Host application metadata

## Policy evaluation

A policy evaluator determines whether the request can proceed.

Implemented governance outcomes include:

* `Allowed`
* `Warning`
* `Denied`
* `Deferred`
* `AcknowledgmentRequired`
* `EscalationRecommended`

These outcomes are represented by `GovernanceDecision` and `GovernanceDecisionOutcome` so every package can reason about decisions consistently.

## Decision result

The decision result is one of the central objects in the package family.

A decision result should answer:

* What was requested?
* Who or what requested it?
* What decision was made?
* Which policy version produced the decision?
* Was acknowledgment required?
* Was escalation required?
* What reason codes explain the decision?
* What correlation ID links this decision to logs and audit records?

## Acknowledgment workflow

Some actions may require a user or system acknowledgment before proceeding.

This workflow supports the broader Dynamic Liability Handshake concept while allowing the public API to use implementation-focused names such as acknowledgment, responsibility handshake, or reflexive acknowledgment.

An acknowledgment workflow may include:

* Intent summary
* Risk notice
* Required confirmation
* Consent or responsibility record
* Timestamp
* Actor identity
* Policy version
* Correlation ID

## Audit receipt

An audit receipt records what happened.

A useful audit receipt should include:

* Decision ID
* Correlation ID
* Actor or system identity
* Requested action
* Decision outcome
* Reason codes
* Policy version
* Policy hash
* Timestamp
* Optional signature

Audit receipts should make decision flow explainable after the fact.

## Capability token

A capability token represents short-lived, scoped permission to perform an operation.

A capability token should be:

* Time-bound
* Purpose-bound
* Scope-bound
* Revocable where possible
* Signed or verifiable where appropriate
* Traceable through audit records

## Gateway pattern

A gateway pattern applies policy and capability checks before a request reaches an external or consequential system.

Examples should focus on safe software scenarios first:

* Document approval
* External API execution
* Administrative workflow execution
* Simulated command validation

Robotics and physical execution should remain later-stage examples after the core governance pattern is stable.

## Current packages

## CDCavell.AsiBackbone.Core

`CDCavell.AsiBackbone.Core` is the framework-neutral foundation package.

It provides:

* Framework-neutral abstractions
* Decision/result primitives
* Policy evaluation contracts
* Acknowledgment contracts
* Audit contracts
* Capability-token contracts

It avoids:

* ASP.NET Core dependencies
* Entity Framework Core dependencies
* Web middleware
* Endpoint mapping
* Host startup logic
* Database-provider assumptions
* Direct dependency on NetCoreApplicationTemplate

## CDCavell.AsiBackbone.Storage.InMemory

`CDCavell.AsiBackbone.Storage.InMemory` provides non-durable in-memory storage helpers for tests, samples, and local validation hosts.

It should not be used as durable production storage.

## CDCavell.AsiBackbone.EntityFrameworkCore

`CDCavell.AsiBackbone.EntityFrameworkCore` provides EF Core model configuration and durable accountability persistence while preserving host ownership of the `DbContext`, provider, connection string, migrations, deployment, and schema lifecycle.

## CDCavell.AsiBackbone.AspNetCore

`CDCavell.AsiBackbone.AspNetCore` provides thin ASP.NET Core host adapters for service registration, request correlation, audit enrichment, HTTP result mapping, and acknowledgment challenge helpers.

It does not own authentication, authorization, persistence, route exposure, UI rendering, policy definitions, or operational execution.

## Planned package areas

Future package areas may include:

* `CDCavell.AsiBackbone.Signing`
* `CDCavell.AsiBackbone.Samples`
* `CDCavell.AsiBackbone.Robotics`

Planned package names are not part of the `1.0.0` stable contract unless a future release explicitly ships them as stable packages.

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

A consumer can use AsiBackbone in:

* an application generated from NetCoreApplicationTemplate
* an existing ASP.NET Core application
* a custom host that provides the required infrastructure

## Recommended first implementation target

The first useful vertical slice was to prove the basic decision flow without requiring ASP.NET Core, EF Core, or external infrastructure.

That foundation now supports integration packages that build on Core while preserving host ownership.

## Documentation guidance

When adding documentation, keep the distinction clear between:

* Implemented stable behavior
* Alpha, preview, or sample behavior
* Host responsibilities
* Future provider work
* Accountable Systems Infrastructure framing
* Broader Eden/Backbone theoretical background
* Structural analogy
* Testable or operational software claims

The documentation should be practical first, conceptual second, and careful about overclaiming.
