# Outbox Claim and Lease Design Record

This design record captures the selected direction for multi-worker governance outbox claim and lease support.

Issue: [#407](https://github.com/cdcavell/AsiBackbone/issues/407)

Status: **Accepted design direction; implementation deferred to a future minor release unless separately scoped.**

## Context

The current provider-neutral governance outbox drain reads pending or retry-ready entries, emits each envelope to a provider, and then saves the resulting delivered, deferred, failed, retryable-failure, or dead-lettered state.

That behavior is appropriate for a single active worker or a host that already partitions workers so each worker reads a disjoint durable outbox slice. It is not sufficient by itself for multiple workers reading the same durable outbox rows.

The risk is duplicate provider emission:

```text
worker A selects row 123
worker B selects row 123
worker A emits to provider
worker B emits to provider
worker A saves delivered
worker B may fail on concurrency or stale-state checks
```

Optimistic concurrency can protect the persisted row from some conflicting final-state writes, but it cannot undo a provider call that already occurred. A claim-before-emit strategy is therefore a separate reliability concern from final-state concurrency protection.

## Decision

Claim and lease support should be treated as an explicit durable outbox capability, not as a silent behavior change to the existing `FindPendingAsync`, `FindRetryReadyAsync`, or drain APIs.

The selected direction is:

1. Keep current single-worker selection APIs supported.
2. Add future opt-in claim/lease contracts in Core so the concept remains provider-neutral.
3. Implement concrete claim/lease behavior in durable storage packages, starting with EF Core only when its provider-neutral limits are clearly documented.
4. Allow provider-specific packages or host-owned adapters to implement stronger atomic claim patterns where the database/provider supports them.
5. Continue requiring provider-side idempotency guidance because claim/lease improves duplicate selection risk but does not create universal exactly-once delivery.

This preserves existing package behavior while creating a path for scaled durable drains.

## Proposed future contract shape

A future claim-capable outbox store should be additive. It should not replace `IAsiBackboneGovernanceOutboxStore` for hosts that only need local tests, samples, or one active worker.

Potential Core contract:

```csharp
public interface IAsiBackboneGovernanceOutboxClaimStore : IAsiBackboneGovernanceOutboxStore
{
    ValueTask<IReadOnlyList<GovernanceOutboxClaim>> ClaimPendingAsync(
        GovernanceOutboxClaimRequest request,
        CancellationToken cancellationToken = default);

    ValueTask<IReadOnlyList<GovernanceOutboxClaim>> ClaimRetryReadyAsync(
        GovernanceOutboxClaimRequest request,
        CancellationToken cancellationToken = default);

    ValueTask<GovernanceOutboxEntry> CompleteClaimAsync(
        GovernanceOutboxClaim claim,
        GovernanceEmissionResult result,
        CancellationToken cancellationToken = default);

    ValueTask ReleaseClaimAsync(
        GovernanceOutboxClaim claim,
        string? reason = null,
        CancellationToken cancellationToken = default);
}
```

The exact API names may change during implementation, but the important design boundary is that claim operations are explicit. A host should know when it has opted into a claim-capable durable path.

## Candidate model fields

The durable outbox row will likely need these additional fields if claim support is implemented in EF Core:

| Field | Purpose |
| --- | --- |
| `ClaimedBy` | Stable worker, process, node, or partition owner identifier. |
| `ClaimedUtc` | UTC timestamp when the claim was acquired. |
| `ClaimExpiresUtc` | UTC timestamp when the lease expires and another worker may reclaim. |
| `ClaimToken` | Opaque value used to verify the same claim owner during final state transition. |
| `ClaimAttemptCount` | Optional operational counter for repeated claims/reclaims. |

`ClaimToken` is preferred over relying only on `ClaimedBy` because worker names can be reused across process restarts. A token allows finalization to prove that the completing worker still holds the same lease instance.

## Status model decision

Do **not** add a provider-neutral `Claimed` or `InProgress` status as the first design step.

The current `GovernanceEmissionStatus` values describe provider-neutral emission state: pending, delivered, deferred, failed, retryable failure, and dead-lettered. A claim is a lease over a row, not necessarily an emission state. Keeping claim as separate metadata avoids broadening status semantics and reduces migration/API churn.

A future `Claimed` or `InProgress` status may still be considered if evidence shows it materially improves diagnostics, operational queries, or provider integration. That should be a separate API review because it would affect stable status semantics and documentation.

## Claim lifecycle

The expected claim lifecycle is:

```text
select eligible pending/retry-ready row
  -> atomically write claim owner, claim token, claimed time, and expiration
  -> emit to provider only after claim succeeds
  -> complete final transition only when claim token still matches
  -> clear claim fields on delivered/deferred/failed/retryable/dead-letter transition
```

A worker that cannot obtain a claim must not emit that entry.

If a worker crashes after claiming but before completing, the row becomes eligible again after `ClaimExpiresUtc`. The reclaiming worker must still treat downstream provider emission as at-least-once unless the provider accepts and enforces an idempotency key.

## EF Core implementation direction

EF Core should expose a portable claim-capable implementation only if the operation can be expressed safely enough for the supported provider set.

A minimal EF Core implementation could use optimistic concurrency:

1. Query eligible unclaimed or expired rows.
2. Attempt to update claim fields with the current `ConcurrencyStamp`.
3. Save changes.
4. Return only rows whose claim update succeeded.

That approach is portable but may be less efficient than provider-specific SQL. It reduces duplicate provider calls only when workers respect the claim result and emit after claim success.

Provider-specific implementations may be better for high-throughput drains:

| Provider family | Possible host/provider-specific pattern |
| --- | --- |
| SQL Server | `UPDATE TOP (...) ... OUTPUT` with `UPDLOCK`, `READPAST`, and appropriate transaction isolation. |
| PostgreSQL | `SELECT ... FOR UPDATE SKIP LOCKED` or `UPDATE ... RETURNING` patterns. |
| Cloud queues | Visibility timeout or native lease semantics. |

These stronger patterns are intentionally provider-specific. They should live in provider-specific packages or host-owned adapters unless a portable EF Core implementation is proven sufficient.

## Completion and ownership verification

Claim completion should verify the claim owner/token before writing delivered, failed, retryable, deferred, or dead-lettered state.

A final transition should fail or no-op when:

* the claim token does not match;
* the claim has expired and another worker has reclaimed the row;
* the row is already terminal;
* the row is not in a status that can be completed by the claim path.

This prevents an old worker from overwriting a newer worker's outcome after a lease expiration/reclaim race.

## Delivery guarantees vocabulary

Documentation and API naming should keep the following terms distinct:

| Term | Meaning |
| --- | --- |
| At-least-once delivery | The same provider emission may be attempted more than once. Hosts and providers must tolerate duplicates. |
| Idempotent delivery | Duplicate attempts use stable identifiers so the provider or host can collapse duplicates into one effective outcome. |
| Claim/lease | Workers coordinate selection so only a claim holder should emit a given row during a lease window. |
| Exactly-once delivery | A full end-to-end guarantee that a provider observes one and only one effective delivery. AsiBackbone should not claim this generically. |

Claim/lease support can reduce duplicate selection risk. It does not by itself prove exactly-once delivery because provider behavior, network failure, transaction scope, and replay/recovery policy remain outside the package boundary.

## Compatibility and migration boundaries

Claim support should be introduced as a backward-compatible additive feature when possible:

* Existing single-worker drains should continue to use the current APIs.
* New claim-capable APIs should be opt-in.
* EF Core claim fields should require an explicit host migration.
* Hosts that do not add claim columns should remain able to use the existing durable outbox model.
* Release notes should identify claim support as a minor release feature unless a breaking schema/API decision is made.

The package should not silently change an existing host's durable table shape or deployment behavior without documentation and migration guidance.

## Recommended next implementation sequence

1. Add Core claim request/result/claim-token models and a claim-capable store interface.
2. Add an opt-in drain path that prefers `IAsiBackboneGovernanceOutboxClaimStore` when configured.
3. Add EF Core claim fields and indexes behind explicit migrations owned by the host.
4. Add EF Core tests for two workers racing to claim the same row before provider emission.
5. Add docs and samples showing one active worker, partitioned workers, and claim-capable multi-worker drains.
6. Preserve provider-side idempotency guidance in all multi-worker examples.

## Rejected alternatives

### Silent claim inside existing find methods

Rejected. It would surprise existing consumers because methods named `FindPendingAsync` and `FindRetryReadyAsync` would mutate durable state.

### Claim status only

Rejected as the first step. A status-only design does not prove ownership during completion and risks conflating row lease state with provider emission state.

### Provider-neutral exactly-once guarantee

Rejected. Exactly-once delivery requires provider participation, durable idempotency semantics, and failure-mode proof beyond the package boundary.

### Package-owned distributed lock manager

Rejected for the current direction. Distributed locking is infrastructure-specific and can become a hidden deployment dependency. Hosts should explicitly choose platform locking, provider-specific claims, partitioning, or a single active worker.

## Documentation outcome

Until claim/lease support is implemented, the recommended production guidance remains:

* use one active worker per durable outbox partition;
* use disjoint partitions when running multiple workers;
* use host-owned claim/lease behavior if scaling drain workers against the same durable rows;
* use provider-side idempotency keys wherever the downstream provider supports them.

This design record records the target direction without overstating current runtime behavior.
