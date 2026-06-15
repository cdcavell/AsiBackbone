# 1.1.0 Release Notes

These notes describe the `1.1.0 - Observability, Outbox, and Governance Emission Providers` release boundary.

In this software project, **ASI** means **Accountable Systems Infrastructure**. AsiBackbone is governance infrastructure for accountable software decision flow. It does not implement artificial superintelligence, host AI models, control robots, certify compliance, or provide production tamper-evidence by itself.

## Release summary

`1.1.0` is an additive minor release over `1.0.0`. Existing `1.0.0` consumers can continue using the stable Core, in-memory storage, EF Core, and ASP.NET Core package surfaces without adopting the new observability, outbox, or provider paths.

The release expands the governance spine from local decision/audit records into a provider-neutral emission pipeline:

```text
Decision
  -> acknowledgment when required
  -> capability token when issued
  -> gateway or host execution boundary
  -> audit residue / lifecycle event
  -> durable local audit and outbox persistence
  -> optional provider emission
```

The durable local audit/outbox record is the reliability baseline. OpenTelemetry, Azure Monitor, Event Hubs, Purview, SIEMs, dashboards, and other downstream systems should be treated as optional projection or enrichment targets unless the host application explicitly designs them as an authoritative store.

## Stable package family

The `1.1.0` stable package line covers the package family below.

| Package | Stable role |
| --- | --- |
| `CDCavell.AsiBackbone.Core` | Framework-neutral governance primitives, decisions, acknowledgments, capability-token references, audit residue, lifecycle events, provider-neutral emission contracts, durable outbox contracts, DLP/classification failure policy primitives, and signing-ready metadata abstractions. |
| `CDCavell.AsiBackbone.Storage.InMemory` | Non-durable in-memory audit, lifecycle, and outbox helpers for tests, samples, local validation, and no-op proof paths. |
| `CDCavell.AsiBackbone.EntityFrameworkCore` | EF Core model configuration and host-owned persistence for audit ledger, audit residue lifecycle, and durable governance outbox records. |
| `CDCavell.AsiBackbone.AspNetCore` | ASP.NET Core host integration seams for service registration, request correlation, audit enrichment, HTTP result mapping, acknowledgment challenge helpers, and hosted outbox drain integration. |
| `CDCavell.AsiBackbone.OpenTelemetry` | Concrete OpenTelemetry governance emission provider that projects provider-neutral governance envelopes into .NET diagnostics primitives such as `ActivitySource` and `Meter`. |

Future Event Hubs, Purview, Azure-specific, signing-provider, gateway, robotics, or immutable-storage packages are not part of the `1.1.0` stable contract unless separately released as stable packages.

## What changed since 1.0.0

### Provider-neutral governance emission

Core now includes a provider-neutral governance emission contract so audit residue, lifecycle, gateway, and decision artifacts can be converted into stable governance emission envelopes without binding Core to OpenTelemetry, Azure Monitor, Event Hubs, Purview, SIEM, robotics, or cloud-provider SDK dependencies.

The key boundary is:

```text
IAsiBackboneGovernanceEmitter
  -> GovernanceEmissionEnvelope
  -> GovernanceEmissionResult
  -> GovernanceEmissionError
```

Providers adapt this contract into downstream systems. Core keeps the vocabulary neutral.

### Durable audit lifecycle and outbox persistence

`1.1.0` adds durable lifecycle and outbox concepts for preserving local accountability records before provider delivery is attempted.

The recommended sequence is:

1. Save the audit residue or lifecycle event locally.
2. Build a `GovernanceEmissionEnvelope`.
3. Enqueue the envelope into `IAsiBackboneGovernanceOutboxStore`.
4. Drain the outbox through an `IAsiBackboneGovernanceEmitter`.
5. Mark the outbox entry delivered, failed, retryable, deferred, or dead-lettered.

This avoids losing governance records when downstream providers are unavailable, rate-limited, misconfigured, or blocked by classification policy.

### In-memory proof paths

`CDCavell.AsiBackbone.Storage.InMemory` includes non-durable development and test helpers for lifecycle and outbox validation.

The no-op governance emitter and in-memory outbox path are intended for tests, samples, local smoke checks, and proof-of-wiring only. They are not durable production storage, not evidence of provider delivery, and not a substitute for EF Core or another host-owned durable store.

### EF Core durable adapter

`CDCavell.AsiBackbone.EntityFrameworkCore` adds host-owned durable persistence for governance outbox entries and audit residue lifecycle events.

The host application still owns:

- the `DbContext`;
- database provider;
- connection string;
- migrations;
- deployment;
- schema lifecycle;
- retention policy;
- backup and recovery;
- access controls.

The EF Core package contributes model configuration and storage adapters. It does not own the consuming application's database.

### Hosted governance outbox drain

`CDCavell.AsiBackbone.AspNetCore` adds hosted outbox drain integration for ASP.NET Core and generic-host applications.

The hosted worker can drain pending outbox entries through a registered provider-neutral emitter. Hosts configure enablement, batch size, polling interval, failure delay, shutdown behavior, stores, and concrete providers.

Hosts should avoid duplicate workers unless they intentionally design for multi-worker behavior and understand the storage/concurrency consequences.

### OpenTelemetry provider

`CDCavell.AsiBackbone.OpenTelemetry` is the first concrete governance emission provider package.

It implements `IAsiBackboneGovernanceEmitter` and projects governance envelopes into OpenTelemetry-friendly .NET diagnostics:

