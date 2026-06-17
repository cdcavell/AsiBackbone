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

AI systems, agents, services, and applications may produce recommendations, requests, or actions. AsiBackbone provides the infrastructure around those decisions: policy evaluation, acknowledgment workflows, audit residue, capability boundaries, durable outbox persistence, optional governance emission providers, and signing-ready or provider-signed accountability artifacts.

In this software project, **ASI** means **Accountable Systems Infrastructure**.

> AI may provide the intellect. AsiBackbone provides the accountable spine.

## What is AsiBackbone?

AsiBackbone helps a host application evaluate intent before execution, apply policy constraints, require acknowledgment when needed, preserve audit records, and optionally scope follow-on execution through short-lived capability tokens.

It is designed for systems where consequential actions need to be governed, explained, audited, constrained, preserved locally, and optionally projected into observability or governance systems before the host application executes or reviews them.

AsiBackbone should be understood as **governance infrastructure**, not an intelligence engine.

> [!IMPORTANT]
> These packages do **not** implement artificial superintelligence, host AI models, train AI models, control robots, certify compliance, or prove the Eden/Backbone framework. They provide framework-neutral building blocks and host integration seams for governing consequential actions in software systems.

## Package family

Stable `1.1.0` package family includes the original `1.0.0` Core, in-memory storage, EF Core, and ASP.NET Core package boundary plus analyzer, OpenTelemetry, and signing provider packages for the compatible `1.x` line.

| Package | Role |
| :--- | :--- |
| `CDCavell.AsiBackbone.Core` | Framework-neutral governance primitives, decisions, constraints, acknowledgments, audit residue, lifecycle events, capability-token abstractions, durable outbox contracts, provider-neutral emission contracts, DLP/classification policy primitives, signing-ready metadata, canonical hashing/signing seams, and verification-policy primitives. |
| `CDCavell.AsiBackbone.Storage.InMemory` | Non-durable in-memory storage helpers for tests, samples, local validation, lifecycle events, and outbox proof paths. |
| `CDCavell.AsiBackbone.EntityFrameworkCore` | EF Core model configuration and host-owned persistence for audit ledger, acknowledgments, lifecycle events, and governance outbox records. |
| `CDCavell.AsiBackbone.AspNetCore` | ASP.NET Core host adapters for actor context, request correlation, audit enrichment, HTTP result mapping, acknowledgment challenge flows, endpoint governance, and hosted outbox drain integration. |
| `CDCavell.AsiBackbone.Analyzers` | Roslyn analyzer safety rails for governance persistence and continuation flows. |
| `CDCavell.AsiBackbone.OpenTelemetry` | OpenTelemetry-friendly governance emission provider that projects provider-neutral envelopes into .NET diagnostics. |
| `CDCavell.AsiBackbone.Signing.LocalDevelopment` | Local-development signing and verification provider for tests, samples, and wiring proof paths only. Not for production key custody. |
| `CDCavell.AsiBackbone.Signing.ManagedKey` | Managed-key signing adapter boundary. The host supplies the actual managed-key client, credentials, key operations, and operational policy. |

Package-specific READMEs and release notes define which surfaces are stable, optional, local-only, or future-facing. Future Event Hubs, Purview, Azure-specific, gateway, robotics, immutable-storage, or additional provider packages are not part of the stable contract unless separately reviewed and released.

## What problem does this solve?

Many systems eventually need more than ordinary authorization and application logging.

A consequential action may need to answer:

- Is this action allowed right now?
- Which constraints, policies, and reason codes shaped the decision?
- Does this request require human acknowledgment before execution?
- Which policy version and policy hash were active?
- Can execution be scoped through a short-lived capability token?
- Was the decision preserved in durable local storage before downstream provider emission?
- Can signing or verification metadata be attached without forcing Core to own key management?
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
  -> Optionally sign or verify governance artifacts when a provider is configured
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
| OpenTelemetry projection | Governance envelopes can be projected into host-configured OpenTelemetry pipelines without binding Core to a cloud provider when the optional provider package is adopted. |
| Signing provider boundary | Signing and verification can be wired through provider packages while Core remains provider-neutral. |
| Host-owned integration | Applications keep ownership of the web host, persistence lifecycle, migrations, deployment, exporter configuration, key management, and execution behavior. |
| Framework-neutral core | Core primitives can be used without requiring ASP.NET Core, EF Core, NetCoreApplicationTemplate, AI packages, robotics dependencies, observability providers, or signing providers. |

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
- Hosts that need signing-ready or provider-signed governance artifacts while retaining control of keys and verification policy.

