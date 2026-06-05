# Getting Started

This guide explains the current direction of the AsiBackbone repository and how to begin working with the project.

AsiBackbone is currently in an early foundation stage. The first package focus is `CDCavell.AsiBackbone.Core`, a dependency-light package intended to define shared contracts, abstractions, result primitives, and domain language.

> [!IMPORTANT]
> This project does not implement artificial superintelligence. It provides governance-oriented software building blocks inspired by the ASI Backbone framework.

## Current status

The repository is currently focused on establishing the Core package boundary before adding persistence, ASP.NET Core integration, samples, or stable packaging.

The current implementation direction is:

1. Abstractions
2. Policy pipeline
3. Decision result model
4. Acknowledgment/handshake workflow
5. Audit receipt
6. Capability token
7. ASP.NET Core integration
8. Sample app
9. Documentation

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

AsiBackbone should be understood as a governance spine.

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

Expected outcomes include:

* `Allow`
* `Deny`
* `Defer`
* `RequireAcknowledgment`
* `Escalate`

These outcomes should be represented by a shared decision result model so that every package can reason about decisions consistently.

## Decision result

The decision result should become one of the central objects in the package family.

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

Early examples should focus on safe software scenarios:

* Document approval
* External API execution
* Administrative workflow execution
* Simulated command validation

Robotics and physical execution should remain later-stage examples after the core governance pattern is stable.

## Current package

## CDCavell.AsiBackbone.Core

`CDCavell.AsiBackbone.Core` is the current foundation package.

It should provide:

* Framework-neutral abstractions
* Decision/result primitives
* Policy evaluation contracts
* Acknowledgment contracts
* Audit contracts
* Capability-token contracts
* Shared domain language

It should avoid:

* ASP.NET Core dependencies
* Entity Framework Core dependencies
* Web middleware
* Endpoint mapping
* Host startup logic
* Database-provider assumptions
* Direct dependency on NetCoreApplicationTemplate

## Planned package areas

Future packages may include:

* `CDCavell.AsiBackbone.AspNetCore`
* `CDCavell.AsiBackbone.Storage.InMemory`
* `CDCavell.AsiBackbone.Storage.EntityFrameworkCore`
* `CDCavell.AsiBackbone.Signing`
* `CDCavell.AsiBackbone.Samples`
* `CDCavell.AsiBackbone.Robotics`

Package names may be adjusted before stable release.

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

A consumer should eventually be able to use AsiBackbone in:

* an application generated from NetCoreApplicationTemplate
* an existing ASP.NET Core application
* a custom host that provides the required infrastructure

## Recommended first implementation target

The first useful vertical slice should prove the basic decision flow without requiring ASP.NET Core, EF Core, or external infrastructure.

A strong first target would include:

1. A request/intent abstraction
2. A policy context abstraction
3. A policy evaluator abstraction
4. A decision result model
5. Reason codes
6. Correlation ID support
7. Policy version/hash fields
8. A basic audit receipt abstraction
9. Unit tests around allow, deny, defer, require acknowledgment, and escalate outcomes

After that foundation is stable, integration packages can build on it.

## Documentation guidance

When adding documentation, keep the distinction clear between:

* Implemented package behavior
* Planned package behavior
* Conceptual ASI Backbone framing
* Eden/ASI theoretical background
* Structural analogy
* Testable or operational software claims

The documentation should be practical first, conceptual second, and careful about overclaiming.
