# CDCavell.AsiBackbone

[![CI](https://github.com/cdcavell/AsiBackbone/actions/workflows/ci.yml/badge.svg)](https://github.com/cdcavell/AsiBackbone/actions/workflows/ci.yml)
[![Coverage Report](https://img.shields.io/badge/coverage%20gate-75%25-brightgreen)](https://cdcavell.github.io/AsiBackbone/coverage/index.html)
[![Documentation](https://github.com/cdcavell/AsiBackbone/actions/workflows/publish-docs.yml/badge.svg)](https://github.com/cdcavell/AsiBackbone/actions/workflows/publish-docs.yml)
[![Docs](https://img.shields.io/badge/docs-GitHub%20Pages-blue)](https://cdcavell.github.io/AsiBackbone/)
[![.NET](https://img.shields.io/badge/.NET-10.0-purple)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/license-MIT-blue)](LICENSE.txt)
[![GitHub Release](https://img.shields.io/github/v/release/cdcavell/AsiBackbone?sort=semver&display_name=tag&label=release)](https://github.com/cdcavell/AsiBackbone/releases)
[![Zenodo DOI](https://img.shields.io/badge/DOI-10.5281%2Fzenodo.20546032-blue)](https://doi.org/10.5281/zenodo.20546032)

## Accountable Systems Infrastructure for governed .NET decision flow.

AsiBackbone is a stable .NET package family for building an accountable governance spine around consequential software actions.

AI systems, agents, services, and applications may produce recommendations, requests, or actions. AsiBackbone provides the infrastructure around those decisions: policy evaluation, acknowledgment workflows, audit residue, capability boundaries, and host-controlled execution.

In this software project, **ASI** means **Accountable Systems Infrastructure**.

> AI may provide the intellect. AsiBackbone provides the accountable spine.

## What is AsiBackbone?

AsiBackbone helps a host application evaluate intent before execution, apply policy constraints, require acknowledgment when needed, preserve audit records, and optionally scope follow-on execution through short-lived capability tokens.

It is designed for systems where consequential actions need to be governed, explained, audited, and constrained before the host application executes them.

AsiBackbone should be understood as **governance infrastructure**, not an intelligence engine.

> [!IMPORTANT]
> These packages do **not** implement artificial superintelligence, host AI models, train AI models, control robots, or prove the Eden/Backbone framework. They provide framework-neutral building blocks and host integration seams for governing consequential actions in software systems.

## Stable package family

The `1.0.0` stable release covers the implemented package family below.

| Package | NuGet | Downloads |
| :--- | :--- | :--- |
| Core | [![NuGet Core](https://img.shields.io/nuget/v/CDCavell.AsiBackbone.Core?label=Release)](https://www.nuget.org/packages/CDCavell.AsiBackbone.Core) | [![NuGet Core Downloads](https://img.shields.io/nuget/dt/CDCavell.AsiBackbone.Core?label=Downloads)](https://www.nuget.org/packages/CDCavell.AsiBackbone.Core) |
| AspNetCore | [![NuGet AspNetCore](https://img.shields.io/nuget/v/CDCavell.AsiBackbone.AspNetCore?label=Release)](https://www.nuget.org/packages/CDCavell.AsiBackbone.AspNetCore) | [![NuGet AspNetCore Downloads](https://img.shields.io/nuget/dt/CDCavell.AsiBackbone.AspNetCore?label=Downloads)](https://www.nuget.org/packages/CDCavell.AsiBackbone.AspNetCore) |
| Storage.InMemory | [![NuGet Storage.InMemory](https://img.shields.io/nuget/v/CDCavell.AsiBackbone.Storage.InMemory?label=Release)](https://www.nuget.org/packages/CDCavell.AsiBackbone.Storage.InMemory) | [![NuGet Storage.InMemory Downloads](https://img.shields.io/nuget/dt/CDCavell.AsiBackbone.Storage.InMemory?label=Downloads)](https://www.nuget.org/packages/CDCavell.AsiBackbone.Storage.InMemory) |
| EntityFrameworkCore | [![NuGet EntityFrameworkCore](https://img.shields.io/nuget/v/CDCavell.AsiBackbone.EntityFrameworkCore?label=Release)](https://www.nuget.org/packages/CDCavell.AsiBackbone.EntityFrameworkCore) | [![NuGet EntityFrameworkCore Downloads](https://img.shields.io/nuget/dt/CDCavell.AsiBackbone.EntityFrameworkCore?label=Downloads)](https://www.nuget.org/packages/CDCavell.AsiBackbone.EntityFrameworkCore) |

Future signing, gateway, cloud observability, robotics, or provider-specific packages are not part of the `1.0.0` stable contract unless they are separately released as stable packages.

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

## Current status

Stable `1.0.0` package family.

The repository includes the Core foundation, in-memory validation storage, EF Core host-owned persistence, ASP.NET Core integration, samples, release validation, and host-validation documentation. Planned follow-up milestones include signing support, gateway integrations, robotics examples, and future provider packages after their own package-boundary reviews.

## Package roles

### CDCavell.AsiBackbone.Core

Defines framework-neutral domain abstractions and primitives.

### CDCavell.AsiBackbone.Storage.InMemory

Provides non-durable in-memory storage helpers for local validation, samples, and tests. It should not be used as durable production storage.

### CDCavell.AsiBackbone.EntityFrameworkCore

Provides EF Core model contributions and audit ledger persistence integration. The host application owns the `DbContext`, provider, connection string, migrations, deployment, and schema lifecycle.

### CDCavell.AsiBackbone.AspNetCore

Provides ASP.NET Core host integration seams for service registration, request correlation, audit enrichment, HTTP result mapping, and acknowledgment challenge helpers.

## Documentation

Key documentation pages:

- [Why AsiBackbone?](docs/articles/why-asi-backbone.md)
- [Getting Started](docs/articles/getting-started.md)
- [1.0.0 Quickstart](docs/articles/quickstart-100.md)
- [1.0.0 Release Notes](docs/articles/release-notes-100.md)
- [Core Domain Language](docs/articles/core-domain-language.md)
- [Policy Evaluator Pipeline](docs/articles/policy-evaluator-pipeline.md)
- [Equations and Toy Models](docs/articles/equations-and-toy-models.md)
- [Historical Alpha Package Boundary](docs/articles/alpha-package-boundary.md)
- [Schema Versioning](docs/articles/schema-versioning.md)
- [API Compatibility and SemVer](docs/articles/api-compatibility-and-semver.md)
- [EF Core Integration Boundary](docs/articles/ef-core-integration-boundary.md)
- [EF Core Host Ownership and Migration Guidance](docs/articles/ef-core-host-ownership-and-migrations.md)
- [ASP.NET Core Integration Boundary](docs/articles/aspnetcore-integration-boundary.md)
- [Plain ASP.NET Core Host Sample](docs/articles/plain-aspnetcore-host-sample.md)
- [NetCoreApplicationTemplate Host Validation](docs/articles/netcoreapplicationtemplate-host-validation.md)

## Relationship to NetCoreApplicationTemplate

NetCoreApplicationTemplate may be used as a preferred host baseline during development and validation, but AsiBackbone does not require it.

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

## Alignment boundary

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

## Design principles

- Keep Core small.
- Keep Core dependency-light.
- Avoid hidden host assumptions.
- Prefer explicit integration over magic.
- Let the host own infrastructure.
- Let integration packages own persistence, web integration, signing, storage, samples, and external execution concerns.
- Keep package boundaries clear before adding behavior.
- Treat NetCoreApplicationTemplate as a preferred host, not a parent framework.
- Treat AsiBackbone as Accountable Systems Infrastructure: governance infrastructure, not an intelligence engine.
