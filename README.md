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

AI systems, agents, services, and applications may produce recommendations, requests, or actions. AsiBackbone provides the infrastructure around those decisions: policy evaluation, acknowledgment workflows, audit residue, capability boundaries, durable outbox persistence, and optional governance emission providers.

In this software project, **ASI** means **Accountable Systems Infrastructure**.

> AI may provide the intellect. AsiBackbone provides the accountable spine.

## What is AsiBackbone?

AsiBackbone helps a host application evaluate intent before execution, apply policy constraints, require acknowledgment when needed, preserve audit records, and optionally scope follow-on execution through short-lived capability tokens.

It is designed for systems where consequential actions need to be governed, explained, audited, constrained, preserved locally, and optionally projected into observability or governance systems before the host application executes or reviews them.

AsiBackbone should be understood as **governance infrastructure**, not an intelligence engine.

> [!IMPORTANT]
> These packages do **not** implement artificial superintelligence, host AI models, train AI models, control robots, certify compliance, or prove the Eden/Backbone framework. They provide framework-neutral building blocks and host integration seams for governing consequential actions in software systems.

## Stable package family

The `1.1.0` stable release covers the implemented package family below.

| Package | NuGet | Downloads |
| :--- | :--- | :--- |
| Core | [![NuGet Core](https://img.shields.io/nuget/v/CDCavell.AsiBackbone.Core?label=Release)](https://www.nuget.org/packages/CDCavell.AsiBackbone.Core) | [![NuGet Core Downloads](https://img.shields.io/nuget/dt/CDCavell.AsiBackbone.Core?label=Downloads)](https://www.nuget.org/packages/CDCavell.AsiBackbone.Core) |
| AspNetCore | [![NuGet AspNetCore](https://img.shields.io/nuget/v/CDCavell.AsiBackbone.AspNetCore?label=Release)](https://www.nuget.org/packages/CDCavell.AsiBackbone.AspNetCore) | [![NuGet AspNetCore Downloads](https://img.shields.io/nuget/dt/CDCavell.AsiBackbone.AspNetCore?label=Downloads)](https://www.nuget.org/packages/CDCavell.AsiBackbone.AspNetCore) |
| Storage.InMemory | [![NuGet Storage.InMemory](https://img.shields.io/nuget/v/CDCavell.AsiBackbone.Storage.InMemory?label=Release)](https://www.nuget.org/packages/CDCavell.AsiBackbone.Storage.InMemory) | [![NuGet Storage.InMemory Downloads](https://img.shields.io/nuget/dt/CDCavell.AsiBackbone.Storage.InMemory?label=Downloads)](https://www.nuget.org/packages/CDCavell.AsiBackbone.Storage.InMemory) |
| EntityFrameworkCore | [![NuGet EntityFrameworkCore](https://img.shields.io/nuget/v/CDCavell.AsiBackbone.EntityFrameworkCore?label=Release)](https://www.nuget.org/packages/CDCavell.AsiBackbone.EntityFrameworkCore) | [![NuGet EntityFrameworkCore Downloads](https://img.shields.io/nuget/dt/CDCavell.AsiBackbone.EntityFrameworkCore?label=Downloads)](https://www.nuget.org/packages/CDCavell.AsiBackbone.EntityFrameworkCore) |
| OpenTelemetry | [![NuGet OpenTelemetry](https://img.shields.io/nuget/v/CDCavell.AsiBackbone.OpenTelemetry?label=Release)](https://www.nuget.org/packages/CDCavell.AsiBackbone.OpenTelemetry) | [![NuGet OpenTelemetry Downloads](https://img.shields.io/nuget/dt/CDCavell.AsiBackbone.OpenTelemetry?label=Downloads)](https://www.nuget.org/packages/CDCavell.AsiBackbone.OpenTelemetry) |

Future Event Hubs, Purview, Azure-specific, signing-provider, gateway, robotics, or immutable-storage packages are not part of the `1.1.0` stable contract unless separately released as stable packages.

## What problem does this solve?

Many systems eventually need more than ordinary authorization and application logging.

A consequential action may need to answer:

- Is this action allowed right now?
- Which constraints, policies, and reason codes shaped the decision?
- Does this request require human acknowledgment before execution?
- Which policy version and policy hash were active?
- Can execution be scoped through a short-lived capability token?
- Was the decision preserved in durable local storage before downstream provider emission?
- Can a reviewer later understand why the system allowed, warned, denied, deferred, required acknowledgment, recommended escalation, or emitted a governance event?

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
  -> Preserve local audit/outbox record when provider emission is used
  -> Optionally emit a minimized governance envelope to a downstream provider
  -> Host application decides whether and how to execute
```

Core benefits include:

| Benefit | What it means |
| --- | --- |
| Policy-driven decision gating | Proposed actions can be evaluated before the host executes them. |
| Explicit decision outcomes | Decisions can be allowed, warned, denied, deferred, acknowledgment-required, or escalation-recommended. |
| Human acknowledgment workflow | Consequential actions can pause until an actor acknowledges responsibility, risk, or intent. |
| Audit residue | Decisions can preserve reason codes, policy version, policy hash, correlation ID, timestamp, and metadata. |
| Durable outbox baseline | Governance events can be preserved locally before optional downstream provider emission. |
| Capability-scoped execution | Follow-on execution can be represented as a short-lived, scoped permission grant. |
| OpenTelemetry projection | Governance envelopes can be projected into host-configured OpenTelemetry pipelines without binding Core to a cloud provider. |
| Host-owned integration | Applications keep ownership of the web host, persistence lifecycle, migrations, deployment, exporter configuration, and execution behavior. |
| Framework-neutral core | Core primitives can be used without requiring ASP.NET Core, EF Core, NetCoreApplicationTemplate, AI packages, robotics dependencies, or observability providers. |

See [Why AsiBackbone?](docs/articles/why-asi-backbone.md) for a fuller benefits overview.

## Who is this for?

AsiBackbone may be useful for:

- Enterprise .NET applications with consequential administrative actions.
- AI-agent gateways that need policy checks before tool or API execution.
- Human-in-the-loop workflows where approval or acknowledgment matters.
- Government, public-sector, education, healthcare, finance, legal, or other regulated systems that need stronger auditability.
- Platform engineering workflows that need clear allow, deny, defer, acknowledgment, or escalation decisions before external execution.
- Applications that need capability-scoped grants instead of broad, long-lived authority.
- Hosts that need durable local governance records before emitting observability or governance events downstream.

## What does it not do?

AsiBackbone does not:

- Replace normal authentication or authorization.
- Guarantee compliance with any law, regulation, audit framework, or security standard.
- Host, train, run, or orchestrate AI models.
- Implement artificial superintelligence.
- Execute tools, APIs, infrastructure changes, or robot commands by itself.
- Own the consuming application's database, migrations, deployment, observability backend, exporter configuration, or operational policy.
- Provide production tamper-evidence, immutability, or non-repudiation by default.
- Replace legal review, AI safety governance, organizational accountability, operational security, DLP review, or key-management controls.

The host application remains responsible for execution behavior and operational controls.

## Current status

Stable `1.1.0` package family.

The repository includes the Core foundation, in-memory validation storage, EF Core host-owned audit/outbox persistence, ASP.NET Core integration, hosted governance outbox drain support, OpenTelemetry governance emission provider, samples, release validation, and host-validation documentation. Planned follow-up milestones may include concrete signing providers, Event Hubs or Purview provider packages, gateway integrations, robotics examples, and future provider packages after their own package-boundary reviews.

## Package roles

### CDCavell.AsiBackbone.Core

Defines framework-neutral domain abstractions and primitives, including provider-neutral governance emission, durable outbox, DLP/classification policy, and signing-ready metadata seams.

### CDCavell.AsiBackbone.Storage.InMemory

Provides non-durable in-memory storage helpers for local validation, samples, tests, lifecycle stores, and outbox proof paths. It should not be used as durable production storage.

### CDCavell.AsiBackbone.EntityFrameworkCore

Provides EF Core model contributions and audit ledger, audit residue lifecycle, and durable governance outbox persistence integration. The host application owns the `DbContext`, provider, connection string, migrations, deployment, retention, and schema lifecycle.

### CDCavell.AsiBackbone.AspNetCore

Provides ASP.NET Core host integration seams for service registration, request correlation, audit enrichment, HTTP result mapping, acknowledgment challenge helpers, and hosted outbox drain integration.

### CDCavell.AsiBackbone.OpenTelemetry

Provides the concrete OpenTelemetry governance emission provider. It projects provider-neutral governance envelopes into `ActivitySource` activity events, stable `asibackbone.*` tags, and `Meter` metrics. Exporters such as Azure Monitor remain host-configured.

## Documentation

Key documentation pages:

- [Why AsiBackbone?](docs/articles/why-asi-backbone.md)
- [Getting Started](docs/articles/getting-started.md)
- [1.0.0 Quickstart](docs/articles/quickstart-100.md)
- [1.0.0 Release Notes](docs/articles/release-notes-100.md)
- [1.1.0 Release Notes](docs/articles/release-notes-110.md)
- [Upgrade Guide: 1.0.0 to 1.1.0](docs/articles/upgrade-100-to-110.md)
- [Core Domain Language](docs/articles/core-domain-language.md)
- [Policy Evaluator Pipeline](docs/articles/policy-evaluator-pipeline.md)
- [Equations and Toy Models](docs/articles/equations-and-toy-models.md)
- [Observability and Governance Emission Architecture](docs/articles/observability-and-governance-emission-architecture.md)
- [Governance Emission Contract](docs/articles/governance-emission-contract.md)
- [Durable Audit and Outbox Persistence](docs/articles/durable-audit-outbox-persistence.md)
- [Hosted Governance Outbox Drain](docs/articles/hosted-governance-outbox-drain.md)
- [OpenTelemetry Governance Emission Provider](docs/articles/opentelemetry-governance-emission-provider.md)
- [DLP and Classification Failure Policy](docs/articles/dlp-classification-failure-policy.md)
- [Signing-Ready Receipts and Key Handling](docs/articles/signing-ready-receipts-and-key-handling.md)
- [Event Hubs Governance Emission Provider Design](docs/articles/event-hubs-governance-emission-provider-design.md)
- [Purview Governance and Lineage Enrichment Strategy](docs/articles/purview-governance-lineage-enrichment-strategy.md)
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
- AsiBackbone helps structure consequential decision flow through constraints, acknowledgment, audit, capability boundaries, durable outbox persistence, and optional provider emission.
- AsiBackbone can surround intelligent or decision-producing systems with accountable execution infrastructure.
- AsiBackbone is inspired by broader Eden/Backbone governance concepts without claiming to implement artificial superintelligence.

Avoid language such as:

- AsiBackbone implements artificial superintelligence.
- AsiBackbone proves the Eden Hypothesis.
- AsiBackbone is an AI model.
- AsiBackbone is tamper-evident or immutable by default.
- AsiBackbone replaces AI safety governance, legal review, operational security, DLP review, or organizational accountability.

## Design principles

- Keep Core small.
- Keep Core dependency-light.
- Avoid hidden host assumptions.
- Prefer explicit integration over magic.
- Let the host own infrastructure.
- Let integration packages own persistence, web integration, signing, storage, samples, observability, provider-specific emission, and external execution concerns.
- Keep package boundaries clear before adding behavior.
- Treat durable local/outbox persistence as the reliability baseline before external emission.
- Treat NetCoreApplicationTemplate as a preferred host, not a parent framework.
- Treat AsiBackbone as Accountable Systems Infrastructure: governance infrastructure, not an intelligence engine.
