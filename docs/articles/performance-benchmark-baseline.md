# Performance Benchmark Baseline

This article documents the repeatable benchmark entry points for core AsiBackbone policy, endpoint governance, audit-residue, decision/result normalization, and outbox drain hot paths.

The benchmark baseline is measurement-first. It exists to help maintainers decide whether caching, pooling, short-circuit behavior, scoped-service changes, metadata handling, or other optimization work is justified by observed hot-path activity. It should not be used as a substitute for unit tests, integration tests, or consumer-specific load testing.

## Benchmark entry points

The repository now has two benchmark entry points:

| Project | Purpose |
| --- | --- |
| `benchmarks/AsiBackbone.Benchmarks.BenchmarkDotNet` | Primary optimization baseline with BenchmarkDotNet `MemoryDiagnoser` allocation output. |
| `benchmarks/AsiBackbone.Benchmarks` | Lightweight manual runner for quick smoke checks and local trend checks. |

Use the BenchmarkDotNet runner for optimization PR before/after evidence.

```powershell
dotnet run -c Release --project benchmarks/AsiBackbone.Benchmarks.BenchmarkDotNet -- --filter "*"
```

Use focused filters when measuring one hot area:

```powershell
# Decision construction with no, one, and multiple reason paths
dotnet run -c Release --project benchmarks/AsiBackbone.Benchmarks.BenchmarkDotNet -- --filter "*Decision*"

# OperationResult success/failure reason normalization paths
dotnet run -c Release --project benchmarks/AsiBackbone.Benchmarks.BenchmarkDotNet -- --filter "*OperationResult*"

# Outbox drain batch 25, batch 100, and scoped batch 100
dotnet run -c Release --project benchmarks/AsiBackbone.Benchmarks.BenchmarkDotNet -- --filter "*OutboxDrain*"

# ASP.NET Core endpoint governance allow, warning, and deny paths
dotnet run -c Release --project benchmarks/AsiBackbone.Benchmarks.BenchmarkDotNet -- --filter "*EndpointGovernance*"

# Policy evaluation zero, simple, mixed, acknowledgment, escalation, and exception-as-denial paths
dotnet run -c Release --project benchmarks/AsiBackbone.Benchmarks.BenchmarkDotNet -- --filter "*Policy*"

# Constraint exception-as-denial path only
dotnet run -c Release --project benchmarks/AsiBackbone.Benchmarks.BenchmarkDotNet -- --filter "*ConstraintException*"

# Audit residue creation from a governance decision and builder metadata variants
dotnet run -c Release --project benchmarks/AsiBackbone.Benchmarks.BenchmarkDotNet -- --filter "*AuditResidue*"
```

The manual runner remains useful for quick checks while editing:

```powershell
dotnet run -c Release --project benchmarks/AsiBackbone.Benchmarks -- --iterations 200000 --warmup 10000
```

Use smaller iteration counts when checking the manual path locally after code changes:

```powershell
dotnet run -c Release --project benchmarks/AsiBackbone.Benchmarks -- --iterations 1000 --warmup 100
```

The benchmark projects are included in `AsiBackbone.slnx`, but benchmark runs remain manual. They are intentionally separate from normal CI unit tests so routine builds do not fail because of machine-specific timing variation.

## Current baseline scenarios

The BenchmarkDotNet runner captures latency and allocation measurements for representative Core, ASP.NET Core adapter, audit, decision/result normalization, and outbox scenarios:

