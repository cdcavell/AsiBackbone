# Documentation

This section contains detailed documentation for the ASI Backbone.

Start with [Getting Started](getting-started.md), then use the navigation menu to browse the major documentation areas.

## Application documentation

* [Core Domain Language](core-domain-language.md)
  Defines the Core terminology for governance spine, constraints, collapse boundary, audit residue, actor context, decision results, operation results, acknowledgment, capability tokens, and gateway boundaries.

* [1.0.0 Release Notes](release-notes-100.md)
  Describes the first stable release identity, stable package list, known limitations, and upgrade guidance.

* [1.1.0 Release Notes](release-notes-110.md)
  Describes the additive `1.1.0` observability, durable outbox, governance emission provider, OpenTelemetry, DLP/classification, signing-ready, and provider-deferral release boundary.

* [Upgrade Guide: 1.0.0 to 1.1.0](upgrade-100-to-110.md)
  Explains how existing `1.0.0` consumers can adopt `1.1.0` incrementally, including durable outbox storage, hosted drain wiring, OpenTelemetry provider usage, Azure Monitor exporter guidance, and accepted deferrals.

* [Release Readiness Checklist](release-readiness-checklist.md)
  Provides the final pre-tag checklist for the first stable NuGet package release, including issue status, workflow gates, package metadata inspection, wording boundaries, and non-blocking follow-up tracking.

* [Release Validation](release-validation.md)
  Documents the release-blocking workflow path for restore, build, formatting, tests, DocFX, package creation, generated NuGet metadata validation, package-consumer smoke tests, and package artifact upload.

* [Governance Tool Comparisons](governance-tool-comparisons.md)
  Compares Azure Policy, Open Policy Agent (OPA), Microsoft Agent Governance Toolkit, and AsiBackbone as complementary governance layers without positioning any tool as a replacement for the others.

* [Equations and Toy Models](equations-and-toy-models.md)
  Explains the conceptual progression from `Λ(t)` to `Λ(τ)` to `ΛS(x, τ)` and maps the Eden/ASI collapse notation into practical AsiBackbone software terms: active policy structure, allowed decision states, acknowledgment, audit residue, and gateway-safe execution.

* [Observability and Governance Emission Architecture](observability-and-governance-emission-architecture.md)
  Documents the `1.1.0` observability, outbox, and governance emission architecture direction, including Core-neutral provider seams, durable local/outbox persistence, OpenTelemetry, Azure Monitor, Event Hubs, Purview enrichment, DLP/classification failure behavior, signing limitations, and phased package boundaries.

* [OpenTelemetry Governance Emission Provider](opentelemetry-governance-emission-provider.md)
  Documents the concrete `CDCavell.AsiBackbone.OpenTelemetry` provider package, envelope-to-activity/metric mappings, Azure Monitor-through-exporter guidance, and decision -> outbox -> drain -> OpenTelemetry flow.

* [Event Hubs Governance Emission Provider Design](event-hubs-governance-emission-provider-design.md)
  Documents the optional Event Hubs streaming provider design, versioned envelope mapping, Managed Identity configuration direction, durable outbox interaction, retry/dead-letter behavior, and no-live-Azure test seams.

* [Purview Governance and Lineage Enrichment Strategy](purview-governance-lineage-enrichment-strategy.md)
  Documents the optional Microsoft Purview governance enrichment strategy, selected summarized enrichment model, PII-safe field mapping, correlation strategy, catalog-noise risks, and Core-neutral provider boundary.

* [Signing-Ready Receipts and Key Handling](signing-ready-receipts-and-key-handling.md)
  Documents the Core-neutral signing and verification abstractions, signing-ready audit receipt metadata, key identifier/version handling, verification seam, and current unsigned-vs-production-signed wording boundary.

* [Signing Provider Package Boundary](signing-provider-package-boundary.md)
  Documents the concrete signing-provider package boundary, provider responsibilities, key-material handling rules, configuration seams, local-development signer posture, and managed-key provider direction.

* [Cryptographic Security Posture and Production Guidance](cryptographic-security-posture.md)
  Documents signing-ready versus signed, verified, chained, and externally anchored records; production key-management guidance; local signer limits; host responsibilities; audit/outbox signing examples; capability-token validation; and security non-goals.

* [Cryptographic Security Hardening Roadmap](cryptographic-security-hardening-roadmap.md)
  Splits cryptographic hardening into child issues for canonical hashing, signing providers, signed audit/outbox records, verification policy, key rotation, audit-chain integrity, and capability-token hardening while preserving Core provider neutrality.

* [DLP and Classification Failure Policy](dlp-classification-failure-policy.md)
  Documents the provider-neutral Core policy model for DLP/classification service outages, timeouts, indeterminate results, blocked/classified results, and risk-based fail-open/fail-closed behavior.

* [Governance Emission Contract](governance-emission-contract.md)
  Defines the provider-neutral emission contract between Core governance artifacts, durable local audit/outbox persistence, and optional downstream providers.

* [Durable Audit and Outbox Persistence](durable-audit-outbox-persistence.md)
  Documents the provider-neutral local persistence seam for audit residue lifecycle events and governance emission outbox entries before optional provider delivery.

* [Audit Residue Observability Schema](audit-residue-observability-schema.md)
  Documents the provider-neutral telemetry, traceability, outbox, emission, and PII-safe identifier fields added to audit residue for observability and governance emission providers.

* [Historical Alpha Package Boundary](alpha-package-boundary.md)
  Documents the original `0.1.0-alpha.1` boundary for `CDCavell.AsiBackbone.Core`, including what belongs in Core and what belongs in integration packages.

* [EF Core Integration Boundary](ef-core-integration-boundary.md)  
  Defines the implemented boundary for `CDCavell.AsiBackbone.EntityFrameworkCore`, including host-owned `DbContext`, migration ownership, provider-neutral configuration, and `ModelBuilder` extension guidance.

* [EF Core Host Ownership and Migration Guidance](ef-core-host-ownership-and-migrations.md)  
  Explains how host applications own the EF Core `DbContext`, provider, connection string, migrations, schema deployment, and operational lifecycle while applying ASI Backbone model configurations.

* [ASP.NET Core Integration Boundary](aspnetcore-integration-boundary.md)  
  Defines the implemented web-host adapter boundary for `CDCavell.AsiBackbone.AspNetCore`, including service registration, request correlation, audit enrichment, HTTP outcome mapping, acknowledgment challenge helpers, and compatibility with both plain ASP.NET Core and NetCoreApplicationTemplate hosts.
