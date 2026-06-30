# Performance Benchmark Baseline

This article documents the repeatable benchmark entry point for core AsiBackbone policy, endpoint governance, audit-residue, and outbox drain hot paths.

The benchmark baseline is measurement-first. It exists to help maintainers decide whether caching, pooling, short-circuit behavior, scoped-service changes, metadata handling, or other optimization work is justified by observed hot-path pressure. It should not be used as a substitute for unit tests, integration tests, or consumer-specific load testing.

## Benchmark entry point

The benchmark runner lives in:

```text
benchmarks/AsiBackbone.Benchmarks
```

Run it from the repository root with Release configuration:

```powershell
dotnet run -c Release --project benchmarks/AsiBackbone.Benchmarks -- --iterations 200000 --warmup 10000
```

Use smaller iteration counts when checking the benchmark path locally after code changes:

```powershell
dotnet run -c Release --project benchmarks/AsiBackbone.Benchmarks -- --iterations 1000 --warmup 100
```

Use `--help` to view runner options:

```powershell
dotnet run -c Release --project benchmarks/AsiBackbone.Benchmarks -- --help
```

The benchmark project is included in `AsiBackbone.slnx`, but the benchmark run remains manual. It is intentionally separate from normal CI unit tests so routine builds do not fail because of machine-specific timing variation.

## Current baseline scenarios

The runner captures latency and allocation measurements for representative Core, ASP.NET Core adapter, and outbox scenarios:

| Scenario | Purpose |
| --- | --- |
| `policy.zero_constraints` | Measures the empty policy-evaluation path. |
| `policy.all_allow_8` | Measures a common all-allow path with several constraints. |
| `policy.warning_and_denial_full` | Measures full aggregation with allow, warning, and denial results. |
| `policy.first_denial_short_circuit` | Measures the optional first-denial fast-abort path from issue #345. |
| `policy.acknowledgment_required` | Measures constraint evaluation followed by acknowledgment-required decision-policy composition. |
| `policy.escalation_recommended` | Measures constraint evaluation followed by escalation-recommended decision-policy composition. |
| `endpoint_governance.policy_allow` | Measures `DefaultAsiBackboneEndpointGovernanceService.EvaluateAsync` with a host policy evaluator returning allow. |
| `endpoint_governance.policy_warning` | Measures `DefaultAsiBackboneEndpointGovernanceService.EvaluateAsync` with a host policy evaluator returning warning. |
| `endpoint_governance.policy_deny` | Measures `DefaultAsiBackboneEndpointGovernanceService.EvaluateAsync` with a host policy evaluator returning deny. |
| `outbox_drain.small_batch_25` | Measures provider-neutral governance outbox drain processing for a small batch of 25 pending entries. |
| `outbox_drain.medium_batch_100` | Measures provider-neutral governance outbox drain processing for a medium batch of 100 pending entries. |
| `outbox_drain.scoped_medium_batch_100` | Measures DI scope creation, scoped `AsiBackboneGovernanceOutboxDrain` resolution, and medium-batch drain processing. |
| `audit_residue.from_decision` | Measures decision receipt / audit residue creation from a governance decision. |

The endpoint governance scenarios use test doubles for the host policy evaluator so the measured path stays focused on the ASP.NET Core adapter, metadata descriptor, request-correlation resolution, decision mapping, and safe allow/block result handling.

The outbox drain scenarios use provider-neutral fake outbox storage and the no-op governance emitter. They are designed to measure framework drain behavior and allocations without adding provider SDK, network, database, or exporter variability.

## Output fields

The runner prints a Markdown table containing:

- scenario name;
- scenario description;
- measurement iterations;
- mean nanoseconds per operation;
- allocated bytes per operation;
- total allocated bytes;
- Gen0 collection count;
- elapsed milliseconds.

It also prints runtime, operating-system, process-architecture, warmup, and measurement-iteration context so results can be interpreted later.

## Interpretation guidance

Benchmark results are for trend detection, not absolute guarantees.

Use results only when comparing:

- the same machine or comparable CI runner type;
- the same build configuration, preferably Release;
- the same .NET runtime family;
- comparable repository revisions;
- comparable iteration and warmup counts.

Do not use these numbers to promise consumer latency. Host applications should run their own benchmarks with their actual constraints, decision policies, persistence, middleware, logging, telemetry emitters, durable stores, network providers, and deployment topology.

Outbox drain benchmarks are especially sensitive to host-owned infrastructure. Real durable stores, provider SDKs, exporters, retry policies, batch sizes, row claiming, and network conditions can dominate drain runtime even if the provider-neutral drain path is lightweight.

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

## Related documentation

- [Policy Evaluator Pipeline](policy-evaluator-pipeline.md)
- [ASP.NET Core Endpoint Governance](aspnetcore-endpoint-governance.md)
- [Hosted Governance Outbox Drain](hosted-governance-outbox-drain.md)
- [Outbox Drain Reliability and Alerting](outbox-drain-reliability-and-alerting.md)
- [High-Throughput Host Service Guidance](high-throughput-host-services.md)
- [Audit Residue Observability Schema](audit-residue-observability-schema.md)
- [API Baseline and Boundary Checks](api-baseline-and-boundary-checks.md)
- [Release Validation](release-validation.md)
