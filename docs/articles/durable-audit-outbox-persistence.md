# Durable Audit and Outbox Persistence

This article documents the provider-neutral durable persistence seam for audit residue, lifecycle events, and governance emission outbox entries.

In this software project, **ASI** means **Accountable Systems Infrastructure**. AsiBackbone remains a governance spine for consequential software decision flow. It is not an AI model host, observability backend, SIEM product, cloud provider, or completed ASI implementation.

## Purpose

External telemetry and governance sinks should be downstream delivery targets, not the first system of record.

The durable persistence seam preserves a local accountability artifact before any optional provider attempts delivery.

```text
Governed decision
  -> AuditResidue / AuditResidueLifecycleEvent
  -> local audit/lifecycle store
  -> GovernanceEmissionEnvelope
  -> governance outbox
  -> optional provider emitter
```

If provider delivery fails, the local audit or lifecycle record remains available for review, retry, escalation, or dead-letter handling.

## Core-neutral contracts

Core defines provider-neutral contracts and models only:

| Type | Purpose |
| --- | --- |
| `IAsiBackboneAuditLedgerStore` | Existing neutral audit ledger abstraction for `AuditLedgerRecord` records. |
| `IAsiBackboneAuditResidueLifecycleStore` | Neutral lifecycle event store for append-only audit residue lifecycle events. |
| `IAsiBackboneGovernanceOutboxStore` | Neutral outbox store for pending governance emission envelopes. |
| `GovernanceOutboxEntry` | Neutral outbox entry state model with status, retry count, last error, next retry time, provider identifiers, and dead-letter reason. |
| `AsiBackboneGovernanceOutboxDrain` | Provider-neutral drain path that hands pending or retry-ready outbox entries to an `IAsiBackboneGovernanceEmitter` and persists the resulting state transition. |
| `NoOpGovernanceEmitter` | No-op test/dev emitter that acknowledges envelopes as delivered without sending data to an external provider. |

Core does not reference Azure Monitor, Event Hubs, Purview, OpenTelemetry, SIEM SDKs, robotics packages, AI model packages, or cloud-provider SDKs.

## In-memory development stores

`CDCavell.AsiBackbone.Storage.InMemory` includes development and test stores:

| Type | Purpose |
| --- | --- |
| `InMemoryAuditResidueLifecycleStore` | Stores lifecycle events in memory for tests, samples, and local development. |
| `InMemoryGovernanceOutboxStore` | Stores governance outbox entries in memory for tests, samples, and local development. |

These stores are intentionally not durable across process restarts. Production hosts should use EF Core or another host-owned durable storage adapter.

## No-op drain proof path

The no-op drain path exists to prove the outbox handoff before a real provider is added:

```text
GovernanceEmissionEnvelope
  -> IAsiBackboneGovernanceOutboxStore
  -> AsiBackboneGovernanceOutboxDrain
  -> NoOpGovernanceEmitter
  -> GovernanceEmissionResult.Delivered
  -> delivered outbox state
```

`NoOpGovernanceEmitter` is not a production emission provider. It is a local validation seam for tests, samples, and smoke flows that need to prove pending -> delivered behavior without OpenTelemetry, Azure Monitor, Event Hubs, Purview, SIEM, or another external service.

The drain path also normalizes retryable, deferred, failed, and terminal provider-neutral results back into outbox state so the local accountability record is preserved when provider handoff does not succeed.

## Outbox status model

Outbox entries use the provider-neutral `GovernanceEmissionStatus` vocabulary:

| Status | Meaning |
| --- | --- |
| `Pending` | The envelope is saved locally and ready for delivery. |
| `Delivered` | A downstream provider accepted or delivered the emission. |
| `Deferred` | Delivery is intentionally delayed by host or provider policy. |
| `Failed` | Delivery failed and retryability is not implied by status. |
| `RetryableFailure` | Delivery failed but is expected to be retryable. |
| `DeadLettered` | Delivery reached a terminal state or policy quarantine. |

`GovernanceOutboxEntry` also tracks:

* `RetryCount`
* `MaxRetryCount`
* `NextRetryUtc`
* `LastError`
* `ProviderName`
* `ProviderRecordId`
* `DeadLetterReason`
* minimized metadata

## Failure handling

Provider failures should be normalized into `GovernanceEmissionError` before updating the outbox. The host can then decide whether to retry, defer, dead-letter, or escalate.

Recommended sequence:

1. Save the audit residue or lifecycle event locally.
2. Create a `GovernanceEmissionEnvelope`.
3. Enqueue the envelope into `IAsiBackboneGovernanceOutboxStore`.
4. Attempt optional provider emission through `AsiBackboneGovernanceOutboxDrain`.
5. Mark the outbox entry delivered, failed, retryable, deferred, or dead-lettered.

This avoids losing the local accountability record when external sinks are unavailable.

## Privacy and provider boundaries

Outbox entries should contain minimized provider-neutral metadata only. Do not store raw capability tokens, secrets, connection strings, provider credentials, raw prompts, protected document bodies, or unredacted sensitive records in the outbox payload or metadata.

Use opaque identifiers, hashes, policy versions, policy hashes, lifecycle stage values, correlation identifiers, and trace identifiers that are safe under host policy.

## Related documentation

- [Governance Emission Contract](governance-emission-contract.md)
- [Observability and Governance Emission Architecture](observability-and-governance-emission-architecture.md)
- [Audit Residue Observability Schema](audit-residue-observability-schema.md)
- [EF Core Integration Boundary](ef-core-integration-boundary.md)
