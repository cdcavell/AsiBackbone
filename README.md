# CDCavell.AsiBackbone

[![CI](https://github.com/cdcavell/AsiBackbone/actions/workflows/ci.yml/badge.svg)](https://github.com/cdcavell/AsiBackbone/actions/workflows/ci.yml)
[![Coverage Report](https://img.shields.io/badge/coverage%20gate-75%25-brightgreen)](https://cdcavell.github.io/AsiBackbone/coverage/index.html)
[![Documentation](https://github.com/cdcavell/AsiBackbone/actions/workflows/publish-docs.yml/badge.svg)](https://github.com/cdcavell/AsiBackbone/actions/workflows/publish-docs.yml)
[![Docs](https://img.shields.io/badge/docs-GitHub%20Pages-blue)](https://cdcavell.github.io/AsiBackbone/)
[![.NET](https://img.shields.io/badge/.NET-10.0-purple)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/license-MIT-blue)](LICENSE.txt)
[![GitHub Release](https://img.shields.io/github/v/release/cdcavell/AsiBackbone?include_prerelease=true&sort=semver&display_name=tag&label=release)](https://github.com/cdcavell/AsiBackbone/releases)
[![Zenodo DOI](https://img.shields.io/badge/DOI-10.5281%2Fzenodo.20546032-blue)](https://doi.org/10.5281/zenodo.20546032)


## Accountable Systems Infrastructure for governed .NET decision flow.

AsiBackbone is a .NET package family for building an accountable governance spine around consequential software actions.

AI systems, agents, services, and applications may produce recommendations, requests, or actions. AsiBackbone provides the infrastructure around those decisions: policy evaluation, acknowledgment workflows, audit residue, capability boundaries, and host-controlled execution.

In this software project, **ASI** means **Accountable Systems Infrastructure**.

> AI may provide the intellect. AsiBackbone provides the accountable spine.

## What is AsiBackbone?

AsiBackbone helps a host application evaluate intent before execution, apply policy constraints, require acknowledgment when needed, preserve audit records, and optionally scope follow-on execution through short-lived capability tokens.

It is designed for systems where consequential actions need to be governed, explained, audited, and constrained before the host application executes them.

AsiBackbone should be understood as **governance infrastructure**, not an intelligence engine.

> [!IMPORTANT]
> These packages do **not** implement artificial superintelligence, host AI models, train AI models, control robots, or prove the Eden/Backbone framework. They provide framework-neutral building blocks and host integration seams for governing consequential actions in software systems.

### Packages

| Package | NuGet | Downloads |
| :--- | :--- | :--- |
| Core | [![NuGet Core](https://img.shields.io/nuget/v/CDCavell.AsiBackbone.Core?label=Release)](https://www.nuget.org/packages/CDCavell.AsiBackbone.Core) | [![NuGet Core Downloads](https://img.shields.io/nuget/dt/CDCavell.AsiBackbone.Core?label=Downloads)](https://www.nuget.org/packages/CDCavell.AsiBackbone.Core) |
| AspNetCore | [![NuGet AspNetCore](https://img.shields.io/nuget/v/CDCavell.AsiBackbone.AspNetCore?label=Release)](https://www.nuget.org/packages/CDCavell.AsiBackbone.AspNetCore) | [![NuGet AspNetCore Downloads](https://img.shields.io/nuget/dt/CDCavell.AsiBackbone.AspNetCore?label=Downloads)](https://www.nuget.org/packages/CDCavell.AsiBackbone.AspNetCore) |
| Storage.InMemory | [![NuGet Storage.InMemory](https://img.shields.io/nuget/v/CDCavell.AsiBackbone.Storage.InMemory?label=Release)](https://www.nuget.org/packages/CDCavell.AsiBackbone.Storage.InMemory) | [![NuGet Storage.InMemory Downloads](https://img.shields.io/nuget/dt/CDCavell.AsiBackbone.Storage.InMemory?label=Downloads)](https://www.nuget.org/packages/CDCavell.AsiBackbone.Storage.InMemory) |
| EntityFrameworkCore | [![NuGet EntityFrameworkCore](https://img.shields.io/nuget/v/CDCavell.AsiBackbone.EntityFrameworkCore?label=Release)](https://www.nuget.org/packages/CDCavell.AsiBackbone.EntityFrameworkCore) | [![NuGet EntityFrameworkCore Downloads](https://img.shields.io/nuget/dt/CDCavell.AsiBackbone.EntityFrameworkCore?label=Downloads)](https://www.nuget.org/packages/CDCavell.AsiBackbone.EntityFrameworkCore) |


## What problem does this solve?

Many systems eventually need more than ordinary authorization and application logging.

A consequential action may need to answer:

- Is this action allowed right now?
- Which constraints, policies, and reason codes shaped the decision?
- Does this request require human acknowledgment before execution?
- Which policy version and policy hash were active?
- Can execution be scoped through a short-lived capability token?
- Can a reviewer later understand why the system allowed, warned, denied, deferred, required acknowledgment, or recommended escalation?

AsiBackbone focuses on that decision boundary: the point between proposed intent and host execution.

## What does it do?

A typical AsiBackbone flow is:

```text
Intent or request
  -> Build policy context
  -> Evaluate constraints
  -> Compose decision result
  -> Require acknowledgment when needed
  -> Write audit residue
  -> Issue optional scoped capability token
  -> Host application decides whether and how to execute
```

Core benefits include:

| Benefit | What it means |
| --- | --- |
| Policy-driven decision gating | Proposed actions can be evaluated before the host executes them. |
| Explicit decision outcomes | Decisions can be allowed, warned, denied, deferred, acknowledgment-required, or escalation-recommended. |
| Human acknowledgment workflow | Consequential actions can pause until an actor acknowledges responsibility, risk, or intent. |
| Audit residue | Decisions can preserve reason codes, policy version, policy hash, correlation ID, timestamp, and metadata. |
| Capability-scoped execution | Follow-on execution can be represented as a short-lived, scoped permission grant. |
| Host-owned integration | Applications keep ownership of the web host, persistence lifecycle, migrations, deployment, and execution behavior. |
| Framework-neutral core | Core primitives can be used without requiring ASP.NET Core, EF Core, NetCoreApplicationTemplate, AI packages, or robotics dependencies. |

See [Why AsiBackbone?](docs/articles/why-asi-backbone.md) for a fuller benefits overview.

## Who is this for?

AsiBackbone may be useful for:

- Enterprise .NET applications with consequential administrative actions.
- AI-agent gateways that need policy checks before tool or API execution.
- Human-in-the-loop workflows where approval or acknowledgment matters.
- Government, public-sector, education, healthcare, finance, legal, or other regulated systems that need stronger auditability.
- Platform engineering workflows that need clear allow, deny, defer, acknowledgment, or escalation decisions before external execution.
- Applications that need capability-scoped grants instead of broad, long-lived authority.

## What does it not do?

AsiBackbone does not:

- Replace normal authentication or authorization.
- Guarantee compliance with any law, regulation, audit framework, or security standard.
- Host, train, run, or orchestrate AI models.
- Implement artificial superintelligence.
- Execute tools, APIs, infrastructure changes, or robot commands by itself.
- Own the consuming application's database, migrations, deployment, or operational policy.
- Replace legal review, AI safety governance, organizational accountability, or operational security.

The host application remains responsible for execution behavior and operational controls.

## Current Status

Early alpha package family.

The repository has completed the initial Core foundation work and now includes optional persistence packages for in-memory validation, EF Core host-owned persistence, ASP.NET Core integration, samples and host validation. Planned follow-up milestones include signing support, gateway integrations, robotics examples, and stable release packaging.

## Project Direction

AsiBackbone is planned as a host-integrated .NET module ecosystem.

The Core package answers:

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

Those concerns belong to consuming applications, provider-specific packages, or later gateway samples.

## Documentation

Key documentation pages:

- [Why AsiBackbone?](docs/articles/why-asi-backbone.md)
- [Getting Started](docs/articles/getting-started.md)
- [Core Domain Language and Alpha Boundary](docs/articles/core-domain-language.md)
- [Policy Evaluator Pipeline](docs/articles/policy-evaluator-pipeline.md)
- [Equations and Toy Models](docs/articles/equations-and-toy-models.md)
- [Alpha Package Boundary](docs/articles/alpha-package-boundary.md)
- [EF Core Integration Boundary](docs/articles/ef-core-integration-boundary.md)
- [EF Core Host Ownership and Migration Guidance](docs/articles/ef-core-host-ownership-and-migrations.md)
- [ASP.NET Core Integration Boundary](docs/articles/aspnetcore-integration-boundary.md)
- [Plain ASP.NET Core Host Sample](docs/articles/plain-aspnetcore-host-sample.md)
- [NetCoreApplicationTemplate Host Validation](docs/articles/netcoreapplicationtemplate-host-validation.md)

## Core Domain Language

Important terms:

| Term | Meaning |
| --- | --- |
| Accountable Systems Infrastructure | The project meaning of ASI: infrastructure that makes consequential software decisions policy-shaped, auditable, acknowledgment-aware, and capability-bounded. |
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

### CDCavell.AsiBackbone.Core

Current package.

Defines framework-neutral domain abstractions and primitives.

### CDCavell.AsiBackbone.Storage.InMemory

Current package.

Provides non-durable in-memory storage helpers for local validation, samples, and tests. It should not be used as durable production storage.

### CDCavell.AsiBackbone.EntityFrameworkCore

Current package.

Provides EF Core model contributions and audit ledger persistence integration. The host application owns the `DbContext`, provider, connection string, migrations, deployment, and schema lifecycle.

### CDCavell.AsiBackbone.AspNetCore

Current package.

Provides ASP.NET Core host integration seams for service registration, policy evaluation, and host-boundary validation.

### CDCavell.AsiBackbone.Signing

Planned future package for signing and verification support around decision receipts, policy hashes, capability tokens, and audit records.

### CDCavell.AsiBackbone.Samples

Planned future samples or validation hosts.

Samples may include a console or worker host, a plain ASP.NET Core host, and a NetCoreApplicationTemplate-based host.

The plain ASP.NET Core host sample is the canonical in-repository validation baseline. NetCoreApplicationTemplate is documented separately as an optional external local validation host for developers who want to test AsiBackbone against a fuller enterprise-style application baseline.

### CDCavell.AsiBackbone.Robotics

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

For validation guidance, see [NetCoreApplicationTemplate Host Validation](docs/articles/netcoreapplicationtemplate-host-validation.md).

## Alignment Boundary

In this repository, ASI means **Accountable Systems Infrastructure**. AsiBackbone documentation may reference the broader Eden/Backbone framework as conceptual inspiration, but implementation claims should remain grounded in practical software governance.

Safe language:

- AsiBackbone stands for Accountable Systems Infrastructure Backbone.
- AsiBackbone implements governance-oriented software primitives.
- AsiBackbone helps structure consequential decision flow through constraints, acknowledgment, audit, and capability boundaries.
- AsiBackbone can surround intelligent or decision-producing systems with accountable execution infrastructure.
- AsiBackbone is inspired by broader Eden/Backbone governance concepts without claiming to implement artificial superintelligence.

Avoid language such as:

- AsiBackbone implements artificial superintelligence.
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
- Treat AsiBackbone as Accountable Systems Infrastructure: governance infrastructure, not an intelligence engine.
