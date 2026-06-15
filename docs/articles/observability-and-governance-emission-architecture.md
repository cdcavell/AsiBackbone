# Observability and Governance Emission Architecture

This article documents the forward `1.1.0` architecture direction for observability, durable outbox persistence, and optional governance emission providers.

Target milestone: `1.1.0 - Observability, Outbox, and Governance Emission Providers`.

In this software project, **ASI** means **Accountable Systems Infrastructure**. AsiBackbone is a governance spine for consequential software decision flow. It is not an artificial superintelligence implementation, AI model host, robot controller, compliance product, signing product, or cloud governance platform by itself.

> [!IMPORTANT]
> Provider-specific integrations depend on Core. Core must never depend on provider-specific integrations.
>
> `CDCavell.AsiBackbone.Core` remains framework-neutral and vendor-neutral. Observability platforms, streaming systems, governance catalogs, signing systems, storage providers, and cloud-specific enrichment belong in optional packages or host applications.

## Purpose

The `1.1.0` integration direction is to preserve AsiBackbone's neutral governance spine while allowing host applications to emit structured decision records into operational and governance systems.

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

## High-level architecture

```text
Host application
  |
  | builds actor context, policy context, metadata, and host policy inputs
  v
CDCavell.AsiBackbone.Core
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
Optional provider packages or host adapters
  |
  | OpenTelemetry export
  | Azure Monitor / Log Analytics emission
  | Event Hubs streaming
  | Purview governance / lineage enrichment
  | signing or immutable storage provider
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
- gateway result contracts;
- provider-neutral event envelope abstractions, if added later;
- provider-neutral outbox contracts, if added later.

Core should not contain:

- Azure Monitor, Application Insights, Log Analytics, Event Hubs, or Purview SDK dependencies;
- OpenTelemetry exporter configuration;
- cloud workspace, tenant, subscription, resource group, namespace, topic, table, or catalog identifiers;
- provider-specific retry policies;
- provider-specific payload mapping;
- provider-specific data classification, DLP, or lineage rules;
- signing-provider implementation or key-management logic.

## Provider-specific enrichment

Provider packages or host adapters translate the neutral governance record into provider-specific payloads.

| Provider path | Appropriate responsibility | Not appropriate responsibility |
| --- | --- | --- |
| OpenTelemetry | Convert neutral decision and audit residue fields into spans, events, metrics, and attributes. | Redefine Core decision semantics or require Core to reference OpenTelemetry packages. |
| Azure Monitor / Log Analytics | Emit operationally useful records for search, alerting, dashboards, and incident review. | Become the only audit store or force Azure dependencies into Core. |
| Event Hubs | Stream governance events to downstream processing systems. | Replace local durability or bypass policy, acknowledgment, and gateway records. |
| Purview | Enrich governance records with classification, lineage, catalog, or policy-context metadata. | Store raw audit records by default or become the primary audit ledger. |
| Signing / immutable storage | Add cryptographic verification or tamper-evidence when implemented. | Claim tamper-evidence before signing, verification, key management, and storage guarantees exist. |

Provider-specific packages must depend on Core. Core must not depend on provider packages.

## Durable outbox baseline

External emission should follow an outbox-style pattern so host applications do not lose governance records when downstream providers are unavailable.

A durable outbox record should capture enough information to retry or inspect provider emission without recomputing the original decision.

Suggested fields:

| Field | Purpose |
| --- | --- |
| `OutboxRecordId` | Stable local identifier for the emission attempt. |
| `EventId` | Stable governance event identifier. |
| `EventType` | Decision, acknowledgment, capability token, gateway result, audit residue, or provider emission event. |
| `SchemaVersion` | Version of the serialized event envelope. |
| `CorrelationId` | Links the event to the host request or workflow. |
| `TraceId` | Links the event to distributed tracing when available. |
| `ActorId` | Host-provided actor identifier, preferably opaque and minimized. |
| `OperationName` | Host-provided operation name or workflow code. |
| `DecisionOutcome` | Neutral decision result. |
| `ReasonCodes` | Neutral reason code list. |
| `PolicyVersion` | Policy version used for the decision. |
| `PolicyHash` | Policy hash used for the decision, when available. |
| `AcknowledgmentId` | Links acknowledgment challenge and response when applicable. |
| `CapabilityTokenId` | Links scoped execution grant when applicable. |
| `GatewayRecordId` | Links gateway execution boundary result when applicable. |
| `ClassificationState` | Unclassified, classified, redacted, blocked, quarantined, or failed. |
| `EmissionStatus` | Pending, ready, emitted, failed-retryable, failed-terminal, or quarantined. |
| `ProviderName` | Optional provider target such as OpenTelemetry, Azure Monitor, Event Hubs, or Purview. |
| `ProviderRecordId` | Provider-side identifier after successful emission, when available. |
| `AttemptCount` | Retry tracking. |
| `NextAttemptUtc` | Retry scheduling. |
| `LastErrorCode` | Normalized provider or classification error code. |
| `CreatedUtc` / `UpdatedUtc` | Operational lifecycle timestamps. |

The outbox should be durable before provider emission. A process crash, provider outage, network failure, rate limit, or DLP failure should not erase the original decision residue.

## OpenTelemetry as the neutral observability path

OpenTelemetry is the preferred neutral observability path because it can represent traces, metrics, logs, spans, events, and attributes without binding Core to a single cloud provider.

Preferred placement:

```text
Core neutral decision and audit residue
  -> host or optional OpenTelemetry adapter
  -> OpenTelemetry spans/events/metrics
  -> selected exporter/backend
