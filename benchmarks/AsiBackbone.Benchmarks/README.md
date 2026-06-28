# AsiBackbone.Benchmarks

This project provides a repeatable benchmark entry point for core AsiBackbone policy hot paths.

It is intentionally separate from the unit-test projects. The runner is for local or release-readiness measurement, not for pass/fail CI assertions.

## Run

From the repository root:

```powershell
dotnet run -c Release --project benchmarks/AsiBackbone.Benchmarks -- --iterations 200000 --warmup 10000
```

The runner prints a Markdown table with:

- scenario name;
- scenario description;
- measurement iterations;
- mean nanoseconds per operation;
- allocated bytes per operation;
- total allocated bytes;
- Gen0 collection count;
- elapsed milliseconds.

## Scenario coverage

The current baseline covers:

- zero-constraint policy evaluation;
- all-allow constraint evaluation;
- mixed warning/denial full aggregation;
- first-denial short-circuit evaluation;
- acknowledgment-required decision policy composition;
- escalation-recommended decision policy composition;
- audit residue creation from a governance decision.

## Interpretation

Benchmark results are useful for trend detection on the same machine, runtime, build configuration, and repository version. They are not absolute performance guarantees and should not be compared across unrelated machines without context.

Use this runner to decide whether proposed caching, pooling, short-circuit, or allocation-reduction work is justified by measured policy hot-path pressure.
