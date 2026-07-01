# AsiBackbone 2.2.0 Release Notes

`2.2.0` is a compatible minor release for the stable `2.x` AsiBackbone package family.

This release preserves the simplified `AsiBackbone.*` package IDs and public namespaces established in `2.0.0` while adding an opt-in reduced endpoint-governance metadata mode and incorporating hot-path allocation refinements from the endpoint governance and Core decision model review.

## Release summary

`2.2.0` is a minor release because it adds backward-compatible public API through `AsiBackboneEndpointGovernanceMetadataMode` and `AsiBackboneEndpointGovernanceOptions.MetadataMode`. Existing `2.1.x` consumers should not need package ID, namespace, or required source-code migration changes for existing APIs.

## Added

* Added `AsiBackboneEndpointGovernanceMetadataMode` with default `Full` behavior and opt-in `Reduced` behavior.
* Added `AsiBackboneEndpointGovernanceOptions.MetadataMode` so high-throughput ASP.NET Core hosts can choose how much endpoint metadata is forwarded through governance evaluation, audit residue, acknowledgment challenge metadata, and development diagnostics.
* Added focused tests for default full endpoint metadata, reduced descriptor metadata, reduced policy-evaluator metadata, and reduced development diagnostic metadata.
* Added release and documentation navigation entries for the `2.2.0` release notes and readiness record.

## Changed

* Promotes central package version metadata from `2.1.1` to `2.2.0` while preserving `AssemblyVersion` as `2.0.0.0` for the compatible stable `2.x` line.
* Updates `FileVersion` to `2.2.0.0`.
* Updates `CITATION.cff` and `.zenodo.json` for the `2.2.0` release.
* Updates README, documentation home, article index, DocFX article navigation, release validation guidance, API compatibility / SemVer guidance, release notes, release readiness, and Source Link validation defaults for the `2.2.0` package family.
* Documents the tradeoff between default full endpoint-governance metadata traceability and reduced metadata allocation behavior.

## Performance and allocation refinements

* Reduced repeated optional dependency resolution in the endpoint governance hot path by caching optional policy evaluator, capability validator, and audit sink lookups within a single evaluation.
* Reduced avoidable allocation churn in `GovernanceDecision.NormalizeReasons` while preserving fallback reason behavior, null filtering, and read-only reason exposure.
* Added reduced endpoint-governance metadata mode for hosts that have measured metadata dictionary construction as meaningful overhead and can safely operate with only `endpoint.operation_name` in metadata payloads.

## Fixed

* Preserved safe default endpoint-governance diagnostics by keeping full metadata behavior as the default.
* Preserved existing public `GovernanceDecision` semantics while reducing internal normalization overhead.
* Preserved host-owned execution, audit, capability validation, and acknowledgment boundaries while adding the reduced metadata option.

## Compatibility

Existing stable `2.0.x` and `2.1.x` consumers should be able to upgrade to `2.2.0` without required source-code changes for existing APIs.

No package ID or namespace changes are included.

No public API removals or renamed public APIs are intended.

`AssemblyVersion` remains `2.0.0.0` for the compatible stable `2.x` line.

The new endpoint-governance reduced metadata behavior is opt-in. The default `Full` metadata mode preserves existing diagnostic and traceability behavior.

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

`2.2.0` does not change the project boundary. AsiBackbone remains Accountable Systems Infrastructure for governed .NET decision flow: a governance spine, not an intelligence engine. See [Project Boundaries and Non-Claims](project-boundaries.md) for the full scope statement.

Event Hubs, Purview, Azure-specific SDK adapters, Aspire runtime packages, robotics, immutable-storage, and additional provider packages remain outside the stable package contract unless separately reviewed and released.

## Validation

Before tagging `v2.2.0`, the release candidate should pass the repository release gates, including:

* CI restore, build, formatting, tests, and coverage gates.
* Stable Release Validation.
* DocFX documentation build.
* Package creation.
* Generated NuGet metadata validation.
* Package SBOM generation.
* Template package smoke validation.
* External consumer smoke tests.
* Version consistency validation for `2.2.0`.
* Package/SBOM provenance handling where supported by the workflow event.

After packages are published and visible on NuGet, validate Source Link repository commit metadata with:

```powershell
./scripts/Validate-Source-Link-commit-metadata.ps1 -Version 2.2.0
```
