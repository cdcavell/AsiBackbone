# Observability and Governance Emission Architecture

This article documents the released `3.0.0` observability, durable outbox, signing, and governance emission architecture boundary plus the future provider directions that remain design-only.

In this software project, **ASI** means **Accountable Systems Infrastructure**. AsiBackbone is a governance spine for consequential software decision flow. It is not an artificial superintelligence implementation, AI model host, robot controller, compliance product, signing product, or cloud governance platform by itself.

> [!IMPORTANT]
> Provider-specific integrations depend on Core. Core must never depend on provider-specific integrations.
>
> `AsiBackbone.Core` remains framework-neutral and vendor-neutral. Observability platforms, streaming systems, governance catalogs, signing systems, storage providers, and cloud-specific enrichment belong in optional packages or host applications.
>
> `AsiBackbone.OpenTelemetry` is the only concrete released governance emission provider package in `3.0.0`. Event Hubs, Purview, Azure Monitor-specific SDK adapters, immutable-storage providers, and additional Azure-specific packages remain design-only or host-owned guidance unless a future release separately reviews and ships them. 

## Purpose

The `3.0.0` integration direction preserves AsiBackbone's neutral governance spine while allowing host applications to emit structured decision records into operational and governance systems.

The primary architecture path is:

```text
Decision
  -> Acknowledgment
  -> Capability token
  -> Gateway execution boundary
  -> Audit residue
  -> Durable local/outbox record
  -> Optional provider emission
```

This sequence keeps the source-of-truth governance record local and durable before downstream emission occurs. External systems may enrich, search, alert, stream, classify, or catalog the event, but they should not become the only place where the governance decision exists.

## Released versus design-only provider boundary

| Provider path | `3.0.0` status | Appropriate responsibility | Not appropriate responsibility |
| --- | --- | --- | --- |
| OpenTelemetry | **Released package**: `AsiBackbone.OpenTelemetry`. | Convert neutral governance emission envelopes into .NET diagnostics through `ActivitySource`, activity events, tags, and `Meter` metrics. | Configure exporters, redefine Core decision semantics, or require Core to reference OpenTelemetry packages. |
| Azure Monitor / Log Analytics | **Host-configured exporter guidance** through OpenTelemetry. No Azure Monitor-specific AsiBackbone package is released. | Receive telemetry through the host's OpenTelemetry exporter pipeline. | Become the only audit store or force Azure SDK dependencies into Core. |
| Event Hubs | **Design-only future provider strategy.** No Event Hubs package is released in `3.0.0`. | Future optional streaming adapter for minimized governance events after durable outbox persistence. | Replace local durability or imply a current Event Hubs NuGet package exists. |
| Purview | **Strategy-only future enrichment direction.** No Purview package is released in `3.0.0`. | Future optional governance/catalog/lineage enrichment for selected, summarized, classified events. | Store raw audit records by default, become the primary audit ledger, or imply a current Purview NuGet package exists. |
| Signing providers | **Released package boundaries** for local-development and managed-key adapter packages. | Attach signing/verification behavior through provider packages and host-owned key operations. | Claim tamper-evidence without deployed signing, verification, key management, storage, and retention controls. |

Provider-specific packages must depend on Core. Core must not depend on provider packages.

## High-level architecture

```text
Host application
  |
  | builds actor context, policy context, metadata, and host policy inputs
  v
AsiBackbone.Core
  |
  | evaluates policy and returns governance decision
  | creates acknowledgment challenge when required
  | creates scoped capability token when allowed
  | creates audit residue / decision receipt shape
  v
Durable local store / outbox
  |
  | persists decision, acknowledgment, token, gateway, and emission state
  | retries provider emission safely
  | records provider failure and classification state
  v
Released provider packages or host adapters
  |
  | OpenTelemetry projection through AsiBackbone.OpenTelemetry
  | host-configured Azure Monitor / Log Analytics exporter
  |\  v
Design-only / future provider strategy
  |
  | Event Hubs streaming provider design
  | Purview governance / lineage enrichment strategy
  | future Azure-specific or immutable-storage providers
```

The local outbox is the reliability boundary. Provider emission is downstream of that boundary.

## Core-neutral rule placement

Core should define the neutral vocabulary and control points that every provider can use without importing cloud SDKs, telemetry SDKs, catalog SDKs, or storage-specific dependencies.

Core-neutral concerns include:

- decision outcomes such as `Allow`, `Deny`, `Defer`, `RequireAcknowledgment`, and `Escalate`;
- actor, policy, operation, acknowledgment, capability, gateway, and audit identifiers;
- correlation ID, trace ID, event ID, record ID, schema version, policy version, and policy hash fields;
- reason codes and policy evaluation results;
- acknowledgment challenge and acknowledgment record shapes;
- scoped capability token result shapes;
- audit residue / decision receipt contracts;
- governance emission envelopes and provider-neutral results;
- durable outbox contracts and drain primitives.