## What does it not do?

AsiBackbone does not:

- Replace normal authentication or authorization.
- Guarantee compliance with any law, regulation, audit framework, or security standard.
- Host, train, run, or orchestrate AI models.
- Implement artificial superintelligence.
- Execute tools, APIs, infrastructure changes, or robot commands by itself.
- Own the consuming application's database, migrations, deployment, observability backend, exporter configuration, key-management boundary, or operational policy.
- Provide production tamper-evidence, immutability, or non-repudiation by default.
- Replace legal review, AI safety governance, organizational accountability, operational security, DLP review, or key-management controls.

The host application remains responsible for execution behavior and operational controls.

## Current status

Stable `1.1.0` package family prepared for release.

The repository includes the Core foundation, in-memory validation storage, EF Core host-owned persistence, ASP.NET Core integration, analyzers, OpenTelemetry provider implementation, local-development signing provider, managed-key signing adapter boundary, samples, release validation, and host-validation documentation.

## Package roles

### CDCavell.AsiBackbone.Core

Defines framework-neutral domain abstractions and primitives, including provider-neutral governance emission, durable outbox, DLP/classification policy, signing-ready metadata seams, canonical hashing/signing seams, and verification-policy primitives.

### CDCavell.AsiBackbone.Storage.InMemory

Provides non-durable in-memory storage helpers for local validation, samples, and tests. It should not be used as durable production storage.

### CDCavell.AsiBackbone.EntityFrameworkCore

Provides EF Core model contributions and audit ledger persistence integration. The host application owns the `DbContext`, provider, connection string, migrations, deployment, retention, and schema lifecycle.

### CDCavell.AsiBackbone.AspNetCore

Provides ASP.NET Core host integration seams for service registration, request correlation, audit enrichment, HTTP result mapping, acknowledgment challenge helpers, endpoint governance, and hosted outbox drain integration.

### CDCavell.AsiBackbone.Analyzers

Provides Roslyn analyzer safety rails for governance persistence and continuation flows. Analyzer guidance is advisory and build-time; it is not runtime enforcement.

### CDCavell.AsiBackbone.OpenTelemetry

Provides the optional OpenTelemetry governance emission provider package. It projects provider-neutral governance envelopes into `ActivitySource` activity events, stable `asibackbone.*` tags, and `Meter` metrics. Exporters such as Azure Monitor remain host-configured.

### CDCavell.AsiBackbone.Signing.LocalDevelopment

Provides local-development RSA signing and verification for tests, samples, and wiring proof paths. It is not production signing, protected key custody, legal non-repudiation, or tamper-evidence.

### CDCavell.AsiBackbone.Signing.ManagedKey

Provides a managed-key signing adapter boundary. The package does not include live Azure Key Vault, Managed HSM, cloud KMS, HSM, or certificate-store implementation by default. The host supplies the managed-key client and operational controls.

## Documentation

Key documentation pages:

- [Why AsiBackbone?](docs/articles/why-asi-backbone.md)
- [ASI Backbone Concept Synopsis](docs/articles/asi-backbone-concept.md)
- [Dynamic Liability Handshake](docs/articles/dynamic-liability-handshake.md)
- [Gateway and Regional Policy Flow](docs/articles/gateway-and-regional-policy-flow.md)
- [Getting Started](docs/articles/getting-started.md)
- [First 15 Minutes: Standard API Gating](docs/articles/quickstart-api-gating.md)
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
- [Signing Provider Package Boundary](docs/articles/signing-provider-package-boundary.md)
- [Managed-Key Signing Provider](docs/articles/managed-key-signing-provider.md)
- [Signed Audit and Outbox Records](docs/articles/signed-audit-and-outbox-records.md)
- [Verification Policy and Result Handling](docs/articles/verification-policy-and-result-handling.md)
- [Capability Grant Hardening](docs/articles/capability-grant-hardening.md)
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
- AsiBackbone helps structure consequential decision flow through constraints, acknowledgment, audit, capability boundaries, durable outbox persistence, provider emission, and signing-provider boundaries.
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
- Treat provider signing as one part of an operational trust model, not as tamper-evidence by itself.
- Treat NetCoreApplicationTemplate as a preferred host, not a parent framework.
- Treat AsiBackbone as Accountable Systems Infrastructure: governance infrastructure, not an intelligence engine.
