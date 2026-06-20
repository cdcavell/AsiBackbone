# Documentation Articles

This section maps the AsiBackbone documentation set for the stable `1.1.x` Accountable Systems Infrastructure package family.

> [!IMPORTANT]
> In this software project, **ASI** means **Accountable Systems Infrastructure**. AsiBackbone is governance infrastructure for accountable software decision flow, not an artificial superintelligence implementation.

## Search and navigation

Use the header search box for package names, API concepts, and article titles. Search is enabled for the published DocFX site; if a newly merged page is missing from results, wait for the documentation publish workflow to finish and refresh the browser cache. Long pages use the left navigation tree and the right **In this article** rail for local heading navigation. Source files live under `docs/` in the repository, and the site header includes a Repository link for source review or edits.

## Current stable package posture

Stable `1.1.x` is the current compatible package line. `1.1.1` is a patch release on the `1.1.0` API surface.

Released stable package surfaces include Core, DependencyInjection, Storage.InMemory, EntityFrameworkCore, AspNetCore, Testing, Analyzers, OpenTelemetry, Signing.LocalDevelopment, and Signing.ManagedKey. OpenTelemetry is the concrete released governance-emission provider. Event Hubs, Purview, Azure-specific SDK adapters, robotics, immutable-storage, and additional provider packages remain design-only, strategy-only, host-owned, or future-provider work unless a later stable release explicitly ships them.

## Start here / implementation-first usage

* [Implementation-First Adoption Path](implementation-first-adoption.md)
* [First 15 Minutes: Standard API Gating](quickstart-api-gating.md)
* [Reference Deployment: Plain ASP.NET Core Host Evidence](reference-deployment.md)
* [Terminology Map](terminology-map.md)
* [Progressive Adoption Ladder](progressive-adoption.md)
* [Getting Started](getting-started.md)
* [Core Governance Flow Diagrams](core-governance-flow-diagrams.md)
* [AddAsiBackbone Builder Facade](add-asibackbone-builder-facade.md)
* [Why AsiBackbone?](why-asi-backbone.md)
* [1.1.x Release Notes](release-notes-110.md)
* [Upgrade Guide: 1.0.0 to 1.1.0](upgrade-100-to-110.md)

## Core engineering concepts and domain language

* [Policy Evaluator Pipeline](policy-evaluator-pipeline.md)
* [Host-Owned Execution Enforcement](host-owned-execution-enforcement.md)
* [ASP.NET Core Endpoint Governance](aspnetcore-endpoint-governance.md)
* [Dynamic Liability Handshake](dynamic-liability-handshake.md)
* [Core Domain Language](core-domain-language.md)
* [Glossary](glossary.md)

## Optional conceptual background

These pages remain available for readers who want the broader framing. They are not required before using the packages.

* [Intent to Execution: An Accountability Pattern](intent-to-execution-pattern.md)
* [ASI Backbone Concept Synopsis](asi-backbone-concept.md)
* [Gateway and Regional Policy Flow](gateway-and-regional-policy-flow.md)
* [Equations and Toy Models](equations-and-toy-models.md)

## Adoption and use-case guidance

* [Governance Tool Comparisons](governance-tool-comparisons.md)
* [Adoption and Target Use Cases](use-cases.md)
* [Enterprise Adoption Personas](enterprise-adoption-personas.md)
* [Government and Regulated Systems](government-and-regulated-systems.md)

## Package integration guides

* [Reference Deployment: Plain ASP.NET Core Host Evidence](reference-deployment.md)
* [1.0.0 Quickstart](quickstart-100.md)
* [EF Core Integration Boundary](ef-core-integration-boundary.md)
* [EF Core Host Ownership and Migration Guidance](ef-core-host-ownership-and-migrations.md)
* [ASP.NET Core Integration Boundary](aspnetcore-integration-boundary.md)
* [ASP.NET Core Endpoint Governance](aspnetcore-endpoint-governance.md)
* [Testing Harness](testing-harness.md)
* [Plain ASP.NET Core Host Sample](plain-aspnetcore-host-sample.md)
* [NetCoreApplicationTemplate Host Validation](netcoreapplicationtemplate-host-validation.md)
* [Schema Versioning](schema-versioning.md)
* [API Compatibility and SemVer](api-compatibility-and-semver.md)
* [Privacy and Signing Boundaries](privacy-and-signing-boundaries.md)

