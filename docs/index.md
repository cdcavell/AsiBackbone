# ASI Backbone Documentation

Welcome to the AsiBackbone documentation site.

In this software project, **ASI** means **Accountable Systems Infrastructure**. AsiBackbone is a .NET governance and policy-control package family inspired by broader Eden/Backbone governance concepts, but implemented as practical software infrastructure. The project is a **governance spine**, not an intelligence engine.

> [!IMPORTANT]
> AsiBackbone does not implement artificial superintelligence, host AI models, control physical systems, certify compliance, or provide production tamper-evidence by itself. It provides framework-neutral building blocks and host integration seams for governing consequential actions in software systems.

## Start here

These pages are the best first stops for current stable package consumers.

* [Why AsiBackbone?](articles/why-asi-backbone.md)  
  High-level purpose, target audience, and adoption rationale.

* [Getting Started](articles/getting-started.md)  
  Project orientation, local build instructions, and stable package direction.

* [First 15 Minutes: Standard API Gating](articles/quickstart-api-gating.md)  
  Practical package-consumer walkthrough for gating one ASP.NET Core endpoint.

* [Terminology Map](articles/terminology-map.md)  
  Quick vocabulary bridge for .NET and application-architecture readers.

* [1.1.0 Release Notes](articles/release-notes-110.md)  
  Current stable package-family boundary for the compatible `1.x` line.

* [Upgrade Guide: 1.0.0 to 1.1.0](articles/upgrade-100-to-110.md)  
  Incremental adoption guidance for existing `1.0.0` consumers.

* [Documentation Articles](articles/)  
  Categorized article index separating current usage, package integration, provider docs, design-only strategy, scenarios, release process, and historical records.

## Current stable package family

Stable `1.1.0` covers these packages:

```text
CDCavell.AsiBackbone.Core
CDCavell.AsiBackbone.Storage.InMemory
CDCavell.AsiBackbone.EntityFrameworkCore
CDCavell.AsiBackbone.AspNetCore
CDCavell.AsiBackbone.Analyzers
CDCavell.AsiBackbone.OpenTelemetry
CDCavell.AsiBackbone.Signing.LocalDevelopment
CDCavell.AsiBackbone.Signing.ManagedKey
```

Package-specific READMEs and release notes define which surfaces are stable, optional, local-only, or future-facing. A design page being present in the documentation does not mean the corresponding provider package has shipped as stable.

## Major documentation areas

### Core concepts and domain language

Use these pages to understand the vocabulary and decision-flow model.

* [ASI Backbone Concept Synopsis](articles/asi-backbone-concept.md)
* [Core Domain Language](articles/core-domain-language.md)
* [Policy Evaluator Pipeline](articles/policy-evaluator-pipeline.md)
* [Host-Owned Execution Enforcement](articles/host-owned-execution-enforcement.md)
* [Dynamic Liability Handshake](articles/dynamic-liability-handshake.md)
* [Gateway and Regional Policy Flow](articles/gateway-and-regional-policy-flow.md)
* [Equations and Toy Models](articles/equations-and-toy-models.md)
* [Glossary](articles/glossary.md)

### Package integration guides

Use these pages when wiring AsiBackbone into a host application.

* [EF Core Integration Boundary](articles/ef-core-integration-boundary.md)
* [EF Core Host Ownership and Migration Guidance](articles/ef-core-host-ownership-and-migrations.md)
* [ASP.NET Core Integration Boundary](articles/aspnetcore-integration-boundary.md)
* [ASP.NET Core Endpoint Governance](articles/aspnetcore-endpoint-governance.md)
* [Plain ASP.NET Core Host Sample](articles/plain-aspnetcore-host-sample.md)
* [NetCoreApplicationTemplate Host Validation](articles/netcoreapplicationtemplate-host-validation.md)
* [Schema Versioning](articles/schema-versioning.md)
* [API Compatibility and SemVer](articles/api-compatibility-and-semver.md)

### Observability, outbox, signing, and governance emission

These pages cover the additive `1.1.0` durability and governance-emission surface.

