# AsiBackbone 2.1.0 Release Notes

`2.1.0` is a compatible minor release for the stable `2.x` AsiBackbone package family.

This release preserves the simplified `AsiBackbone.*` package IDs and public namespaces established in `2.0.0` while adding opt-in policy-pipeline ergonomics, audit-residue construction helpers, benchmark guidance, documentation examples, and in-memory outbox hardening.

## Release summary

`2.1.0` is a minor release because it includes backward-compatible public API and adoption-surface expansion. Existing `2.0.x` consumers should not need package ID, namespace, or required source-code migration changes for existing APIs.

## Added

* Added `AsiBackbonePolicyEvaluatorOptions.ShortCircuitOnFirstDenial`, defaulting to `false`, so existing full constraint aggregation remains the default audit-friendly behavior.
* Added ASP.NET Core endpoint metadata support for the optional first-denial fast-abort preference through attribute, route-builder extension, endpoint descriptor, and descriptor metadata surfaces.
* Added `AuditResidueBuilder` as a fluent construction path for complex audit residue values while preserving existing immutable `AuditResidue.Create`, `FromDecision`, and `FromConstraint` factories.
* Added a benchmark console project under `benchmarks/AsiBackbone.Benchmarks` for repeatable policy hot-path latency and allocation trend checks.
* Added custom decision-policy examples covering strict deny-wins composition, warning preservation, regional overlays, acknowledgment-required outcomes, gateway readiness checks, and latency-sensitive orchestration.
* Added release and documentation navigation entries for the new benchmark and decision-policy guidance.

## Changed

* Promotes central package version metadata from `2.0.2` to `2.1.0` while preserving `AssemblyVersion` as `2.0.0.0` for the compatible stable `2.x` line.
* Updates `FileVersion` to `2.1.0.0`.
* Updates `CITATION.cff` and `.zenodo.json` for the `2.1.0` release.
* Updates README, documentation home, article index, DocFX article navigation, release validation guidance, API compatibility / SemVer guidance, changelog, and Source Link validation defaults for the `2.1.0` package family.
* Replaces stale alpha-era wording in current policy-evaluator documentation with stable package-family wording.

## Fixed

* Hardened in-memory governance outbox transition behavior so same-entry state transitions remain terminal and compare-and-swap style updates do not accidentally overwrite delivered or dead-lettered records.
* Consolidated repeated project-boundary disclaimers around the canonical Project Boundaries and Non-Claims guidance.
* Clarified release cadence and readiness guidance for patch, minor, and major release classification.
* Refreshed documentation branding/navigation alignment after the `2.0.2` package-icon correction.

## Compatibility

Existing stable `2.0.0`, `2.0.1`, and `2.0.2` consumers should be able to upgrade to `2.1.0` without required source-code changes for existing APIs.

No package ID or namespace changes are included.

No public API removals or renamed public APIs are intended.

`AssemblyVersion` remains `2.0.0.0` for the compatible stable `2.x` line.

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

`2.1.0` does not change the project boundary. AsiBackbone remains Accountable Systems Infrastructure for governed .NET decision flow: a governance spine, not an intelligence engine. See [Project Boundaries and Non-Claims](project-boundaries.md) for the full scope statement.

Event Hubs, Purview, Azure-specific SDK adapters, Aspire runtime packages, robotics, immutable-storage, and additional provider packages remain outside the stable package contract unless separately reviewed and released.

## Validation

Before tagging `v2.1.0`, the release candidate should pass the repository release gates, including:

* CI restore, build, formatting, tests, and coverage gates.
* Stable Release Validation.
* DocFX documentation build.
* Package creation.
* Generated NuGet metadata validation.
* Package SBOM generation.
* Template package smoke validation.
* External consumer smoke tests.
* Version consistency validation for `2.1.0`.
* Package/SBOM provenance handling where supported by the workflow event.

After packages are published and visible on NuGet, validate Source Link repository commit metadata with:

```powershell
./scripts/Validate-Source-Link-commit-metadata.ps1 -Version 2.1.0
```
