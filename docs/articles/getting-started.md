# Getting Started

This guide explains the current direction of the AsiBackbone repository and how to begin working with the project.

In this software project, **ASI** means **Accountable Systems Infrastructure**. AsiBackbone is a stable `1.2.x` .NET package family for governance-oriented decision flow. The foundation package is `AsiBackbone.Core`, with optional integration packages for in-memory validation, EF Core host-owned persistence, ASP.NET Core host integration, analyzer guidance, OpenTelemetry projection, and signing-provider boundaries.

> [!IMPORTANT]
> This project does not implement artificial superintelligence. It provides Accountable Systems Infrastructure: governance-oriented software building blocks inspired by broader Backbone framework concepts.

## Current status

The repository includes the Core foundation and optional packages for in-memory validation, EF Core host-owned persistence, ASP.NET Core host integration, Roslyn analyzer safety rails, OpenTelemetry governance emission, local-development signing, and managed-key signing adapter wiring.

The stable `1.2.1` package lineup is:

```text
AsiBackbone.Core
AsiBackbone.DependencyInjection
AsiBackbone.Storage.InMemory
AsiBackbone.EntityFrameworkCore
AsiBackbone.AspNetCore
AsiBackbone.Testing
AsiBackbone.Templates
AsiBackbone.Analyzers
AsiBackbone.OpenTelemetry
AsiBackbone.Signing.LocalDevelopment
AsiBackbone.Signing.ManagedKey
```

Planned or later package areas remain separate from the implemented stable lineup:

```text
AsiBackbone.EventHubs
AsiBackbone.Purview
AsiBackbone.Robotics
AsiBackbone.ImmutableStorage
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
10. Durable audit lifecycle and governance outbox persistence
11. Provider-neutral governance emission contracts
12. OpenTelemetry provider projection
13. Analyzer safety rails
14. Signing-ready metadata and provider package boundaries
15. Plain ASP.NET Core sample host
16. Documentation and host-validation guidance

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
  -> Local audit/outbox persistence
  -> Optional signing or verification
  -> Optional provider emission
  -> Gateway or host execution
```

The goal is controlled, auditable decision flow.

A request should not move directly into action. It should pass through explicit policy, constraint, acknowledgment, audit, persistence, optional signing/verification, and optional provider-emission boundaries first.

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
* Optional signature metadata

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

## Durable outbox and provider emission

The durable governance outbox preserves local accountability records before downstream provider delivery is attempted.

Provider emission is optional. Hosts can use provider-neutral emission contracts and adopt a concrete provider such as `AsiBackbone.OpenTelemetry` when governance events should be projected into diagnostics, observability, or governance systems.

The host remains responsible for deciding whether a downstream system is authoritative, supplemental, or enrichment-only.

## Signing and verification boundary

Core keeps signing and verification provider-neutral. The signing packages provide optional provider boundaries:

* `AsiBackbone.Signing.LocalDevelopment` for tests, samples, and local proof paths.
* `AsiBackbone.Signing.ManagedKey` for host-owned managed-key client integration.

Signing does not equal production tamper-evidence by itself. Hosts own key custody, verification policy, storage controls, retention, monitoring, and operational procedures.

## Gateway pattern

A gateway pattern applies policy and capability checks before a request reaches an external or consequential system.

Examples should focus on safe software scenarios first:

* Document approval
* External API execution
* Administrative workflow execution
* Simulated command validation

Robotics and physical execution should remain future, sample-only, or separately reviewed provider scenarios unless a later stable release explicitly ships them as part of the package family.

## Current packages

## AsiBackbone.Core

`AsiBackbone.Core` is the framework-neutral foundation package.

It provides:

* Framework-neutral abstractions
* Decision/result primitives
* Policy evaluation contracts
* Acknowledgment contracts
* Audit contracts
* Capability-token contracts
* Durable outbox contracts
* Provider-neutral governance emission contracts
* Signing-ready and verification-policy seams

It avoids:

* ASP.NET Core dependencies
* Entity Framework Core dependencies
* Web middleware
* Endpoint mapping
* Host startup logic
* Database-provider assumptions
* Direct dependency on NetCoreApplicationTemplate
* Cloud-provider SDK assumptions
* Signing-provider implementation assumptions

## AsiBackbone.Storage.InMemory

`AsiBackbone.Storage.InMemory` provides non-durable in-memory storage helpers for tests, samples, and local validation hosts.

It should not be used as durable production storage.

## AsiBackbone.EntityFrameworkCore

`AsiBackbone.EntityFrameworkCore` provides EF Core model configuration and durable accountability persistence while preserving host ownership of the `DbContext`, provider, connection string, migrations, deployment, and schema lifecycle.

## AsiBackbone.AspNetCore

`AsiBackbone.AspNetCore` provides thin ASP.NET Core host adapters for service registration, request correlation, audit enrichment, HTTP result mapping, acknowledgment challenge helpers, endpoint governance, and hosted outbox drain integration.

It does not own authentication, authorization, persistence, route exposure, UI rendering, policy definitions, exporter configuration, key management, or operational execution.

## AsiBackbone.Analyzers

`AsiBackbone.Analyzers` provides Roslyn analyzer safety rails for governance persistence and continuation flows.

Analyzer guidance is build-time feedback. It is not runtime enforcement and does not prove compliance.

## AsiBackbone.OpenTelemetry

`AsiBackbone.OpenTelemetry` provides a concrete governance emission provider that projects provider-neutral governance envelopes into .NET diagnostics through `ActivitySource` and `Meter`.

Exporters such as Azure Monitor remain host-configured.

## AsiBackbone.Signing.LocalDevelopment

`AsiBackbone.Signing.LocalDevelopment` provides local-development RSA signing and verification for tests, samples, and proof paths.

It is not a production managed-key provider and does not provide protected key custody, immutability, legal non-repudiation, compliance certification, or production tamper-evidence.

## AsiBackbone.Signing.ManagedKey

`AsiBackbone.Signing.ManagedKey` provides a provider-neutral managed-key signing adapter boundary.

The host supplies the actual managed-key client, credentials, key operations, monitoring, verification path, and operational policy. The package does not include live Azure Key Vault, Managed HSM, cloud KMS, HSM, or certificate-store implementation by default.

## AsiBackbone.DependencyInjection

`AsiBackbone.DependencyInjection` provides the shared `AddAsiBackbone(...)` builder facade for host-selected provider registration.

It coordinates package registration without making Core own infrastructure, persistence, web hosting, telemetry exporters, signing providers, or execution behavior.

## AsiBackbone.Testing

`AsiBackbone.Testing` provides test-only helpers for deterministic governance and package-wiring validation.

It is intended for tests, smoke checks, and package-consumer validation. It is not runtime enforcement and should not be treated as production governance infrastructure by itself.

## AsiBackbone.Templates

`AsiBackbone.Templates` provides `dotnet new` templates for generating governed ASP.NET Core host scaffolds.

The templates are developer-experience scaffolding. They are not runtime dependencies and do not replace host-owned architecture, security, persistence, deployment, or operational review.

## Planned package areas

Future package areas may include:

* `AsiBackbone.EventHubs`
* `AsiBackbone.Purview`
* `AsiBackbone.Robotics`
* `AsiBackbone.ImmutableStorage`

Planned package names are not part of the current `1.2.x` stable contract unless a future release explicitly ships them as stable packages.

## Relationship to NetCoreApplicationTemplate

NetCoreApplicationTemplate may be used as a preferred host baseline during development and validation, but AsiBackbone should not require it.