| Scenario | Purpose |
| --- | --- |
| `decision.allow_no_reasons` | Measures direct allowed decision construction with no reason collection. |
| `decision.deny_one_reason` | Measures direct denied decision construction with one prebuilt reason. |
| `decision.deny_multiple_reasons` | Measures direct denied decision construction and reason-code projection for multiple reasons. |
| `decision.escalate_one_reason` | Measures direct escalation-recommended decision construction. |
| `operation_result.success_no_reasons` | Measures successful `OperationResult` construction with no reasons or warnings. |
| `operation_result.failure_one_reason` | Measures failed `OperationResult` construction with one prebuilt reason. |
| `operation_result.failure_multiple_reasons` | Measures failed `OperationResult` reason normalization and reason-code projection with multiple reasons. |
| `policy.zero_constraints` | Measures the empty policy-evaluation path. |
| `policy.all_allow_8` | Measures a common all-allow path with several constraints. |
| `policy.warning_and_denial_full` | Measures full aggregation with allow, warning, and denial results. |
| `policy.first_denial_short_circuit` | Measures the optional first-denial fast-abort path from issue #345. |
| `policy.acknowledgment_required` | Measures constraint evaluation followed by acknowledgment-required decision-policy composition. |
| `policy.escalation_recommended` | Measures constraint evaluation followed by escalation-recommended decision-policy composition. |
| `policy.constraint_exception_as_denial` | Measures the opt-in fail-closed constraint exception path that emits a synthetic denied decision. |
| `endpoint_governance.policy_allow` | Measures `DefaultAsiBackboneEndpointGovernanceService.EvaluateAsync` with a host policy evaluator returning allow. |
| `endpoint_governance.policy_warning` | Measures `DefaultAsiBackboneEndpointGovernanceService.EvaluateAsync` with a host policy evaluator returning warning. |
| `endpoint_governance.policy_deny` | Measures `DefaultAsiBackboneEndpointGovernanceService.EvaluateAsync` with a host policy evaluator returning deny. |
| `outbox_drain.small_batch_25` | Measures provider-neutral governance outbox drain processing for a small batch of 25 pending entries. |
| `outbox_drain.medium_batch_100` | Measures provider-neutral governance outbox drain processing for a medium batch of 100 pending entries. |
| `outbox_drain.scoped_medium_batch_100` | Measures DI scope creation, scoped `AsiBackboneGovernanceOutboxDrain` resolution, and medium-batch drain processing. |
| `audit_residue.from_decision` | Measures decision receipt / audit residue creation from a governance decision. |
| `audit_residue.builder_no_metadata` | Measures fluent audit residue builder construction with no metadata supplied. |
| `audit_residue.builder_one_metadata` | Measures fluent audit residue builder construction with one metadata entry. |
| `audit_residue.builder_many_metadata` | Measures fluent audit residue builder construction with multiple metadata entries. |

The endpoint governance scenarios use test doubles for the host policy evaluator so the measured path stays focused on the ASP.NET Core adapter, metadata descriptor, request-correlation resolution, decision mapping, and safe allow/block result handling.

The outbox drain scenarios use provider-neutral fake outbox storage and the no-op governance emitter. They are designed to measure framework drain behavior and allocations without adding provider SDK, network, database, or exporter variability.

## Output fields

BenchmarkDotNet output includes timing statistics and memory columns. With `MemoryDiagnoser`, the important columns are:

- `Mean`: average time per operation for the selected benchmark job;
- `Median`: midpoint time value when present in the generated report or artifacts;
- `Gen0`: generation 0 collection activity normalized by BenchmarkDotNet;
- `Allocated`: allocated bytes per operation.

The lightweight manual runner prints a Markdown table containing:

- scenario name;
- scenario description;
- measurement iterations;
- mean nanoseconds per operation;
- allocated bytes per operation;
- total allocated bytes;
- Gen0 collection count;
- elapsed milliseconds.

Both runners also print runtime, operating-system, process-architecture, warmup, and measurement context so results can be interpreted later.

## Interpretation guidance

Benchmark results are for trend detection, not absolute guarantees.

Use results only when comparing:

- the same machine or comparable CI runner type;
- the same build configuration, preferably Release;
- the same .NET runtime family;
- comparable repository revisions;
- comparable benchmark filters, iteration settings, and warmup settings.

Prefer at least three repeated BenchmarkDotNet runs before deciding that a small delta is meaningful. When results are noisy, use the median or repeated-run direction instead of a single run's best or worst number.

Do not use these numbers to promise consumer latency. Host applications should run their own benchmarks with their actual constraints, decision policies, persistence, middleware, logging, telemetry emitters, durable stores, network providers, and deployment topology.

Outbox drain benchmarks are especially sensitive to host-owned infrastructure. Real durable stores, provider SDKs, exporters, retry policies, batch sizes, row claiming, and network conditions can dominate drain runtime even if the provider-neutral drain path is lightweight.

## Threat-contributor exception metadata allocation

Issue #486 reviewed the metadata dictionary allocated when a threat model contributor throws and `TreatThreatContributorExceptionAsDenial` converts that failure into a denied governance decision. This remains a profiling-gated failure path, not a routine evaluator hot path.

No code optimization is warranted without evidence that threat-contributor exceptions are frequent enough to dominate incident traffic. The current allocation is intentionally local to the single denied decision so the evaluator does not introduce shared mutable metadata, pooling complexity, or public API churn for an exceptional path.

