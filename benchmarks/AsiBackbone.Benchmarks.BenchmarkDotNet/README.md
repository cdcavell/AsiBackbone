# AsiBackbone.Benchmarks.BenchmarkDotNet

This project provides the BenchmarkDotNet allocation and latency baselines for AsiBackbone hot paths.

The benchmark classes are annotated with `MemoryDiagnoser`, so the summary output includes allocation data such as Gen0 activity and allocated bytes per operation. Use this project for optimization PR baselines and before/after comparisons.

## Run all hot-path baselines

From the repository root:

```powershell
dotnet run -c Release --project benchmarks/AsiBackbone.Benchmarks.BenchmarkDotNet -- --filter "*"
```

## Run focused groups

Outbox drain scenarios:

```powershell
dotnet run -c Release --project benchmarks/AsiBackbone.Benchmarks.BenchmarkDotNet -- --filter "*OutboxDrain*"
```

EF Core outbox claim batch and injected-latency scenarios:

```powershell
dotnet run -c Release --project benchmarks/AsiBackbone.Benchmarks.BenchmarkDotNet -- --filter "*EfCoreOutboxClaim*"
```

Endpoint governance scenarios:

```powershell
dotnet run -c Release --project benchmarks/AsiBackbone.Benchmarks.BenchmarkDotNet -- --filter "*EndpointGovernance*"
```

Policy evaluation scenarios:

```powershell
dotnet run -c Release --project benchmarks/AsiBackbone.Benchmarks.BenchmarkDotNet -- --filter "*Policy*"
```

Audit residue scenario:

```powershell
dotnet run -c Release --project benchmarks/AsiBackbone.Benchmarks.BenchmarkDotNet -- --filter "*AuditResidue*"
```

## Scenario coverage

The current BenchmarkDotNet baseline covers:

- `outbox_drain.small_batch_25`;
- `outbox_drain.medium_batch_100`;
- `outbox_drain.scoped_medium_batch_100`;
- `efcore_outbox.claim_pending_portable` with batch sizes 1, 10, 50, and 100 and injected per-command latency of 0 ms, 1 ms, and 5 ms;
- `endpoint_governance.policy_allow`;
- `endpoint_governance.policy_warning`;
- `endpoint_governance.policy_deny`;
- `audit_residue.from_decision`;
- `audit_residue.builder_no_metadata`;
- `audit_residue.builder_one_metadata`;
- `audit_residue.builder_many_metadata`;
- `policy.zero_constraints`;
- `policy.all_allow_8`;
- `policy.warning_and_denial_full`;
- `policy.first_denial_short_circuit`;
- `policy.acknowledgment_required`;
- `policy.escalation_recommended`.

## Interpretation

Use BenchmarkDotNet output for trend comparison on the same machine, runtime, build configuration, and repository revision. Prefer repeated runs or median-focused review before making optimization decisions.

The EF Core outbox claim benchmark seeds data outside the measured invocation and injects latency through a relational command interceptor. It verifies the uncontended portable command shape of one candidate query plus one read and one optimistic-concurrency update per successful claim. The injected delay isolates round-trip sensitivity but does not reproduce provider-specific locking, query plans, server load, or network jitter. See [EF Core Outbox Bulk-Claim Performance Evaluation](../../docs/articles/efcore-outbox-bulk-claim-evaluation.md) for the command-count model, provider-specific analysis, and production decision.

### Audit residue metadata allocation shape

The `audit_residue.builder_no_metadata`, `audit_residue.builder_one_metadata`, and `audit_residue.builder_many_metadata` scenarios intentionally exercise the fluent `AuditResidueBuilder` metadata path. The builder keeps its metadata storage lazy: the no-metadata path leaves the internal metadata dictionary unset, while the first metadata entry creates the builder dictionary. `Build()` then passes that metadata to `AuditResidue.Create`, where metadata is normalized, copied into a new ordinal dictionary, and wrapped as read-only residue metadata so the built value remains immutable and detached from later source or builder mutations.

Because of that shape, the first metadata item is expected to introduce a visible allocation step. A one-entry and small many-entry case may report the same allocated bytes on a given runtime because the builder dictionary and the normalized read-only metadata dictionary use small initial bucket/capacity sizes rather than allocating exactly one bucket per metadata item. Treat that plateau as expected unless repeated before/after BenchmarkDotNet runs show a meaningful reduction that preserves metadata normalization, immutability, and public API clarity.

For quick smoke checks while editing, the sibling `benchmarks/AsiBackbone.Benchmarks` project remains available as a lightweight manual runner, but optimization PRs should use this BenchmarkDotNet project as the allocation baseline.