Core should not contain:

- Azure Monitor, Application Insights, Log Analytics, Event Hubs, or Purview SDK dependencies;
- OpenTelemetry exporter configuration;
- cloud workspace, tenant, subscription, resource group, namespace, topic, table, or catalog identifiers;
- provider-specific retry policies;
- provider-specific payload mapping;
- provider-specific data classification, DLP, or lineage rules;
- signing-provider implementation or key-management logic.

## Durable outbox baseline

External emission should follow an outbox-style pattern so host applications do not lose governance records when downstream providers are unavailable.

The durable outbox record should capture enough information to retry or inspect provider emission without recomputing the original decision. The outbox should be durable before provider emission. A process crash, provider outage, network failure, rate limit, or DLP failure should not erase the original decision residue.

Recommended sequence:

```text
Audit residue / lifecycle event
  -> host-owned durable store
  -> GovernanceEmissionEnvelope
  -> IAsiBackboneGovernanceOutboxStore
  -> AsiBackboneGovernanceOutboxDrain
  -> IAsiBackboneGovernanceEmitter
  -> provider result
  -> delivered / failed / retryable / deferred / dead-letter outbox state
```

## OpenTelemetry as the released governance emission provider

`AsiBackbone.OpenTelemetry` is the concrete released governance emission provider package for current release.

Preferred placement:

```text
Core neutral governance emission envelope
  -> AsiBackbone.OpenTelemetry
  -> ActivitySource / Meter
  -> host OpenTelemetry SDK pipeline
  -> selected exporter/backend
```

The OpenTelemetry package is responsible for provider-neutral envelope projection into .NET diagnostics primitives. The host remains responsible for exporter selection, Azure Monitor connection settings, sampling, retention, backend routing, and operational alerting.

Telemetry payloads should remain metadata-minimized. Do not place secrets, raw tokens, raw personal data, protected records, prompt bodies, document contents, or sensitive payloads into telemetry attributes.

## Azure Monitor / Log Analytics guidance

Azure Monitor and Log Analytics are backend targets reached through the host-owned OpenTelemetry exporter pipeline. No Azure Monitor-specific AsiBackbone package is included in `3.0.0`.

Accurate `3.0.0` wording:

```text
AsiBackbone.OpenTelemetry
  -> ActivitySource / Meter
  -> host OpenTelemetry SDK pipeline
  -> host-configured Azure Monitor exporter
  -> Azure Monitor / Application Insights / Log Analytics
```

Boundary limits:

- Azure Monitor / Log Analytics should not be described as the authoritative audit ledger unless the host explicitly designs it that way;
- provider emission should not replace durable local/outbox persistence;
- Azure dependencies belong in host exporter configuration or a future optional provider package, not in Core;
- provider payloads must be classified, minimized, and redacted according to host policy before emission.

## Design-only: Event Hubs streaming provider path

Event Hubs is a design-only future provider strategy in the current documentation set. It is not a released AsiBackbone NuGet package in `3.0.0`.

Appropriate future Event Hubs usage:

- stream minimized governance event envelopes after local outbox persistence;
- partition by stable low-sensitivity keys such as tenant, region, operation group, or event type when appropriate;
- feed downstream processors for dashboards, anomaly detection, incident review, or governance workflows;
- allow retry and replay from the local outbox if emission fails.

Boundary limits:

- Event Hubs is not the local source of truth;
- event streaming should not bypass acknowledgment, capability token, or gateway boundaries;
- payloads should avoid raw sensitive data unless the host has explicitly classified, approved, encrypted, and governed that data path;
- the presence of [Design-Only: Event Hubs Governance Emission Provider](event-hubs-governance-emission-provider-design.md) does not imply a released Event Hubs provider package exists.

## Strategy-only: Purview governance and lineage enrichment

Purview is a strategy-only future enrichment direction in the current documentation set. It is not a released AsiBackbone NuGet package in `3.0.0`.

Appropriate future Purview usage:

- map selected governance events to classified assets, data products, workflows, or lineage nodes;
- attach classification labels or policy-context metadata to summarized records;
- enrich investigation workflows with catalog context;
- provide governance discovery for where consequential decisions interact with sensitive resources.

Boundary limits:

- raw audit records should remain in the host-owned durable store unless the host explicitly chooses another authoritative audit store;
- Purview should not receive raw personal data, protected records, prompts, secrets, or full payloads by default;
- Purview enrichment failures should be captured in the outbox and handled according to policy without erasing the original local record;
- the presence of [Strategy-Only: Purview Governance and Lineage Enrichment](purview-governance-lineage-enrichment-strategy.md) does not imply a released Purview provider package exists.

