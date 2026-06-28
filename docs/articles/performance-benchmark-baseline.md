# Performance Benchmark Baseline

This article documents the repeatable benchmark entry point for core AsiBackbone policy hot paths.

The benchmark baseline is measurement-first. It exists to help maintainers decide whether caching, pooling, short-circuit behavior, or other optimization work is justified by observed policy-pipeline pressure. It should not be used as a substitute for unit tests, integration tests, or consumer-specific load testing.

## Benchmark entry point

The benchmark runner lives in:

```text
benchmarks/AsiBackbone.Benchmarks
```

Run it from the repository root with Release configuration:

```powershell
dotnet run -c Release --project benchmarks/AsiBackbone.Benchmarks -- --iterations 200000 --warmup 10000
```

Use `--help` to view runner options:

```powershell
dotnet run -c Release --project benchmarks/AsiBackbone.Benchmarks -- --help
```

## Current baseline scenarios

The runner captures latency and allocation measurements for representative Core scenarios:

| Scenario | Purpose |
| --- | --- |
| `policy.zero_constraints` | Measures the empty policy-evaluation path. |
| `policy.all_allow_8` | Measures a common all-allow path with several constraints. |
| `policy.warning_and_denial_full` | Measures full aggregation with allow, warning, and denial results. |
| `policy.first_denial_short_circuit` | Measures the optional first-denial fast-abort path from issue #345. |
| `policy.acknowledgment_required` | Measures constraint evaluation followed by acknowledgment-required decision-policy composition. |
| `policy.escalation_recommended` | Measures constraint evaluation followed by escalation-recommended decision-policy composition. |
| `audit_residue.from_decision` | Measures decision receipt / audit residue creation from a governance decision. |

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

Do not use these numbers to promise consumer latency. Host applications should run their own benchmarks with their actual constraints, decision policies, persistence, middleware, logging, and deployment topology.

## Optimization decision rule

Before adding caching, pooling, shared mutable state, or specialized fast paths, capture benchmark output and document:

1. the scenario under pressure;
2. the before/after latency and allocation deltas;
3. whether the change improves common paths without making auditability or host-owned boundaries harder to reason about;
4. whether an existing option, such as `ShortCircuitOnFirstDenial`, is sufficient for the target workload.

This keeps optimization work evidence-driven and avoids adding complexity before the hot path is measured.

## Related issues

- Issue #345 introduced optional first-denial short-circuit behavior.
- Issue #362 introduced this benchmark baseline.

## Related documentation

- [Policy Evaluator Pipeline](policy-evaluator-pipeline.md)
- [Audit Residue Observability Schema](audit-residue-observability-schema.md)
- [API Baseline and Boundary Checks](api-baseline-and-boundary-checks.md)
- [Release Validation](release-validation.md)
