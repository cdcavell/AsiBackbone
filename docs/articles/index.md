# Documentation Articles

This section contains detailed documentation for AsiBackbone, the Accountable Systems Infrastructure package family for governed .NET decision flow.

Use this page as a map. The groups below separate current stable guidance from release-process records, historical design notes, released provider package documentation, design-only provider strategy, security boundaries, and advanced scenarios.

> [!IMPORTANT]
> In this software project, **ASI** means **Accountable Systems Infrastructure**. AsiBackbone is governance infrastructure for accountable software decision flow, not an artificial superintelligence implementation.

## Provider documentation boundary

`1.1.0` includes one concrete released governance emission provider package: [OpenTelemetry Governance Emission Provider](opentelemetry-governance-emission-provider.md). The analyzer and signing packages are also released package documentation, but OpenTelemetry is the only concrete released governance emission provider package in the `1.1.0` family.

Design-only or strategy-only provider pages remain useful planning references, but they do not mean a corresponding NuGet package shipped in `1.1.0`. See [1.1.0 Release Notes](release-notes-110.md#accepted-deferrals) for accepted deferrals covering Event Hubs, Purview, Azure-specific SDK adapters, and other future provider work.

## Start here / current stable usage

These pages are the best entry point for current package consumers.

* [Getting Started](getting-started.md)  
  Project orientation, local build instructions, and stable package direction.

* [First 15 Minutes: Standard API Gating](quickstart-api-gating.md)  
  A practical quickstart for gating one ASP.NET Core API endpoint with a simple constraint, allowed/denied paths, and local audit residue.

* [Terminology Map](terminology-map.md)  
  Translates AsiBackbone vocabulary into familiar .NET and application-architecture language, including first-use versus advanced concepts and a recommended reading path.

* [Why AsiBackbone?](why-asi-backbone.md)  
  Explains the project purpose, adoption rationale, and governance-spine framing.

* [1.1.0 Release Notes](release-notes-110.md)  
  Describes the released `1.1.0` observability, durable outbox, governance emission provider, OpenTelemetry, DLP/classification, analyzer, and signing-provider boundary.

* [Upgrade Guide: 1.0.0 to 1.1.0](upgrade-100-to-110.md)  
  Explains how existing `1.0.0` consumers can adopt `1.1.0` incrementally.

## Core concepts and domain language

These pages explain the framework-neutral language used across the package family.

* [ASI Backbone Concept Synopsis](asi-backbone-concept.md)  
  Finalized concept synopsis for Accountable Systems Infrastructure, controlled decision flow, active policy structure, and safe wording boundaries.

* [Core Domain Language](core-domain-language.md)  
  Defines the Core terminology for governance spine, constraints, collapse boundary, audit residue, actor context, decision results, operation results, acknowledgment, capability tokens, and gateway boundaries.

* [Policy Evaluator Pipeline](policy-evaluator-pipeline.md)  
  Documents the host-neutral policy and constraint evaluation flow.

* [Host-Owned Execution Enforcement](host-owned-execution-enforcement.md)  
  Documents the execution boundary where host applications must honor governance decisions before consequential paths execute side effects.

* [Dynamic Liability Handshake](dynamic-liability-handshake.md)  
  Documents the acknowledgment/responsibility-handshake workflow for consequential actions and its wording limits.

* [Gateway and Regional Policy Flow](gateway-and-regional-policy-flow.md)  
  Documents the no-direct-global-to-edge command pattern, regional/local policy evaluation, capability-scoped execution, and operational gateway validation.

* [Equations and Toy Models](equations-and-toy-models.md)  
  Maps the Eden/Backbone collapse notation into practical AsiBackbone software terms.

* [Glossary](glossary.md)  
  Provides a concise vocabulary reference.

## Adoption and use-case guidance

These pages help readers decide where AsiBackbone fits.

* [Governance Tool Comparisons](governance-tool-comparisons.md)  
  Compares Azure Policy, Open Policy Agent, Microsoft Agent Governance Toolkit, and AsiBackbone as complementary governance layers.

* [Adoption and Target Use Cases](use-cases.md)  
  Summarizes target use cases and adoption posture.

* [Enterprise Adoption Personas](enterprise-adoption-personas.md)  
  Maps AsiBackbone value to common enterprise roles.

* [Government and Regulated Systems](government-and-regulated-systems.md)  
  Frames use in government, public-sector, and regulated-system contexts without overstating compliance guarantees.

## Package integration guides

These pages describe implemented package boundaries and host-owned integration seams.

* [1.0.0 Quickstart](quickstart-100.md)  
  Package-consumer guidance for the first stable release line.

* [EF Core Integration Boundary](ef-core-integration-boundary.md)  
  Defines the implemented boundary for `CDCavell.AsiBackbone.EntityFrameworkCore`.

* [EF Core Host Ownership and Migration Guidance](ef-core-host-ownership-and-migrations.md)  
  Explains how host applications own the EF Core `DbContext`, provider, connection string, migrations, schema deployment, and operational lifecycle.

* [ASP.NET Core Integration Boundary](aspnetcore-integration-boundary.md)  
  Defines the implemented web-host adapter boundary for `CDCavell.AsiBackbone.AspNetCore`.

* [ASP.NET Core Endpoint Governance](aspnetcore-endpoint-governance.md)  
  Documents endpoint metadata, middleware behavior, failure behavior, and host-owned enforcement boundaries.

* [Plain ASP.NET Core Host Sample](plain-aspnetcore-host-sample.md)  
  Shows package use in a plain ASP.NET Core host without requiring NetCoreApplicationTemplate.

* [NetCoreApplicationTemplate Host Validation](netcoreapplicationtemplate-host-validation.md)  
  Documents validation with the preferred host baseline while preserving package independence.

* [Schema Versioning](schema-versioning.md)  
  Documents stable artifact schema and durable-artifact versioning guidance.

* [API Compatibility and SemVer](api-compatibility-and-semver.md)  
  Documents the stable package contract and compatibility expectations.

* [Privacy and Signing Boundaries](privacy-and-signing-boundaries.md)  
  Defines privacy and signing boundaries that package consumers must preserve.

## 1.1.0 observability, outbox, signing, and governance emission

These pages cover the additive `1.1.0` governance-emission and durability surface.

* [Observability and Governance Emission Architecture](observability-and-governance-emission-architecture.md)  
  Documents the `1.1.0` observability, outbox, and governance emission architecture direction, including the released OpenTelemetry provider and design-only future provider paths.

* [Governance Emission Contract](governance-emission-contract.md)  
  Defines the provider-neutral emission contract between Core governance artifacts, durable local audit/outbox persistence, and optional downstream providers.

* [Durable Audit and Outbox Persistence](durable-audit-outbox-persistence.md)  
  Documents the provider-neutral local persistence seam for audit residue lifecycle events and governance emission outbox entries.

* [Hosted Governance Outbox Drain](hosted-governance-outbox-drain.md)  
  Documents the ASP.NET Core/generic-host worker integration for draining provider-neutral governance outbox entries.

* [Outbox Multi-Worker Concurrency](outbox-multi-worker-concurrency.md)  
  Documents horizontally scaled worker guidance, EF Core optimistic concurrency limits, and host-owned claim/lease or partitioning patterns.

* [Outbox Drain Reliability and Alerting](outbox-drain-reliability-and-alerting.md)  
  Documents provider-neutral monitoring, alerting, retry/dead-letter handling, and recovery guidance.

* [Safe Audit and Telemetry Data](safe-audit-telemetry-data.md)  
  Documents safe-to-store and safe-to-export practices for audit residue, governance emissions, telemetry attributes, reason codes, metadata, and host context mapping.

* [Audit Residue Observability Schema](audit-residue-observability-schema.md)  
  Documents provider-neutral telemetry, traceability, outbox, emission, and PII-safe identifier fields.

* [DLP and Classification Failure Policy](dlp-classification-failure-policy.md)  
  Documents provider-neutral policy behavior for DLP/classification outages, timeouts, indeterminate results, and blocked/classified results.

## Released provider package documentation

These pages describe currently released provider or provider-adjacent packages. OpenTelemetry is the only concrete released governance emission provider package in `1.1.0`.

* [Roslyn Analyzers](roslyn-analyzers.md)  
  Documents build-time analyzer safety rails for governance persistence and continuation flows.

* [Released: OpenTelemetry Governance Emission Provider](opentelemetry-governance-emission-provider.md)  
  Documents the concrete `CDCavell.AsiBackbone.OpenTelemetry` provider package and host-owned exporter boundary.

* [Signing Provider Package Boundary](signing-provider-package-boundary.md)  
  Documents the released signing-provider package boundary, local-development signer posture, and managed-key adapter direction.

* [Managed-Key Signing Provider](managed-key-signing-provider.md)  
  Documents the released managed-key signing adapter package and host-owned managed-key client boundary.

## Security and cryptographic boundaries

These pages describe signing, verification, capability, and cryptographic-hardening posture. They do not imply production tamper-evidence unless a concrete signing/storage/key-management path is deployed by the host.

* [Production Wording and Stable Signing Boundaries](production-wording-and-alpha-limitations.md)  
  Distinguishes stable Core signing-ready primitives, the local-development signer, the managed-key adapter boundary, future concrete provider packages, and unsupported production tamper-evidence claims.

* [Signing-Ready Receipts and Key Handling](signing-ready-receipts-and-key-handling.md)  
  Documents Core-neutral signing and verification abstractions, signing-ready audit receipt metadata, and key identifier/version handling.

* [Signed Audit and Outbox Records](signed-audit-and-outbox-records.md)  
  Documents implemented unsigned, signing-ready, and signed artifact flows.

* [Verification Policy and Result Handling](verification-policy-and-result-handling.md)  
  Documents provider-neutral verification categories and host policy action mapping.

* [Key Rotation and Retired-Key Verification](key-rotation-and-retired-key-verification.md)  
  Documents key metadata, key lifecycle states, retired-key verification behavior, compromised-key response, and verification policy mappings.

* [Capability Grant Hardening](capability-grant-hardening.md)  
  Documents scoped grant metadata, proof validation, execution-boundary checks, bounded-use storage expectations, and failure behavior.

* [Audit Integrity Chain Model](audit-integrity-chain-model.md)  
  Documents the append-only hash-chain integrity model and safe wording around chained records versus externally anchored tamper-evidence.

* [Cryptographic Security Posture and Production Guidance](cryptographic-security-posture.md)  
  Documents signing-ready versus signed, verified, chained, and externally anchored records; production key-management guidance; local signer and managed-key adapter limits; and security non-goals.

* [Cryptographic Security Hardening Roadmap](cryptographic-security-hardening-roadmap.md)  
  Splits cryptographic hardening into child issues while preserving Core provider neutrality.

## Design-only and future provider strategy

These pages remain available as strategy/design material. They are not released provider packages unless a future release explicitly says so. See [1.1.0 Release Notes](release-notes-110.md#accepted-deferrals) for current deferral status.

* [Design-Only: Event Hubs Governance Emission Provider](event-hubs-governance-emission-provider-design.md)  
  Documents a future optional Event Hubs streaming provider design. No Event Hubs NuGet package or Azure SDK adapter is included in the `1.1.0` stable package family.

* [Strategy-Only: Purview Governance and Lineage Enrichment](purview-governance-lineage-enrichment-strategy.md)  
  Documents a future optional Microsoft Purview governance enrichment strategy. No Purview NuGet package or SDK adapter is included in the `1.1.0` stable package family.

## Advanced scenarios

Scenario pages describe applied patterns. They remain available without implying current package implementation beyond the documented seams.

* [AI Agent Gateway](scenarios/ai-agent-gateway.md)
* [Human Approval Before AI Tool Execution](scenarios/human-approval-before-ai-tool-execution.md)
* [High-Risk Administrative Action](scenarios/high-risk-administrative-action.md)
* [Sensitive Data Access Request](scenarios/sensitive-data-access-request.md)
* [Deployment or Infrastructure Change Gate](scenarios/deployment-or-infrastructure-change-gate.md)
* [Robotics Operational Gateway](scenarios/robotics-operational-gateway.md)

## Quality and release process

These pages support maintainers and release validation. They are useful, but they are not the first stop for package consumers.

* [Quality Reports](../quality/index.md)  
  Landing page for coverage, Core branch coverage, mutation analysis, and external consumer smoke-test reports.

* [Release Validation](release-validation.md)  
  Documents the reusable stable release validation process.

* [Historical 1.1.0 Release Readiness Record](release-readiness-checklist.md)  
  Retains the `1.1.0` release-candidate control sheet as a historical record and future checklist shape.

* [API Baseline and Boundary Checks](api-baseline-and-boundary-checks.md)  
  Documents package-boundary checks and API baseline direction.

* [Developer Checklist](developer-checklist.md)  
  Provides maintainer-oriented development checks.

* [1.0.0 Release Notes](release-notes-100.md)  
  Describes the first stable package-family release identity, known limitations, and stable package boundary.

## Historical design records

These pages are retained for traceability. They are separated from current stable usage so readers do not mistake them for the latest package guidance.

* [Historical Stable API Review](stable-api-review.md)  
  Records the initial stable API review for the `1.0.0` surface.

* [Historical Alpha Package Boundary](alpha-package-boundary.md)  
  Documents the original `0.1.0-alpha.1` Core package boundary.

* [Historical Core Alpha Readiness Review](core-alpha-readiness-review.md)  
  Retains the historical alpha-readiness review.