* [Observability and Governance Emission Architecture](articles/observability-and-governance-emission-architecture.md)
* [Governance Emission Contract](articles/governance-emission-contract.md)
* [Durable Audit and Outbox Persistence](articles/durable-audit-outbox-persistence.md)
* [Hosted Governance Outbox Drain](articles/hosted-governance-outbox-drain.md)
* [Outbox Multi-Worker Concurrency](articles/outbox-multi-worker-concurrency.md)
* [Outbox Drain Reliability and Alerting](articles/outbox-drain-reliability-and-alerting.md)
* [Safe Audit and Telemetry Data](articles/safe-audit-telemetry-data.md)
* [Audit Residue Observability Schema](articles/audit-residue-observability-schema.md)
* [DLP and Classification Failure Policy](articles/dlp-classification-failure-policy.md)
* [DLP and Classification Scanner Integration](articles/dlp-classification-scanner-integration.md)

### Released provider package documentation

These pages document released provider or provider-adjacent packages.

* [Roslyn Analyzers](articles/roslyn-analyzers.md)
* [OpenTelemetry Governance Emission Provider](articles/opentelemetry-governance-emission-provider.md)
* [Signing Provider Package Boundary](articles/signing-provider-package-boundary.md)
* [Managed-Key Signing Provider](articles/managed-key-signing-provider.md)

### Design-only and future provider strategy

These pages are strategy/design material. They remain separate from released provider package documentation.

* [Event Hubs Governance Emission Provider Design](articles/event-hubs-governance-emission-provider-design.md)
* [Purview Governance and Lineage Enrichment Strategy](articles/purview-governance-lineage-enrichment-strategy.md)

### Security and cryptographic boundaries

These pages explain signing-ready artifacts, provider signing, verification, chain models, and safe wording. They do not imply production tamper-evidence unless the host deploys a concrete signing, storage, verification, and key-management path.

* [Production Wording and Stable Signing Boundaries](articles/production-wording-and-alpha-limitations.md)
* [Signing-Ready Receipts and Key Handling](articles/signing-ready-receipts-and-key-handling.md)
* [Signed Audit and Outbox Records](articles/signed-audit-and-outbox-records.md)
* [Verification Policy and Result Handling](articles/verification-policy-and-result-handling.md)
* [Key Rotation and Retired-Key Verification](articles/key-rotation-and-retired-key-verification.md)
* [Capability Grant Hardening](articles/capability-grant-hardening.md)
* [Audit Integrity Chain Model](articles/audit-integrity-chain-model.md)
* [Cryptographic Security Posture and Production Guidance](articles/cryptographic-security-posture.md)
* [Cryptographic Security Hardening Roadmap](articles/cryptographic-security-hardening-roadmap.md)

### Advanced scenarios

Scenario pages describe applied patterns. They do not imply current physical execution, robotics control, or model-hosting behavior beyond the documented package seams.

* [AI Agent Gateway](articles/scenarios/ai-agent-gateway.md)
* [Human Approval Before AI Tool Execution](articles/scenarios/human-approval-before-ai-tool-execution.md)
* [High-Risk Administrative Action](articles/scenarios/high-risk-administrative-action.md)
* [Sensitive Data Access Request](articles/scenarios/sensitive-data-access-request.md)
* [Deployment or Infrastructure Change Gate](articles/scenarios/deployment-or-infrastructure-change-gate.md)
* [Robotics Operational Gateway](articles/scenarios/robotics-operational-gateway.md)

### Quality and release process

These pages support maintainers and release validation.

* [Quality Reports](quality/)
* [Release Validation](articles/release-validation.md)
* [Historical 1.1.0 Release Readiness Record](articles/release-readiness-checklist.md)
* [API Baseline and Boundary Checks](articles/api-baseline-and-boundary-checks.md)
* [Developer Checklist](articles/developer-checklist.md)

### Historical design records

These records remain available for traceability, but they are separated from current stable usage.

* [Historical Stable API Review](articles/stable-api-review.md)
* [Historical Alpha Package Boundary](articles/alpha-package-boundary.md)
* [Historical Core Alpha Readiness Review](articles/core-alpha-readiness-review.md)

## API reference and repository

* [API Reference](api/CDCavell.AsiBackbone.Core.html)  
  Generated API documentation for public types.

* [Repository](https://github.com/cdcavell/AsiBackbone)  
  Source code, issues, pull requests, and release history.

## Design principle

AsiBackbone should make consequential software actions easier to govern, audit, constrain, acknowledge, preserve, emit, verify, and explain.

It should be understood as Accountable Systems Infrastructure, not an intelligence engine.