```

Recommended OpenTelemetry mapping:

| AsiBackbone concept | OpenTelemetry mapping |
| --- | --- |
| `CorrelationId` / `TraceId` | Trace context or span attributes. |
| Decision evaluation | Span event or structured log event. |
| Decision outcome | Attribute such as `asi.decision.outcome`. |
| Reason codes | Attribute list or structured event field. |
| Policy version/hash | Attributes such as `asi.policy.version` and `asi.policy.hash`. |
| Acknowledgment challenge | Span event or linked event. |
| Capability token issuance | Span event with minimized token identifier only. |
| Gateway execution result | Span event or child span. |
| Outbox emission | Span event or metric for pending, emitted, failed, and quarantined records. |

Telemetry payloads should remain metadata-minimized. Do not place secrets, raw tokens, raw personal data, protected records, prompt bodies, document contents, or sensitive payloads into telemetry attributes.

## Azure Monitor / Log Analytics provider path

Azure Monitor and Log Analytics are possible backend targets or optional provider paths. They are useful for operational search, dashboards, alerts, and incident response.

Appropriate Azure Monitor / Log Analytics usage:

- emit minimized decision and audit residue metadata;
- support dashboards for decision outcomes, escalation rates, acknowledgment rates, gateway denials, and provider emission failures;
- support operational alerts for repeated denials, DLP failures, outbox backlog growth, or provider outage patterns;
- preserve correlation and trace fields so host logs and governance records can be joined during incident review.

Boundary limits:

- Azure Monitor / Log Analytics should not be described as the authoritative audit ledger unless the host explicitly designs it that way;
- provider emission should not replace durable local/outbox persistence;
- Azure dependencies belong in an optional provider package or host-owned adapter, not in Core;
- provider payloads must be classified, minimized, and redacted according to host policy before emission.

## Event Hubs streaming provider path

Event Hubs is an optional streaming provider for organizations that need downstream processing, security analytics, cross-system integration, or near-real-time governance feeds.

Appropriate Event Hubs usage:

- stream minimized governance event envelopes after local outbox persistence;
- partition by stable low-sensitivity keys such as tenant, region, operation group, or event type when appropriate;
- feed downstream processors for dashboards, anomaly detection, incident review, or governance workflows;
- allow retry and replay from the local outbox if emission fails.

Boundary limits:

- Event Hubs is not the local source of truth;
- event streaming should not bypass acknowledgment, capability token, or gateway boundaries;
- payloads should avoid raw sensitive data unless the host has explicitly classified, approved, encrypted, and governed that data path.

## Purview governance and lineage enrichment

Purview should be treated as optional governance and lineage enrichment, not raw audit storage by default.

Appropriate Purview usage:

- map governance events to classified assets, data products, workflows, or lineage nodes;
- attach classification labels or policy-context metadata to emitted records;
- enrich investigation workflows with catalog context;
- provide governance discovery for where consequential decisions interact with sensitive resources.

Boundary limits:

- raw audit records should remain in the host-owned durable store unless the host explicitly chooses another authoritative audit store;
- Purview should not receive raw personal data, protected records, prompts, secrets, or full payloads by default;
- Purview enrichment failures should be captured in the outbox and handled according to policy without erasing the original local record.

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

Suggested failure behavior:

| Failure condition | Recommended default |
| --- | --- |
| Classifier unavailable | Do not emit sensitive or unclassified payloads. Keep the outbox record pending, deferred, or quarantined according to policy. |
| Classification inconclusive | Emit only a minimized safe envelope, or defer emission until review. |
| DLP violation | Block provider emission, retain local audit residue, and mark the outbox record as blocked or quarantined. |
| Provider unavailable | Retain local record and retry with backoff. |
| Provider rejects payload | Record normalized error, stop or retry according to error class, and avoid repeated unsafe emission attempts. |
| Purview enrichment unavailable | Continue preserving local governance record; retry enrichment if policy allows. |
| Signing required but signing unavailable | Do not claim tamper-evidence. Defer, quarantine, or emit unsigned only if host policy explicitly permits it. |

DLP and classification are host-owned responsibilities unless a future package explicitly implements a classifier integration boundary.

## Signing-ready and current limitations

The `1.0.0` package family already carries fields that are useful for future signing and verification, such as schema version, event ID, record ID, timestamps, correlation ID, trace ID, policy version, and policy hash.

Those fields are signing-ready, not signed.

Do not describe current records as:

- cryptographically signed;
- tamper-evident;
- tamper-proof;
- immutable;
- non-repudiable;
- legally certified;
- compliance-approved.

Accurate wording for the current boundary:

- records include fields that can support future signing providers;
- durable local/outbox records can carry policy and schema version identifiers;
- hosts may apply their own signing, hashing, timestamping, immutable storage, or retention controls;
- future provider packages may add explicit signing behavior after their own stable API and security review.

## Candidate package boundaries

These names are planning examples, not release promises.

| Candidate boundary | Role | Dependency direction |
| --- | --- | --- |
| `CDCavell.AsiBackbone.Core` | Neutral governance primitives, decision contracts, acknowledgment, capability, audit residue, and future provider-neutral seams. | No provider dependencies. |
| `CDCavell.AsiBackbone.Storage.InMemory` | Development and test storage. | Depends on Core. |
| `CDCavell.AsiBackbone.EntityFrameworkCore` | Host-owned EF Core persistence and durable local storage support. | Depends on Core. |
| `CDCavell.AsiBackbone.Outbox` | Provider-neutral outbox contracts and processing helpers, if split from Core. | Depends on Core. |
| `CDCavell.AsiBackbone.Observability.OpenTelemetry` | OpenTelemetry adapter package. | Depends on Core. |
| `CDCavell.AsiBackbone.Observability.AzureMonitor` | Azure Monitor / Log Analytics adapter package. | Depends on Core and Azure SDKs. |
| `CDCavell.AsiBackbone.Streaming.EventHubs` | Event Hubs streaming adapter package. | Depends on Core and Azure SDKs. |
| `CDCavell.AsiBackbone.Governance.Purview` | Purview catalog, classification, and lineage enrichment adapter package. | Depends on Core and Microsoft Purview SDKs/APIs. |
| `CDCavell.AsiBackbone.Signing.*` | Signing, verification, key, or immutable storage adapters. | Depends on Core and selected signing/storage providers. |

Future package names may change. The architectural rule should not change: provider packages depend on Core; Core never depends on provider packages.

## Implementation phases

The milestone should be implemented in phases so documentation does not imply integrations are already available.

| Phase | Focus | Outcome |
| --- | --- | --- |
| Phase 0 | Stable `1.0.0` foundation | Core decision flow, acknowledgment, capability, audit residue, storage, and ASP.NET Core host seams remain the baseline. |
| Phase 1 | Architecture documentation | Publish this architecture direction and link it from documentation navigation. |
| Phase 2 | Durable outbox model | Define host-owned durable/outbox persistence contracts, record status values, retry expectations, and privacy boundaries. |
| Phase 3 | Neutral observability | Add OpenTelemetry guidance or optional adapter surface without binding Core to a cloud provider. |
| Phase 4 | Azure provider paths | Add optional Azure Monitor / Log Analytics and Event Hubs provider packages or samples. |
| Phase 5 | Governance enrichment | Add optional Purview enrichment guidance or provider package for catalog, classification, and lineage context. |
| Phase 6 | Signing and tamper-evidence | Add signing/verification providers only after key management, verification, rotation, storage, and wording boundaries are documented. |

Until a phase is implemented and released, describe it as planned, optional, preview, sample-only, or host-owned guidance.

## Release wording checklist

Use this checklist when documenting `1.1.0` provider work:

- State that AsiBackbone is a governance spine, not an intelligence engine.
- State that Core remains vendor-neutral and framework-neutral.
- State that provider packages depend on Core, never the reverse.
- State that durable local/outbox persistence is the reliability baseline before external emission.
- State that OpenTelemetry is the preferred neutral observability path.
- State that Azure Monitor / Log Analytics is an optional backend target or provider path.
- State that Event Hubs is optional streaming, not a replacement for local durability.
- State that Purview is optional governance/lineage enrichment, not raw audit storage by default.
- State that DLP/classification policy is host-owned unless a future provider explicitly implements it.
- State that current records are signing-ready, not automatically signed or tamper-evident.
- List implementation phases instead of implying all integrations already exist.

## Related documentation

- [Privacy and Signing Boundaries](privacy-and-signing-boundaries.md)
- [Policy Evaluator Pipeline](policy-evaluator-pipeline.md)
- [Core Domain Language](core-domain-language.md)
- [EF Core Host Ownership and Migrations](ef-core-host-ownership-and-migrations.md)
- [ASP.NET Core Integration Boundary](aspnetcore-integration-boundary.md)
- [API Baseline and Architecture Boundary Checks](api-baseline-and-boundary-checks.md)
