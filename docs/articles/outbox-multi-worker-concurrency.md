# Outbox Multi-Worker Concurrency Guidance

This article records the current concurrency review for the provider-neutral governance outbox drain and the EF Core-backed outbox store.

The goal is to help hosts avoid accidental duplicate emissions when an ASP.NET Core application is horizontally scaled and the hosted drain worker is registered in every replica.

## Summary decision

For the current release line, multi-worker outbox safety is handled through explicit documentation and host-owned deployment/storage patterns rather than a package-owned distributed lock manager.

The existing APIs are safe for single active worker usage and for local/test validation. They do not currently provide an atomic provider-neutral claim-before-emit operation.

A future release may add an explicit claim/lease abstraction, but that should be designed deliberately because it affects schema shape, migrations, provider-specific SQL, retry semantics, and host deployment operations.

## Repeatable validation path

Issue #311 adds a CI-friendly EF Core validation path for concurrent outbox/lifecycle writes, retryable drain failures, and drain-worker contention:

```text
tests/AsiBackbone.EntityFrameworkCore.Tests/EfCoreOutboxConcurrencyValidationTests.cs
```

The validation deliberately confirms both sides of the current reliability story:

- EF Core-backed local outbox and lifecycle records can be preserved under concurrent host-owned context writes in the tested SQLite relational path.
- Retryable provider failures remain represented as local outbox state and can be queried as retry-ready work.
- Two drain workers can still reach the same pending entry before final state is saved, so the current drain should not be described as exactly-once or duplicate-proof.

See [EF Core Outbox Concurrency Validation](../quality/ef-core-outbox-concurrency-validation.md) for the local command and interpretation guidance.

## Current behavior

| Area | Current behavior | Multi-worker implication |
| --- | --- | --- |
| `IAsiBackboneGovernanceOutboxStore.FindPendingAsync` | Returns pending entries ordered for delivery. | Selection only. It does not claim, lease, lock, or hide rows from another worker. |
| `IAsiBackboneGovernanceOutboxStore.FindRetryReadyAsync` | Returns retry-ready entries ordered for delivery. | Selection only. It does not prevent another worker from selecting the same entry. |
| `AsiBackboneGovernanceOutboxDrain` | Reads candidate entries, emits each envelope, then persists delivered/deferred/failed state. | Two workers can read the same candidate before either persists the final state. |
| `EfCoreGovernanceOutboxStore` | Uses EF Core persistence and configured concurrency tokens for state updates. | Optimistic concurrency helps detect conflicting saves, but it does not prevent duplicate provider calls before the save. |
| `InMemoryGovernanceOutboxStore` | Intended for tests, samples, and local validation. Same-entry status transitions use single-process compare-and-swap updates, and delivered/dead-lettered entries are terminal for later in-memory updates. | Single-process only. It is not durable, does not claim work, and does not model cross-replica concurrency. |
| Hosted drain worker | Runs wherever it is registered and enabled. | In scaled deployments, each replica may run a worker unless the host disables or coordinates it. |

## What optimistic concurrency does and does not solve

The EF Core persistence configuration marks `ConcurrencyStamp` as a concurrency token. That is useful for detecting stale updates when two contexts try to save incompatible state transitions for the same row.

However, the drain flow performs provider emission before the final delivered/failed state is saved. Optimistic concurrency at save time cannot undo a provider call that already happened. In other words:

```text
worker A reads pending row
worker B reads same pending row
worker A emits to provider
worker B emits to provider
worker A saves delivered
worker B may hit a concurrency conflict or overwrite attempt
```

Even if the second save fails, the second provider emission may already have occurred. Treat optimistic concurrency as state-protection, not as duplicate-emission protection.

## Recommended host patterns

### 1. Single active worker per durable outbox partition

For most hosts, the safest default is one active drain worker per shared durable outbox partition.

Common deployment patterns include:

- run the web/API replicas with `AsiBackboneGovernanceOutboxDrainWorkerOptions.Enabled = false`;
- run one dedicated worker process, job, container, or app service instance with the worker enabled;
- use platform leader election or a singleton scheduler if the hosting platform provides it;
- ensure only one replica has permission or configuration to drain a given outbox partition.

This is the recommended pattern unless the host has designed and tested a multi-worker claim strategy.

### 2. Partitioned workers

