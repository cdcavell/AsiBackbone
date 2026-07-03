# AsiBackbone 2.2.1 Release Notes

`2.2.1` is a compatible patch release for the stable `2.x` AsiBackbone package family.

This release preserves the simplified `AsiBackbone.*` package IDs and public namespaces established in `2.0.0` while adding allocation-measurement infrastructure and reducing allocation pressure across the measured governance hot paths introduced and refined during the `2.2.x` line.

## Release summary

`2.2.1` is a patch release because it does not intentionally add new public package APIs or alter the package/namespace boundary. The release focuses on performance measurement, implementation hardening, tests, and release metadata alignment.

Existing `2.0.x`, `2.1.x`, and `2.2.0` consumers should be able to upgrade without required source-code changes for existing APIs.

## Added

* Added a BenchmarkDotNet benchmark project for allocation-sensitive governance hot paths.
* Added `MemoryDiagnoser` allocation baselines for policy evaluation, endpoint governance, outbox drain, scoped outbox drain, and audit residue creation.
* Added benchmark documentation with focused filters and profiling guidance for `dotnet-counters`, `dotnet-trace`, `dotnet-gcdump`, and PerfView.
* Added targeted tests for benchmark support and allocation-sensitive hot-path behavior.

## Changed

* Promotes central package version metadata from `2.2.0` to `2.2.1` while preserving `AssemblyVersion` as `2.0.0.0` for the compatible stable `2.x` line.
* Updates `FileVersion` to `2.2.1.0`.
* Updates `CITATION.cff` and `.zenodo.json` for the `2.2.1` release.
* Updates README, documentation home, article index, DocFX article navigation, release validation guidance, API compatibility / SemVer guidance, release notes, release readiness, and Source Link validation defaults for the `2.2.1` package family.

## Performance and allocation refinements

* Reduced provider-neutral outbox drain allocation pressure by avoiding unnecessary batch-copy allocations, avoiding retry merge allocation when only one source is drained, and replacing LINQ retry deduplication with direct loops.
* Reduced no-op governance emission and delivered outbox metadata re-normalization overhead while preserving outbox transition behavior.
* Reduced ASP.NET Core endpoint governance hot-path allocation by caching descriptor metadata, avoiding repeated metadata dictionary/string-join construction, and deferring fallback decision and actor-context creation until needed.
* Reduced policy evaluator allocation by reusing pass-through constraint results, avoiding eager warning/denial list allocation, and preserving decision-policy visibility without extra array-copy snapshots.
* Reduced audit residue decision-record allocation by reusing immutable decision/constraint reason-code collections, replacing hot-path enum `ToString()` calls with stable outcome-name helpers, and simplifying public reason-code normalization.

## Benchmark snapshot

Representative focused BenchmarkDotNet runs on Windows 11, .NET SDK `10.0.301`, .NET `10.0.9`, x64 RyuJIT AVX2 produced these post-change allocation baselines:

| Scenario | Mean | Allocated |
| --- | ---: | ---: |
| `outbox_drain.small_batch_25` | 2.612 us | 5.97 KB |
| `outbox_drain.medium_batch_100` | 10.407 us | 23.55 KB |
| `outbox_drain.scoped_medium_batch_100` | 10.883 us | 23.93 KB |
| `endpoint_governance.policy_allow` | 544.5 ns | 1.41 KB |
| `endpoint_governance.policy_warning` | 610.4 ns | 1.56 KB |
| `endpoint_governance.policy_deny` | 557.9 ns | 1.56 KB |
| `policy.zero_constraints` | 24.82 ns | 72 B |
| `policy.all_allow_8` | 61.19 ns | 72 B |
| `policy.warning_and_denial_full` | 164.91 ns | 408 B |
| `policy.first_denial_short_circuit` | 203.58 ns | 608 B |
| `policy.acknowledgment_required` | 99.50 ns | 408 B |
| `policy.escalation_recommended` | 103.65 ns | 408 B |
| `audit_residue.from_decision` | 211.8 ns | 592 B |

These values are local measurement snapshots, not consumer latency guarantees. Hosts should benchmark their own constraints, audit sinks, durable stores, emitters, logging, and deployment topology.

## Compatibility

No package ID or namespace changes are included.

No public API removals or renamed public APIs are intended.

`AssemblyVersion` remains `2.0.0.0` for the compatible stable `2.x` line.

This release keeps existing endpoint governance, outbox, policy evaluator, and audit residue semantics while reducing avoidable allocation overhead.

## Stable package family

The stable package set remains:

```text
AsiBackbone.Core
AsiBackbone.DependencyInjection
AsiBackbone.Storage.InMemory
AsiBackbone.EntityFrameworkCore
AsiBackbone.AspNetCore
AsiBackbone.Testing
AsiBackbone.Templates
AsiBackbone.Analyzers
AsiBackbone.OpenTelemetry
AsiBackbone.Signing.LocalDevelopment
AsiBackbone.Signing.ManagedKey
```

## Release boundary

`2.2.1` does not change the project boundary. AsiBackbone remains Accountable Systems Infrastructure for governed .NET decision flow: a governance spine, not an intelligence engine. See [Project Boundaries and Non-Claims](project-boundaries.md) for the full scope statement.

Event Hubs, Purview, Azure-specific SDK adapters, Aspire runtime packages, robotics, immutable-storage, and additional provider packages remain outside the stable package contract unless separately reviewed and released.

## Validation

Before tagging `v2.2.1`, the release candidate should pass the repository release gates, including:

* CI restore, build, formatting, tests, and coverage gates.
* Stable Release Validation.
* DocFX documentation build.
* Package creation.
* Generated NuGet metadata validation.
* Package SBOM generation.
* Template package smoke validation.
* External consumer smoke tests.
* Version consistency validation for `2.2.1`.
* Package/SBOM provenance handling where supported by the workflow event.

After packages are published and visible on NuGet, validate Source Link repository commit metadata with:

```powershell
./scripts/Validate-Source-Link-commit-metadata.ps1 -Version 2.2.1
```