Preserve the current safety boundary when revisiting this area:

- keep the public reason code and message configured by evaluator options;
- include only bounded diagnostic metadata such as contributor identity and exception type;
- do not copy exception messages, stack traces, secrets, raw payloads, prompts, protected content, or user input into decisions;
- preserve logger behavior for operational diagnostics, where the exception object is available to the logging pipeline;
- prefer fixing, disabling, circuit-breaking, or isolating unstable host-owned contributors before optimizing framework metadata construction.

Reopen optimization work only after BenchmarkDotNet output, `dotnet-trace`, `dotnet-counters`, `dotnet-gcdump`, or production-equivalent host profiling shows this exception path is materially hot for a target workload. If that happens, add a focused benchmark or trace evidence first, then consider a low-allocation representation only if `OperationReason` and the decision model can support it without public API churn.

## Allocation profiling plan

Start with BenchmarkDotNet `MemoryDiagnoser` output. If `Allocated`, `Gen0`, or run-to-run variance points to a hot scenario, move to process-level profiling.

### dotnet-counters

Use counters for a live view of allocation rate and GC activity while a focused benchmark is running:

```powershell
dotnet-counters monitor --process-id <PID> System.Runtime
```

Watch `alloc-rate`, `gen-0-gc-count`, `gc-heap-size`, and `% time in GC` while running focused filters such as `*OutboxDrain*` or `*EndpointGovernance*`.

### dotnet-trace

Use trace collection when allocation pressure needs call-stack evidence:

```powershell
dotnet-trace collect --process-id <PID> --providers Microsoft-Windows-DotNETRuntime:0x1C000080018:5 --output artifacts/perf/asi-backbone-hotpath.nettrace
```

Open the `.nettrace` file in Visual Studio, PerfView, or another trace viewer and inspect allocation stacks for the focused benchmark process.

### dotnet-gcdump

Use a GC dump when the question is what object types dominate the managed heap during a long or focused run:

```powershell
dotnet-gcdump collect --process-id <PID> --output artifacts/perf/asi-backbone-hotpath.gcdump
```

Then inspect the dump with Visual Studio or another GC dump viewer.

### PerfView

On Windows, PerfView can collect GC allocation stacks for deeper investigation:

```powershell
PerfView.exe /AcceptEula /NoGui /Collect:GCCollectOnly /MaxCollectSec:120 /DataFile:artifacts/perf/asi-backbone-hotpath.etl.zip collect
```

Use PerfView when BenchmarkDotNet allocation output identifies a scenario but `dotnet-trace` does not give enough allocation-stack detail.

## Optimization decision rule

Before adding caching, pooling, shared mutable state, or specialized fast paths, capture benchmark output and document:

1. the scenario under pressure;
2. the before/after latency and allocation deltas;
3. whether the change improves common paths without making auditability or host-owned boundaries harder to reason about;
4. whether an existing option, such as `ShortCircuitOnFirstDenial`, is sufficient for the target workload;
5. whether the measured bottleneck is framework orchestration or host-owned I/O, storage, telemetry, or provider behavior.

This keeps optimization work evidence-driven and avoids adding complexity before the hot path is measured.

## Related issues

- Issue #345 introduced optional first-denial short-circuit behavior.
- Issue #362 introduced the initial benchmark baseline.
- Issue #383 extended the baseline to endpoint governance and outbox drain paths.
- Issue #394 added BenchmarkDotNet allocation baselines and this profiling plan.
- Issue #456 extended BenchmarkDotNet coverage for decision/result normalization, audit residue builder metadata paths, and exception-as-denial evaluation.
- Issue #486 documented the profiling-gated decision to leave threat-contributor exception metadata allocation local to the exceptional denial path.

## Related documentation

- [Policy Evaluator Pipeline](policy-evaluator-pipeline.md)
- [ASP.NET Core Endpoint Governance](aspnetcore-endpoint-governance.md)
- [Hosted Governance Outbox Drain](hosted-governance-outbox-drain.md)
- [Outbox Drain Reliability and Alerting](outbox-drain-reliability-and-alerting.md)
- [High-Throughput Host Service Guidance](high-throughput-host-services.md)
- [Audit Residue Observability Schema](audit-residue-observability-schema.md)
- [API Baseline and Boundary Checks](api-baseline-and-boundary-checks.md)
- [Release Validation](release-validation.md)
