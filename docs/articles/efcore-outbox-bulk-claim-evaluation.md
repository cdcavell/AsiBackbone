# EF Core Outbox Bulk-Claim Performance Evaluation

Issue: [#589](https://github.com/cdcavell/AsiBackbone/issues/589)

Status: **Evaluation complete. Keep the portable per-row implementation as the production baseline. Do not add provider-specific raw SQL to `EfCoreGovernanceOutboxStore`.**

## Purpose

This record evaluates whether `EfCoreGovernanceOutboxStore` should replace or supplement its provider-neutral per-row optimistic claim path with a set-based claim operation.

The investigation covers:

* batch sizes of 1, 10, 50, and 100;
* local and injected-latency execution;
* provider-neutral `ExecuteUpdateAsync` feasibility;
* SQL Server and PostgreSQL-style atomic claim patterns;
* claim ordering, eligibility, lease, token, attempt-count, and competing-worker guarantees;
* the package boundary for any future optimized implementation.

This is a performance investigation, not a correctness repair. The existing implementation remains valid and intentionally favors portable, explicit lease semantics.

## Current portable claim path

For an uncontended successful pending batch, the current flow is:

1. query an ordered candidate-ID batch;
2. re-read each candidate by outbox entry ID;
3. recheck eligibility and lease state;
4. create a unique claim token and increment `ClaimAttemptCount`;
5. save one optimistic-concurrency update;
6. return only successful claim winners.

The retry-ready path uses the same claim loop after applying its retry eligibility and ordering rules.

The ordering rules remain:

* pending: `CreatedUtc`, then `OutboxEntryId`;
* retry-ready: `NextRetryUtc ?? UpdatedUtc`, then `OutboxEntryId`.

The second read is not redundant from a correctness perspective. It narrows the stale-candidate window by rechecking eligibility immediately before the concurrency-checked update.

## Confirmed relational command shape

The integration coverage in `EfCoreGovernanceOutboxClaimRoundTripTests` runs the portable path against relational SQLite and verifies the uncontended command count.

For `N` successful claims:

```text
commands = 1 candidate query + N row reads + N row updates
         = 1 + 2N
```

| Requested batch | Confirmed database commands |
| ---: | ---: |
| 1 | 3 |
| 10 | 21 |
| 50 | 101 |
| 100 | 201 |

Under contention, a candidate that becomes ineligible before its second read may consume only the read. A candidate that loses during its concurrency-checked update still consumes both per-row commands. The table therefore describes the stable all-success baseline used for latency comparison.

## Latency sensitivity

The command count makes network latency the dominant scaling factor for a remote relational database. The following table is a latency-only floor derived from the confirmed command count. It excludes query execution, serialization, EF Core materialization, context creation, and server load.

| Batch | Commands | Added floor at 1 ms/command | Latency-only ceiling at 1 ms | Added floor at 5 ms/command | Latency-only ceiling at 5 ms |
| ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 3 | 3 ms | 333.3 claims/s | 15 ms | 66.7 claims/s |
| 10 | 21 | 21 ms | 476.2 claims/s | 105 ms | 95.2 claims/s |
| 50 | 101 | 101 ms | 495.0 claims/s | 505 ms | 99.0 claims/s |
| 100 | 201 | 201 ms | 497.5 claims/s | 1,005 ms | 99.5 claims/s |

The asymptotic throughput ceiling is approximately `1 / (2L)` claims per unit time, where `L` is per-command latency. Larger batches amortize the one candidate query, but they do not remove the two commands per successful row.

These are not universal production benchmark numbers. They are the network-latency component implied by the verified command shape. Hosts should measure with their own provider, connection topology, isolation configuration, indexes, and contention level.

## BenchmarkDotNet harness

`EfCoreOutboxClaimBenchmarks` adds a repeatable BenchmarkDotNet scenario using relational SQLite plus a command interceptor that injects 0 ms, 1 ms, or 5 ms before each measured database command.

The benchmark:

* covers batch sizes 1, 10, 50, and 100;
* seeds an uncontended ordered pending batch outside the measured invocation;
* verifies all requested claims are won;
* verifies the measured command count remains `1 + 2N`;
* reports elapsed time and allocations for same-machine trend comparison.

Run only this group from the repository root:

```powershell
dotnet run -c Release --project benchmarks/AsiBackbone.Benchmarks.BenchmarkDotNet -- --filter "*EfCoreOutboxClaim*"
```

BenchmarkDotNet output should be compared on the same machine, runtime, repository revision, and build configuration. The injected delay isolates round-trip sensitivity; it does not emulate provider locking, transaction log pressure, query-plan differences, or real network jitter.

## Provider-neutral `ExecuteUpdateAsync` evaluation

`ExecuteUpdateAsync` is useful when a caller only needs to apply one set of values to a translated query and receive an affected-row count. It does not provide the complete portable primitive required by the outbox claim contract.

A correct bulk claim must atomically:

1. select a bounded ordered eligible set;
2. skip or resolve rows held by competing workers;
3. assign a unique claim token to every winning row;
4. assign a new concurrency stamp to every winning row;
5. increment each row's existing attempt count;
6. return the actual winning rows and their generated claim metadata;
7. preserve enough ordering information to return claims deterministically.

The provider-neutral limitations are:

* EF Core does not expose a cross-provider ordered, bounded update-and-return contract.
* `ExecuteUpdateAsync` returns an affected-row count, not the updated entities needed to construct `GovernanceOutboxClaim` values.
* client-evaluated claim-token values would normally be evaluated once for the statement, which would not preserve per-row token uniqueness.
* database-side UUID/random functions and update-returning syntax are provider-specific.
* a bulk update followed by a second read would reintroduce an ownership race unless the operation remained in a carefully controlled provider-specific transaction.
* skip-locked and lock-hint behavior is not portable through the base EF Core API.

A provider-neutral set-based branch would therefore either weaken current guarantees or hide provider-specific behavior behind a misleadingly portable surface.

## SQL Server prototype evaluation

A viable SQL Server adapter could use an updateable ordered CTE with `UPDLOCK`, `READPAST`, an appropriate isolation mode, and `OUTPUT`:

```sql
;WITH candidates AS
(
    SELECT TOP (@maxCount) *
    FROM dbo.AsiBackboneGovernanceOutboxEntries
         WITH (UPDLOCK, READPAST, ROWLOCK)
    WHERE Status = @pendingStatus
      AND (ClaimToken IS NULL OR ClaimExpiresUtc IS NULL OR ClaimExpiresUtc <= @utcNowTicks)
    ORDER BY CreatedUtc, OutboxEntryId
)
UPDATE candidates
SET ClaimOwner = @workerId,
    ClaimToken = CONVERT(nvarchar(36), NEWID()),
    ClaimedUtc = @utcNowTicks,
    ClaimExpiresUtc = @claimExpiresUtcTicks,
    ClaimAttemptCount = ClaimAttemptCount + 1,
    ConcurrencyStamp = CONVERT(nvarchar(36), NEWID()),
    UpdatedUtc = @utcNowTicks
OUTPUT inserted.*;
```

This can reduce the claim acquisition to one server round trip while producing actual winners. However, production correctness depends on details that must be tested against SQL Server itself:

* lock hints and transaction isolation, including read-committed snapshot behavior;
* the exact updateable-CTE plan and index use;
* starvation behavior under sustained `READPAST` contention;
* deadlock and retry handling;
* ordering of the `OUTPUT` result, which is not guaranteed and may require client-side reordering;
* retry-ready predicate and nullable ordering equivalence;
* token and concurrency-stamp format compatibility;
* cancellation and transaction cleanup.

The pattern is technically promising but is not provider-neutral.

## PostgreSQL prototype evaluation

A PostgreSQL adapter could use a locked candidate CTE and `UPDATE ... RETURNING`:

```sql
WITH candidates AS
(
    SELECT id
    FROM "AsiBackboneGovernanceOutboxEntries"
    WHERE "Status" = @pendingStatus
      AND ("ClaimToken" IS NULL OR "ClaimExpiresUtc" IS NULL OR "ClaimExpiresUtc" <= @utcNowTicks)
    ORDER BY "CreatedUtc", "OutboxEntryId"
    FOR UPDATE SKIP LOCKED
    LIMIT @maxCount
)
UPDATE "AsiBackboneGovernanceOutboxEntries" AS entry
SET "ClaimOwner" = @workerId,
    "ClaimToken" = gen_random_uuid()::text,
    "ClaimedUtc" = @utcNowTicks,
    "ClaimExpiresUtc" = @claimExpiresUtcTicks,
    "ClaimAttemptCount" = entry."ClaimAttemptCount" + 1,
    "ConcurrencyStamp" = gen_random_uuid()::text,
    "UpdatedUtc" = @utcNowTicks
FROM candidates
WHERE entry."Id" = candidates.id
RETURNING entry.*;
```

This is also capable of returning partial winners safely while avoiding locked rows. It still requires provider integration coverage for extension availability, isolation, ordering, starvation, retry-ready equivalence, and result reordering.

## SQLite assessment

SQLite supports set-based updates and returning clauses in modern versions, but its database-level write serialization does not provide the same skip-locked row-claim model as SQL Server or PostgreSQL. A SQLite-specific bulk path would add complexity without addressing the main high-volume multi-worker scenario. The portable optimistic path remains the appropriate SQLite baseline.

## Correctness requirements for any future optimized store

A provider-specific implementation must pass the same semantic contract as the portable store:

| Requirement | Required behavior |
| --- | --- |
| Eligibility | Re-evaluate status, retry readiness, and lease expiration in the atomic claim statement. |
| Ordering | Select the same bounded order and return claims in that order after any provider result reordering. |
| Partial winners | Return only rows actually acquired by the worker. |
| Claim token | Generate a unique opaque token per winning row. |
| Attempt count | Increment exactly once for every successful acquisition or reacquisition. |
| Lease timestamps | Store normalized UTC claim and expiration values derived from the request. |
| Concurrency stamp | Replace the stamp on every winning update. |
| Competing workers | Prevent two workers from both receiving a valid claim for the same lease instance. |
| Completion | Continue requiring owner/token verification for final transitions. |
| Failure behavior | Roll back or return no ambiguous claims when the atomic statement fails. |

Provider-specific race tests must run against the real provider. SQLite tests cannot prove SQL Server lock-hint or PostgreSQL skip-locked behavior.

## Package-boundary decision

Do not branch on provider name inside `EfCoreGovernanceOutboxStore`.

If a provider-optimized claim path is later justified, prefer a separate implementation or package, for example:

```text
AsiBackbone.EntityFrameworkCore.SqlServer
AsiBackbone.EntityFrameworkCore.PostgreSql
```

The optimized store should implement `IAsiBackboneGovernanceOutboxClaimStore`, remain explicitly registered by the host, and fall back conceptually—not silently at runtime—to the portable store when the provider-specific guarantees are unavailable.

This keeps:

* raw SQL out of the portable package;
* provider dependencies optional;
* transaction and isolation behavior explicit;
* correctness tests scoped to the database engine that supplies the guarantee;
* the current implementation available as the safe baseline.

## Optimization threshold

Provider complexity is justified only when representative real-provider testing demonstrates all of the following:

* at least a 2x sustained throughput improvement for batches of 50 or 100 under the target network latency;
* materially lower p95 claim latency under competing workers;
* no duplicate claim winners in stress testing;
* no loss of ordering, retry eligibility, token uniqueness, attempt counts, lease expiration, or concurrency-stamp behavior;
* acceptable starvation, deadlock, and cancellation behavior;
* a production host or provider package with a demonstrated need for the additional maintenance surface.

The exact threshold may be raised for a provider package with a small user base because raw SQL, engine-version support, and concurrency testing create ongoing maintenance obligations.

## Decision

The current investigation does not justify a production bulk path in the provider-neutral store.

The accepted outcome is:

1. retain the existing per-row optimistic-concurrency implementation;
2. keep its exact command shape covered by relational integration tests;
3. provide the latency-injection BenchmarkDotNet harness for host-specific measurement;
4. document SQL Server and PostgreSQL set-based approaches as provider-specific future options;
5. require a separate optimized store or package rather than branching inside the base store;
6. introduce no raw SQL or production behavior change for this issue.

This closes the performance investigation while preserving a measurable path for a future provider-backed optimization when real workload evidence warrants it.