## DLP and classification failure policy

Provider emission must pass through host-owned classification and DLP rules before leaving the local boundary.

At minimum, the host should define policy for:

- what fields are allowed to leave the local store;
- which metadata fields require redaction, hashing, tokenization, generalization, or omission;
- whether actor identifiers can be emitted as-is or must be opaque;
- whether region, tenant, workflow, or resource identifiers need coarsening;
- whether provider emission is allowed when classification is unavailable;
- what happens when classification is inconclusive;
- which event types require signing before external emission;
- which provider failures are retryable, terminal, or escalation-worthy.

DLP and classification are host-owned responsibilities unless a future package explicitly implements a classifier integration boundary.

## Signing-ready and current limitations

The `3.0.0` package family includes signing-ready abstractions and provider signing boundaries, but signing alone does not prove tamper-evidence.

Do not describe records as:

- tamper-proof;
- immutable;
- non-repudiable;
- legally certified;
- compliance-approved;
- externally anchored by default.

Accurate wording:

- records can carry signing-ready metadata;
- artifact hashes can be signed through configured provider packages or host-owned signing services;
- verification policy can classify signature outcomes;
- production tamper-evidence requires deployed signing, verification, protected key management, durable append-only or otherwise controlled storage, retention policy, monitoring, and incident response.

## Candidate and released package boundaries

| Boundary | Current status | Role |
| --- | --- | --- |
| `AsiBackbone.Core` | Released | Neutral governance primitives, decision contracts, acknowledgment, capability, audit residue, emission contracts, outbox contracts, signing-ready seams, and verification policy. |
| `AsiBackbone.Storage.InMemory` | Released | Development, test, sample, and local-validation storage. |
| `AsiBackbone.EntityFrameworkCore` | Released | Host-owned EF Core persistence and durable local storage support. |
| `AsiBackbone.AspNetCore` | Released | ASP.NET Core host integration and hosted outbox drain support. |
| `AsiBackbone.Analyzers` | Released | Build-time governance safety rails. |
| `AsiBackbone.OpenTelemetry` | Released | Concrete OpenTelemetry governance emission provider. |
| `AsiBackbone.Signing.LocalDevelopment` | Released | Local-development signing and verification proof path. |
| `AsiBackbone.Signing.ManagedKey` | Released | Managed-key signing adapter boundary with host-owned managed-key client. |
| `AsiBackbone.Streaming.EventHubs` | Design-only candidate | Future Event Hubs streaming adapter package. |
| `AsiBackbone.Governance.Purview` | Strategy-only candidate | Future Purview catalog, classification, and lineage enrichment adapter package. |
| `AsiBackbone.Observability.AzureMonitor` | Future/host-owned guidance | Azure Monitor should currently be reached through host OpenTelemetry exporter configuration. |

Future package names may change. The architectural rule should not change: provider packages depend on Core; Core never depends on provider packages.

## Release wording checklist

Use this checklist when documenting provider work:

- State that AsiBackbone is a governance spine, not an intelligence engine.
- State that Core remains vendor-neutral and framework-neutral.
- State that provider packages depend on Core, never the reverse.
- State that durable local/outbox persistence is the reliability baseline before external emission.
- State that OpenTelemetry is the concrete released governance emission provider in current release.
- State that Azure Monitor / Log Analytics is reached through host-configured OpenTelemetry exporter guidance unless a future package says otherwise.
- State that Event Hubs is design-only future streaming provider strategy, not a released package in current release.
- State that Purview is strategy-only future governance/lineage enrichment, not a released package in current release.
- State that DLP/classification policy is host-owned unless a future provider explicitly implements it.
- State that signed does not automatically mean verified, tamper-evident, immutable, or legally non-repudiable.

## Related documentation

- [Released: OpenTelemetry Governance Emission Provider](opentelemetry-governance-emission-provider.md)
- [Design-Only: Event Hubs Governance Emission Provider](event-hubs-governance-emission-provider-design.md)
- [Strategy-Only: Purview Governance and Lineage Enrichment](purview-governance-lineage-enrichment-strategy.md)
- [Privacy and Signing Boundaries](privacy-and-signing-boundaries.md)
- [Policy Evaluator Pipeline](policy-evaluator-pipeline.md)
- [Core Domain Language](core-domain-language.md)
- [EF Core Host Ownership and Migrations](ef-core-host-ownership-and-migrations.md)
- [ASP.NET Core Integration Boundary](aspnetcore-integration-boundary.md)
- [API Baseline and Architecture Boundary Checks](api-baseline-and-boundary-checks.md)