## 1.1.x observability, outbox, signing, and governance emission

These pages cover the additive `1.1.x` governance-emission and durability surface introduced in `1.1.0` and maintained by compatible patches such as `1.1.1`.

* [Observability and Governance Emission Architecture](observability-and-governance-emission-architecture.md)
* [Governance Emission Contract](governance-emission-contract.md)
* [Durable Audit and Outbox Persistence](durable-audit-outbox-persistence.md)
* [Hosted Governance Outbox Drain](hosted-governance-outbox-drain.md)
* [Outbox Multi-Worker Concurrency](outbox-multi-worker-concurrency.md)
* [Outbox Drain Reliability and Alerting](outbox-drain-reliability-and-alerting.md)
* [Safe Audit and Telemetry Data](safe-audit-telemetry-data.md)
* [Audit Residue Observability Schema](audit-residue-observability-schema.md)
* [DLP and Classification Failure Policy](dlp-classification-failure-policy.md)
* [DLP and Classification Scanner Integration](dlp-classification-scanner-integration.md)

## Released provider package documentation

* [Roslyn Analyzers](roslyn-analyzers.md)
* [Released: OpenTelemetry Governance Emission Provider](opentelemetry-governance-emission-provider.md)
* [Signing Provider Package Boundary](signing-provider-package-boundary.md)
* [Managed-Key Signing Provider](managed-key-signing-provider.md)

## Security and cryptographic boundaries

These pages describe signing, verification, capability, and cryptographic-hardening posture. They do not imply production tamper-evidence unless a concrete signing, storage, verification, and key-management path is deployed by the host.

* [Production Wording and Stable Signing Boundaries](production-wording-and-alpha-limitations.md)
* [Signing-Ready Receipts and Key Handling](signing-ready-receipts-and-key-handling.md)
* [Signed Audit and Outbox Records](signed-audit-and-outbox-records.md)
* [Verification Policy and Result Handling](verification-policy-and-result-handling.md)
* [Key Rotation and Retired-Key Verification](key-rotation-and-retired-key-verification.md)
* [Capability Grant Hardening](capability-grant-hardening.md)
* [Audit Integrity Chain Model](audit-integrity-chain-model.md)
* [Cryptographic Security Posture and Production Guidance](cryptographic-security-posture.md)
* [Cryptographic Security Hardening Roadmap](cryptographic-security-hardening-roadmap.md)

## Design-only and future provider strategy

These pages remain available as strategy/design material. They are not released provider packages unless a future release explicitly says so.

* [Design-Only: Event Hubs Governance Emission Provider](event-hubs-governance-emission-provider-design.md)
* [Strategy-Only: Purview Governance and Lineage Enrichment](purview-governance-lineage-enrichment-strategy.md)

## Advanced scenarios

Scenario pages describe applied patterns. They remain optional and do not imply current package implementation beyond the documented seams.

* [AI Agent Gateway](scenarios/ai-agent-gateway.md)
* [Human Approval Before AI Tool Execution](scenarios/human-approval-before-ai-tool-execution.md)
* [High-Risk Administrative Action](scenarios/high-risk-administrative-action.md)
* [Sensitive Data Access Request](scenarios/sensitive-data-access-request.md)
* [Deployment or Infrastructure Change Gate](scenarios/deployment-or-infrastructure-change-gate.md)
* [Robotics Operational Gateway](scenarios/robotics-operational-gateway.md)

## Quality and release process

* [Quality Reports](../quality/index.md)
* [Release Validation](release-validation.md)
* [Historical 1.1.0 Release Readiness Record](release-readiness-checklist.md)
* [API Baseline and Boundary Checks](api-baseline-and-boundary-checks.md)
* [Developer Checklist](developer-checklist.md)
* [1.0.0 Release Notes](release-notes-100.md)

## Historical design records

Historical pages are retained for traceability and are separated from current stable usage.

* [Historical Stable API Review](stable-api-review.md)
* [Historical Alpha Package Boundary](alpha-package-boundary.md)
* [Historical Core Alpha Readiness Review](core-alpha-readiness-review.md)

## Read next

- [Implementation-First Adoption Path](implementation-first-adoption.md)
- [First 15 Minutes: Standard API Gating](quickstart-api-gating.md)
- [Reference Deployment: Plain ASP.NET Core Host Evidence](reference-deployment.md)
