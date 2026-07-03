# AsiBackbone 2.2.1 Release Readiness Record

This record tracks the release-candidate posture for `2.2.1`, a compatible patch release on the stable `2.x` package family.

## Release intent

`2.2.1` promotes measurement-backed hot-path allocation refinements into the current stable `2.2.x` line.

The release preserves the `2.0.0` public package and namespace boundary. It does not intentionally remove or rename public APIs, rename packages, alter namespaces, or add a new public API surface.

## Release classification

| Field | Value |
| --- | --- |
| Release | `2.2.1` |
| Type | Compatible patch |
| Stable line | `2.x` |
| Assembly identity | `2.0.0.0` |
| File version | `2.2.1.0` |
| Primary purpose | Benchmark-backed performance and allocation hardening |
| Public API expansion | No |
| Runtime behavior change | Existing semantics preserved; implementation hot paths refined |
| Package ID changes | No |
| Namespace changes | No |

## Included release surfaces

- BenchmarkDotNet allocation baseline project is present and runnable.
- BenchmarkDotNet `MemoryDiagnoser` reports allocation baselines for policy, endpoint governance, outbox drain, and audit residue scenarios.
- Outbox drain hot-path allocation reductions preserve pending/retry-ready behavior, delivery, failure, deferred, dead-letter, and cancellation paths.
- Endpoint governance descriptor metadata caching preserves full/reduced metadata behavior and existing endpoint governance semantics.
- Policy evaluator hot-path optimization preserves warning aggregation, denial precedence, short-circuit behavior, and decision-policy visibility.
- Audit residue optimization preserves outcome naming, reason-code fidelity, metadata defensive copying, timestamp handling, and optional field normalization.
- Release notes include benchmark snapshot values and explicitly frame them as local measurement snapshots, not consumer latency guarantees.

## Required release-candidate checks

Before tagging `v2.2.1`, confirm:

- `Directory.Build.props` uses `VersionPrefix` `2.2.1`.
- `AssemblyVersion` remains `2.0.0.0`.
- `FileVersion` is `2.2.1.0`.
- `CITATION.cff` references `2.2.1`.
- `.zenodo.json` references `2.2.1`.
- `2.2.1` release notes exist and identify the release as a compatible patch release.
- `CHANGELOG.md` includes a `2.2.1` entry.
- README, documentation home, article index, DocFX article TOC, release validation, and API compatibility / SemVer guidance reference `2.2.1` where current-release guidance is expected.
- Source Link post-publish validation defaults to `2.2.1`.
- Release notes state that no package ID or namespace changes are included.
- Release notes state that existing `2.0.x`, `2.1.x`, and `2.2.0` consumers should be able to upgrade without required source-code changes for existing APIs.
- CI passes on the release-candidate commit.
- Stable Release Validation passes on the release-candidate commit.
- Package metadata validation passes for generated `.nupkg` artifacts.
- Package SBOM generation passes for generated `.nupkg` artifacts.
- Template package smoke validation passes.
- External consumer smoke tests pass.
- DocFX documentation build passes.

## Compatibility notes

Existing `2.0.x`, `2.1.x`, and `2.2.0` consumers should be able to upgrade to `2.2.1` without required source-code changes for existing APIs.

`2.2.1` is a patch release focused on benchmark-backed implementation hardening, allocation reduction, tests, documentation, and release metadata alignment. Event Hubs, Purview, Azure-specific SDK adapters, Aspire runtime packages, robotics, immutable-storage, and additional provider packages remain outside the stable package contract unless separately reviewed and released.

## Post-publish checks

After packages are published and visible on NuGet, validate Source Link repository commit metadata:

```powershell
./scripts/Validate-Source-Link-commit-metadata.ps1 -Version 2.2.1
```

If package metadata, Source Link metadata, SBOM artifacts, or provenance artifacts are incorrect after publish, document the failure and prepare a follow-up patch rather than attempting to overwrite immutable NuGet package metadata.