- `ActivitySource` activity events and tags;
- `Meter` counters and latency histograms;
- stable `asibackbone.*` attribute constants;
- provider-neutral delivered, failed, retryable, deferred, and dead-letter result behavior.

The provider does not configure exporters. It does not depend on Azure Monitor, Application Insights, Log Analytics, Event Hubs, Purview, Datadog, Grafana, Splunk, Elastic, SIEM, robotics, AI model, or cloud-provider SDK packages.

### Azure Monitor guidance

Azure Monitor should be reached through host-owned OpenTelemetry exporter configuration:

```text
CDCavell.AsiBackbone.OpenTelemetry
  -> ActivitySource / Meter
  -> host OpenTelemetry SDK pipeline
  -> host-configured Azure Monitor exporter
  -> Azure Monitor / Application Insights / Log Analytics
```

The OpenTelemetry provider should not hold Azure connection strings, instrumentation keys, workspace IDs, tenant IDs, or Azure SDK types.

### DLP and classification failure behavior

Core includes provider-neutral DLP/classification failure policy primitives so hosts can decide how governance emission should behave when classification is unavailable, times out, returns indeterminate results, blocks a payload, or reports a classified result.

Hosts should explicitly choose risk-sensitive behavior such as fail-open, fail-closed, defer, require acknowledgment, or escalate. Sensitive or unclassified payloads should not be emitted to external providers merely because a classifier is unavailable.

### Signing-ready receipts

Core includes signing-ready abstractions and metadata fields for audit receipts and downstream verification seams.

This is not a concrete signing implementation. `1.1.0` does not add Azure Key Vault, HSM, local signer, immutable storage, or production tamper-evidence behavior by default.

Accurate wording:

- records can carry signing-ready metadata;
- hosts or future provider packages can sign and verify precomputed artifact hashes;
- production tamper-evidence requires concrete signing, verification, protected key management, durable storage guarantees, retention policy, and operational procedures outside the Core seam.

## SemVer and compatibility

`1.1.0` is intended to be SemVer-compatible with `1.0.0` consumers.

Compatibility expectations:

- existing `1.0.0` package references can be upgraded to `1.1.0` without required source-code changes for consumers that do not use new features;
- new public APIs are additive;
- new provider package adoption is optional;
- new persisted fields and schema additions should be treated as additive migration work owned by the host;
- preview or future provider packages remain outside the stable compatibility promise until separately released as stable.

Hosts using EF Core should still review generated migrations before deployment because durable outbox and lifecycle records add storage surfaces that the host owns.

## Accepted deferrals

The following work is intentionally deferred or documentation/design-only for this release boundary:

| Area | `1.1.0` status |
| --- | --- |
| Event Hubs | Design documentation only. No Event Hubs SDK dependency or implementation package is included in the stable package family. |
| Purview | Governance and lineage enrichment strategy documentation only. No Purview SDK dependency or implementation package is included in the stable package family. |
| Azure Monitor | Supported through host-configured OpenTelemetry exporter guidance. No Azure Monitor-specific package is included. |
| Signing provider | Core signing-ready abstractions and metadata only. No concrete signing provider, Azure Key Vault adapter, HSM adapter, local signer, or key-management package is included. |
| Production tamper evidence | Not claimed by default. Requires concrete signing, verification, storage, retention, and operational controls implemented by the host or a future provider package. |
| Robotics / physical execution | Not implemented. Robotics remains a later gateway/scenario area and does not change the software package boundary. |

## Upgrade guidance

See [Upgrade Guide: 1.0.0 to 1.1.0](upgrade-100-to-110.md) for step-by-step guidance.

At a high level:

1. Upgrade existing stable packages from `1.0.0` to `1.1.0`.
2. Add `CDCavell.AsiBackbone.OpenTelemetry` only if provider emission is needed.
3. Prefer durable EF Core or another durable host-owned outbox store before provider emission.
4. Use in-memory stores and no-op emitters only for tests, samples, and local validation.
5. Configure Azure Monitor through the host OpenTelemetry pipeline if Azure Monitor is the selected backend.
6. Do not claim signing, immutability, or tamper-evidence unless a concrete signing/storage design is actually implemented.

## Validation expectations

Before tagging or publishing `1.1.0`, run release validation from a clean working tree and capture results in the release PR or release checklist.

Recommended commands:

```powershell
dotnet format AsiBackbone.slnx
dotnet build AsiBackbone.slnx -c Release
```

The most recent milestone validation captured during OpenTelemetry provider implementation reported a successful Release build and test run with 451 tests passed, 0 failed, and 0 skipped. Rerun validation against the final release candidate before tagging.

## Related documentation

- [Upgrade Guide: 1.0.0 to 1.1.0](upgrade-100-to-110.md)
- [Observability and Governance Emission Architecture](observability-and-governance-emission-architecture.md)
- [Governance Emission Contract](governance-emission-contract.md)
- [Durable Audit and Outbox Persistence](durable-audit-outbox-persistence.md)
- [Hosted Governance Outbox Drain](hosted-governance-outbox-drain.md)
- [OpenTelemetry Governance Emission Provider](opentelemetry-governance-emission-provider.md)
- [DLP and Classification Failure Policy](dlp-classification-failure-policy.md)
- [Signing-Ready Receipts and Key Handling](signing-ready-receipts-and-key-handling.md)
- [Event Hubs Governance Emission Provider Design](event-hubs-governance-emission-provider-design.md)
- [Purview Governance and Lineage Enrichment Strategy](purview-governance-lineage-enrichment-strategy.md)
- [API Compatibility and SemVer](api-compatibility-and-semver.md)