Multiple workers can be safe when each worker owns a disjoint outbox partition. Partitions can be based on tenant, region, workload, provider path, shard, or another host-owned routing key.

Partitioning must be enforced in the durable selection query or storage adapter. Merely giving workers different names is not enough if they still read the same pending rows.

### 3. Host-owned claim or lease before provider emission

A multi-worker durable store should claim work before calling the provider.

A typical host-owned claim pattern includes:

- claim fields such as `ClaimedBy`, `ClaimedUtc`, and `ClaimExpiresUtc`;
- an atomic update from unclaimed/retry-ready to claimed;
- a lease expiration policy so abandoned claims can be retried;
- provider-specific locking or update semantics in the same database transaction;
- a final delivered/failed/deferred/dead-letter transition that verifies the claim owner where appropriate.

This package does not prescribe those columns or migration steps in the current release line. The host owns the database schema, migrations, and provider behavior.

### 4. Provider-side idempotency

Even with a claim strategy, downstream providers should be treated as at-least-once targets unless the provider and host have a verified exactly-once model.

Use stable identifiers where available:

- `GovernanceEmissionEnvelope.EnvelopeId`;
- `GovernanceOutboxEntry.OutboxEntryId`;
- source event or audit residue identifiers;
- provider idempotency keys, when the provider supports them;
- provider record IDs returned after delivery.

Provider idempotency is especially important for retry, recovery, replay, and manual re-drain operations.

## Provider-specific SQL patterns

Provider-specific locking and skip-locked semantics should remain host/provider-specific unless a future package explicitly targets a provider.

Examples that hosts may evaluate in their own infrastructure include:

- PostgreSQL-style `SELECT ... FOR UPDATE SKIP LOCKED`;
- SQL Server patterns using `UPDLOCK`, `READPAST`, and appropriate transaction isolation;
- database-specific atomic `UPDATE ... OUTPUT` / `RETURNING` claim statements;
- cloud queue visibility timeouts or lease-based message claims.

These patterns are useful, but they are not provider-neutral. They also require testing with the host's actual database provider, isolation level, indexes, retry policy, and migration process.

## Worker configuration guidance

`AddAsiBackboneGovernanceOutboxDrainWorker` should be treated as an operational registration. In multi-replica applications, do not assume it becomes singleton across replicas.

Recommended configuration posture:

```csharp
builder.Services.Configure<AsiBackboneGovernanceOutboxDrainWorkerOptions>(options =>
{
    options.Enabled = builder.Configuration.GetValue<bool>("AsiBackbone:OutboxDrain:Enabled");
    options.BatchSize = 100;
    options.PollingInterval = TimeSpan.FromSeconds(30);
});
```

Then set `AsiBackbone:OutboxDrain:Enabled` to `true` only for the selected worker role or selected partition owner.

## Operational checks

Hosts should monitor for signals that may indicate accidental duplicate workers or unsafe claim behavior:

- more worker heartbeats than expected for a partition;
- duplicate provider records with the same envelope or outbox entry identifier;
- repeated EF Core concurrency exceptions during outbox state transitions;
- provider throttling caused by duplicate drain attempts;
- delivered records without the expected provider record IDs;
- rising retry/dead-letter counts after a scale-out event.

If duplicate workers are discovered, disable extra workers first, then inspect provider-side duplicates and outbox state before replaying records.

## Wording boundary

Do not describe the current provider-neutral outbox drain as exactly-once delivery.

A safer description is:

> AsiBackbone provides durable local outbox records and provider-neutral drain primitives. Hosts that run multiple workers against the same durable store must add host-owned claiming, partitioning, or provider-side idempotency to avoid duplicate emissions.

This keeps the package boundary clear and avoids overstating delivery guarantees.

## Future design considerations

A future claim/lease feature should be evaluated as a deliberate API and storage design, not as an incidental change to the current find-and-drain API.

Open design questions include:

- whether claim APIs belong in Core or a durable-store-specific abstraction;
- how to represent claim owner, lease expiration, and abandoned claims;
- whether a new `Claimed` or `InProgress` status is needed;
- how to avoid breaking existing migrations and host-owned schemas;
- whether EF Core can provide a provider-neutral implementation or only guidance;
- how provider idempotency keys should flow into downstream emitters;
- how tests should model real database concurrency instead of only in-memory concurrency.

Until that design exists, production multi-replica hosts should use a single active worker, partitioned workers, or host-owned claim/lease behavior.
