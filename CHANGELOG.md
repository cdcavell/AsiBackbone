# Changelog

All notable changes to this project are documented in this file.

This project follows the spirit of [Keep a Changelog](https://keepachangelog.com/) and adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [2.2.1] - 2026-07-03

### Release summary

`2.2.1` is a compatible patch release for the stable `2.x` package family.

This release preserves the `2.0.0` public package and namespace boundary while adding BenchmarkDotNet allocation baselines and reducing allocation pressure across outbox drain, endpoint governance, policy evaluation, and audit residue decision-record hot paths. No public API expansion, package ID change, or namespace change is intended.

### Added

* Added BenchmarkDotNet allocation baselines for policy evaluation, endpoint governance, outbox drain, scoped outbox drain, and audit residue creation.
* Added benchmark documentation and profiling guidance for repeatable allocation measurement.
* Added focused tests for allocation-sensitive hot-path behavior and branch coverage preservation.
* Added release notes and a release readiness record for the `2.2.1` release.

### Changed

* Promoted central package version metadata from `2.2.0` to `2.2.1` while preserving `AssemblyVersion` as `2.0.0.0`.
* Updated `FileVersion` to `2.2.1.0`.
* Updated `CITATION.cff` and `.zenodo.json` for the `2.2.1` release.
* Updated README, documentation home, article index, DocFX navigation, release validation, API compatibility / SemVer guidance, release notes, release readiness guidance, and Source Link validation defaults.

### Performance

* Reduced allocation pressure in provider-neutral outbox drain batch handling.
* Reduced endpoint governance hot-path allocations through descriptor metadata caching and deferred fallback object creation.
* Reduced policy evaluator allocation overhead by reusing pass-through constraint results and avoiding eager reason-list allocation.
* Reduced audit residue decision-record allocation by reusing immutable reason-code collections and replacing hot-path enum formatting with stable outcome-name helpers.

### Compatibility notes

* Existing stable `2.0.x`, `2.1.x`, and `2.2.0` consumers should be able to upgrade to `2.2.1` without required source-code changes.
* `2.2.1` is a patch release because it focuses on benchmark-backed implementation hardening, allocation reduction, tests, documentation, and release metadata alignment.
* `AssemblyVersion` remains `2.0.0.0` for the compatible stable `2.x` line.


## [2.2.0] - 2026-07-01

### Release summary

`2.2.0` is a compatible minor release for the stable `2.x` package family.

This release preserves the `2.0.0` public package and namespace boundary while adding opt-in endpoint-governance reduced metadata mode and incorporating endpoint governance and Core decision hot-path allocation refinements.

### Added

* Added `AsiBackboneEndpointGovernanceMetadataMode` with default `Full` behavior and opt-in `Reduced` behavior.
* Added `AsiBackboneEndpointGovernanceOptions.MetadataMode` so high-throughput ASP.NET Core hosts can forward reduced endpoint metadata through governance evaluation, audit residue, acknowledgment challenge metadata, and development diagnostics.
* Added focused tests for default full metadata, reduced descriptor metadata, reduced evaluator metadata, and reduced development diagnostic metadata.
* Added release notes and a release readiness record for the `2.2.0` release.

### Changed

* Promoted central package version metadata from `2.1.1` to `2.2.0` while preserving `AssemblyVersion` as `2.0.0.0`.
* Updated `FileVersion` to `2.2.0.0`.
* Updated `CITATION.cff` and `.zenodo.json` for the `2.2.0` release.
* Updated README, documentation home, article index, DocFX navigation, release validation, API compatibility / SemVer guidance, release notes, release readiness guidance, and Source Link validation defaults.

### Performance

* Reduced repeated optional dependency resolution in endpoint governance evaluation.
* Reduced avoidable allocation churn in `GovernanceDecision.NormalizeReasons`.
* Added reduced endpoint-governance metadata mode for measured high-throughput production paths that can safely operate with only `endpoint.operation_name` metadata.

### Compatibility notes

* Existing stable `2.0.x` and `2.1.x` consumers should be able to upgrade to `2.2.0` without required source-code changes.
* `2.2.0` is a minor release because it includes backward-compatible public APIs and options.
* `AssemblyVersion` remains `2.0.0.0` for the compatible stable `2.x` line.

## [2.1.1] - 2026-06-28

### Fixed
- Updated the package README image markup for NuGet.org compatibility.
- Replaced unsupported HTML image/alignment tags with standard Markdown image syntax so the social preview image renders correctly on the NuGet package page instead of appearing as literal HTML text.
- Adjusted the README image reference to use a NuGet-compatible hosted image URL.

## [2.1.0] - 2026-06-28

### Release summary

`2.1.0` is a compatible minor release for the stable `2.x` package family.

This release preserves the `2.0.0` public package and namespace boundary while adding backward-compatible policy-pipeline ergonomics, audit-residue construction helpers, benchmark guidance, custom decision-policy examples, documentation alignment, and in-memory outbox hardening.

### Added

* Added `AsiBackbonePolicyEvaluatorOptions.ShortCircuitOnFirstDenial`, defaulting to `false`, so existing full constraint aggregation remains the default audit-friendly behavior.
* Added ASP.NET Core endpoint metadata support for the optional first-denial fast-abort preference through attribute, route-builder extension, endpoint descriptor, and descriptor metadata surfaces.
* Added `AuditResidueBuilder` as a fluent construction path for complex audit residue values while preserving existing immutable `AuditResidue.Create`, `FromDecision`, and `FromConstraint` factories.
* Added an isolated benchmark console project for core policy-pipeline measurement under `benchmarks/AsiBackbone.Benchmarks`.
* Added benchmark scenarios for policy evaluation, policy composition, and audit residue creation.
* Added custom decision-policy examples covering strict deny-wins composition, warning preservation, regional/local overlays, acknowledgment-required outcomes, gateway readiness checks, and latency-sensitive orchestration.
* Added release notes and a release readiness record for the `2.1.0` release.

### Changed

* Promoted central package version metadata from `2.0.2` to `2.1.0` while preserving `AssemblyVersion` as `2.0.0.0` for the compatible stable `2.x` line.
* Updated `FileVersion` to `2.1.0.0`.
* Updated `CITATION.cff` and `.zenodo.json` for the `2.1.0` release.
* Updated README, documentation home, article index, DocFX article navigation, release validation, API compatibility / SemVer guidance, release notes, release readiness guidance, and Source Link validation defaults for the `2.1.0` package family.
* Replaced stale alpha-era wording in current policy-evaluator documentation with stable package-family wording.

### Fixed

* Hardened in-memory governance outbox transition behavior so same-entry state transitions remain terminal and compare-and-swap style updates do not accidentally overwrite delivered or dead-lettered records.
* Consolidated repeated project-boundary disclaimers around the canonical Project Boundaries and Non-Claims guidance.
* Clarified release cadence and readiness guidance for patch, minor, and major release classification.
* Refreshed documentation branding/navigation alignment after the `2.0.2` package-icon correction.

### Validation

* Release-candidate validation is expected to pass through CI, Stable Release Validation, package metadata validation, template package smoke validation, external consumer smoke tests, stable package integration smoke tests, DocFX build, package SBOM generation, and artifact provenance handling before tagging.
* Version-consistency validation should pass for `2.1.0`, including `Directory.Build.props`, `CITATION.cff`, `.zenodo.json`, optional tag `v2.1.0`, and generated package filenames when package artifacts are supplied.
* After packages are published and visible on NuGet, Source Link repository commit metadata should be validated with:

```powershell
./scripts/Validate-Source-Link-commit-metadata.ps1 -Version 2.1.0
```

### Compatibility notes

* Existing stable `2.0.0`, `2.0.1`, and `2.0.2` consumers should be able to upgrade to `2.1.0` without required source-code changes for existing APIs.
* `2.1.0` is a minor release because it includes backward-compatible public APIs, options, developer-experience tooling, tests, and documentation alignment.
* `AssemblyVersion` remains `2.0.0.0` for the compatible stable `2.x` line.
* `FileVersion` should be updated to `2.1.0.0`.
* Package `Version` and `InformationalVersion` should be updated to `2.1.0`.
* Event Hubs, Purview, Azure-specific SDK adapters, Aspire runtime packages, robotics, immutable-storage, and additional provider packages remain outside the stable package contract unless separately reviewed and released.

## [2.0.2] - 2026-06-26

### Release summary

`2.0.2` is a compatible patch release for the stable `2.0.x` package family.

This release corrects the package-facing icon presentation issue discovered after `2.0.1`. The previous package icon asset could render incompletely in package-list and package-detail contexts. `2.0.2` preserves the `2.0.0` public package and namespace boundary while aligning release metadata and documentation for the corrected package presentation release.

### Fixed

* Corrected the package icon asset used for NuGet and repository/package presentation.
* Rebuilt `PACKAGE-ICON.png` from the source SVG so the icon renders as a complete image rather than an incomplete or partially rendered PNG.
* Preserved bounded project language around Accountable Systems Infrastructure, host-owned execution, package-signing status, and non-claims.

### Changed

* Promoted central package version metadata from `2.0.1` to `2.0.2` while preserving `AssemblyVersion` as `2.0.0.0` for the compatible `2.x` line.
* Updated `FileVersion` to `2.0.2.0`.
* Updated `CITATION.cff` and `.zenodo.json` for the `2.0.2` release.
* Updated README, documentation home, article index, table of contents, release validation, API compatibility / SemVer guidance, release notes, release readiness guidance, and Source Link validation defaults for the `2.0.2` package family.

### Validation

* Release-candidate validation is expected to pass through CI, Stable Release Validation, package metadata validation, template package smoke validation, external consumer smoke tests, stable package integration smoke tests, DocFX build, package SBOM generation, and artifact provenance handling before tagging.
* Version-consistency validation should pass for `2.0.2`, including `Directory.Build.props`, `CITATION.cff`, `.zenodo.json`, optional tag `v2.0.2`, and generated package filenames when package artifacts are supplied.
* Package icon validation should confirm that `PACKAGE-ICON.png` is included in generated `.nupkg` artifacts and renders correctly in package-list and package-detail contexts.
* After packages are published and visible on NuGet, Source Link repository commit metadata should be validated with:

```powershell
./scripts/Validate-Source-Link-commit-metadata.ps1 -Version 2.0.2
```

### Compatibility notes

* Existing stable `2.0.0` and `2.0.1` consumers should be able to upgrade to `2.0.2` without required source-code changes.
* `2.0.2` is a patch release focused on package/repository icon presentation metadata, release metadata, documentation alignment, and validation guidance.
* `AssemblyVersion` remains `2.0.0.0` for the compatible stable `2.x` line.
* `FileVersion` should be updated to `2.0.2.0`.
* Package `Version` and `InformationalVersion` should be updated to `2.0.2`.
* Event Hubs, Purview, Azure-specific SDK adapters, Aspire runtime packages, robotics, immutable-storage, and additional provider packages remain outside the stable package contract unless separately reviewed and released.

## [2.0.1] - 2026-06-26

### Release summary

`2.0.1` is a compatible patch release for the stable `2.0.x` package family.

This release preserves the `2.0.0` public package and namespace boundary while tightening post-`2.0.0` documentation alignment, release-path SBOM/provenance handling, repository/package icon metadata, and release-facing version metadata. No runtime behavior changes or breaking public API changes are intended.

`AssemblyVersion` remains fixed at `2.0.0.0` for the compatible stable `2.x` line. Package `Version`, `FileVersion`, and `InformationalVersion` move to `2.0.1`.

### Added

* Added package-level SBOM generation for produced `.nupkg` artifacts, including SPDX JSON output and an SBOM manifest.
* Added package and SBOM artifact provenance attestation steps where GitHub artifact attestations are available for the workflow event.
* Added refreshed text-free repository/package icon assets for README, DocFX, favicon, and NuGet package metadata.
* Added `2.0.1` release notes and a dedicated `2.0.1` release readiness record.

### Changed

* Promoted central package version metadata from `2.0.0` to `2.0.1` while preserving `AssemblyVersion` as `2.0.0.0` for the compatible `2.x` line.
* Updated `FileVersion` to `2.0.1.0`.
* Updated `CITATION.cff` and `.zenodo.json` for the `2.0.1` release.
* Updated README, documentation home, article index, table of contents, release validation, API compatibility / SemVer guidance, Source Link validation defaults, and release-readiness guidance for the `2.0.1` package family.
* Updated stable release workflows and release validation guidance to include package SBOM generation and package/SBOM provenance artifact handling.
* Refreshed post-`2.0.0` documentation currency while keeping host-owned production boundaries explicit.

### Fixed

* Corrected stale post-`2.0.0` references in current documentation navigation and release-validation guidance.
* Replaced the previous repository/package branding assets with a small-size-friendly governance spine icon for README, documentation, favicon, and package metadata contexts.
* Preserved bounded project language around Accountable Systems Infrastructure, host-owned execution, package-signing status, and non-claims.

### Validation

* Release-candidate validation is expected to pass through CI, Stable Release Validation, package metadata validation, template package smoke validation, external consumer smoke tests, stable package integration smoke tests, DocFX build, package SBOM generation, and artifact provenance handling before tagging.
* Version-consistency validation should pass for `2.0.1`, including `Directory.Build.props`, `CITATION.cff`, `.zenodo.json`, optional tag `v2.0.1`, and generated package filenames when package artifacts are supplied.
* After packages are published and visible on NuGet, Source Link repository commit metadata should be validated with:

```powershell
./scripts/Validate-Source-Link-commit-metadata.ps1 -Version 2.0.1
```

### Compatibility notes

* Existing stable `2.0.0` consumers should be able to upgrade to `2.0.1` without required source-code changes.
* `2.0.1` is a patch release focused on release readiness, documentation alignment, package SBOM/provenance hardening, repository/package branding, and metadata updates.
* `AssemblyVersion` remains `2.0.0.0` for the compatible stable `2.x` line.
* `FileVersion` should be updated to `2.0.1.0`.
* Package `Version` and `InformationalVersion` should be updated to `2.0.1`.
* Event Hubs, Purview, Azure-specific SDK adapters, Aspire runtime packages, robotics, immutable-storage, and additional provider packages remain outside the stable package contract unless separately reviewed and released.

## [2.0.0] - 2026-06-24

### Breaking Changes

* Renamed NuGet package IDs from `CDCavell.AsiBackbone.*` to `AsiBackbone.*`.
* Renamed public namespaces from `CDCavell.AsiBackbone.*` to `AsiBackbone.*`.
* Consumers must update package references and `using` statements when migrating from the 1.x package line.

### Changed

* Updated project/package identity to align the public NuGet package names with the simplified `AsiBackbone.*` namespace.
* Updated source namespaces, project references, and documentation references to use the new `AsiBackbone.*` naming convention.
* Prepared the package family for the new 2.x release line.

### Migration Notes

Replace package references such as:

```xml
<PackageReference Include="CDCavell.AsiBackbone.Core" Version="1.x.x" />
```

with:

```xml
<PackageReference Include="AsiBackbone.Core" Version="2.0.0" />
```

Replace namespace imports such as:

```csharp
using CDCavell.AsiBackbone.Core;
```

with:

```csharp
using AsiBackbone.Core;
```

### Notes

The previous `CDCavell.AsiBackbone.*` packages should be treated as the 1.x package line. The `AsiBackbone.*` packages begin the 2.x package line and represent the preferred package identity going forward.

## [1.2.1] - 2026-06-24

### Release summary

`1.2.1` is a compatible patch release for the stable `1.2.x` package family.

This release preserves the `1.2.0` package/API boundary while hardening release metadata, Source Link repository-commit metadata, package-signing wording, workflow hygiene, documentation alignment, and release-validation guidance. No breaking public API changes are intended.

`AssemblyVersion` remains fixed at `1.0.0.0` for the compatible stable `1.x` line. Package `Version`, `FileVersion`, and `InformationalVersion` move to `1.2.1`.

### Added

* Added Source Link metadata support across package projects and samples so generated packages can include repository commit metadata when built with Source Link enabled.
* Added `scripts/Validate-Source-Link-commit-metadata.ps1` for post-publish validation of NuGet repository metadata and Source Link commit metadata.
* Added `1.2.1` release notes and a dedicated `1.2.1` release readiness record.
* Added explicit .NET Foundation Code of Conduct alignment wording.

### Changed

* Promoted central package version metadata from `1.2.0` to `1.2.1` while preserving `AssemblyVersion` as `1.0.0.0` for the compatible `1.x` line.
* Updated `CITATION.cff` and `.zenodo.json` for the `1.2.1` release.
* Updated README, documentation home, article index, table of contents, release validation, API compatibility / SemVer guidance, template package documentation, and quality posture for the `1.2.1` package family.
* Updated package-signing wording to clarify that current NuGet packages are not signed release artifacts from the project maintainer.
* Updated the Source Link validation script default package version to `1.2.1`.
* Refreshed GitHub Actions checkout usage to `actions/checkout` `v7.0.0`.
* Normalized GitHub workflow YAML files to LF line endings to prevent repeated local modification noise after checkout or hard reset.

### Fixed

* Hardened release-validation documentation so generated NuGet metadata and post-publish Source Link metadata checks are called out explicitly.
* Clarified `1.2.1` as a patch release on the `1.2.0` minor-release boundary rather than a new package/API expansion.
* Preserved bounded project language around Accountable Systems Infrastructure, host-owned execution, package-signing status, and non-claims.

### Validation

* Release-candidate validation is expected to pass through CI, Stable Release Validation, package metadata validation, template package smoke validation, external consumer smoke tests, stable package integration smoke tests, and DocFX build before tagging.
* Version-consistency validation should pass for `1.2.1`, including `Directory.Build.props`, `CITATION.cff`, `.zenodo.json`, optional tag `v1.2.1`, and generated package filenames when package artifacts are supplied.
* After packages are published and visible on NuGet, Source Link repository commit metadata should be validated with:

```powershell
./scripts/Validate-Source-Link-commit-metadata.ps1 -Version 1.2.1
```

### Compatibility notes

* Existing stable `1.2.0` consumers should be able to upgrade to `1.2.1` without required source-code changes.
* `1.2.1` is a patch release focused on release readiness, package metadata, Source Link validation, workflow hygiene, documentation, and implementation hardening.
* `AssemblyVersion` remains `1.0.0.0` for the compatible stable `1.x` line.
* `FileVersion` should be updated to `1.2.1.0`.
* Package `Version` and `InformationalVersion` should be updated to `1.2.1`.
* Event Hubs, Purview, Azure-specific SDK adapters, robotics, immutable-storage, and additional provider packages remain outside the stable package contract unless separately reviewed and released.

## [1.2.0] - 2026-06-20

### Release summary

`1.2.0` is a compatible minor release for the stable `1.x` package family.

This release promotes post-`1.1.x` adoption, diagnostics, testing, templates, samples, documentation alignment, and project-governance work into the current stable release line. No breaking public API changes are intended.

`AssemblyVersion` remains fixed at `1.0.0.0` for the compatible stable `1.x` line. Package `Version`, `FileVersion`, and `InformationalVersion` move to `1.2.0`.

### Added

* Added `AsiBackbone.DependencyInjection` with an explicit `AddAsiBackbone(...)` builder facade for host-selected provider registration.
* Added `AsiBackbone.Testing` as a test-only package for deterministic endpoint-governance harnesses, policy-result shaping, in-memory inspection, and no-signature signing seams.
* Added `AsiBackbone.Templates` with `dotnet new asibackbone-webapi` scaffolding for governed ASP.NET Core hosts.
* Added opt-in ASP.NET Core endpoint-governance development diagnostics gated by explicit configuration and development-environment checks.
* Added a sample-first .NET Aspire AppHost path without introducing an Aspire runtime package.
* Added reference-deployment evidence for the Plain ASP.NET Core host sample.
* Added visual governance flow diagrams for intent-to-execution, policy evaluation, acknowledgment, capability tokens, and durable outbox/emission.
* Added `1.2.0` release notes and a `1.2.0` release readiness record.
* Added project governance and contribution documentation, including code of conduct and governance process documentation.

### Changed

* Promoted central package version metadata from `1.1.1` to `1.2.0` while preserving `AssemblyVersion` as `1.0.0.0` for the compatible `1.x` line.
* Updated README, documentation home, article index, table of contents, release validation, API compatibility, citation metadata, and Zenodo metadata for the `1.2.0` package family.
* Clarified that `1.2.0` is a minor release because it includes additive public/package surfaces and developer-experience tooling.
* Improved implementation-first documentation, search/navigation guidance, release-readiness guidance, and package-family wording.
* Clarified that Aspire remains a sample path, not a runtime package boundary.

### Fixed

* Fixed DocFX/Mermaid rendering for the Dynamic Liability Handshake diagram.
* Fixed the documentation workflow Core coverage input path so Core branch coverage report generation consumes the docs workflow test output.
* Corrected documentation evaluation findings around package-family listings, glossary wording, and SemVer expectations.

### Validation

* Release-candidate validation is expected to pass through CI, Stable Release Validation, package metadata validation, template package smoke validation, external consumer smoke tests, stable package integration smoke tests, and DocFX build before tagging.
* Version-consistency validation should pass for `1.2.0`, including `Directory.Build.props`, `CITATION.cff`, `.zenodo.json`, optional tag `v1.2.0`, and generated package filenames when package artifacts are supplied.

### Compatibility notes

* Existing stable `1.x` consumers should be able to upgrade to `1.2.0` without required source-code changes for existing APIs.
* `1.2.0` is a minor release because it includes additive public APIs, packages, diagnostics, samples, templates, and documentation alignment.
* `AssemblyVersion` remains `1.0.0.0` for the compatible stable `1.x` line.
* `FileVersion` should be updated to `1.2.0.0`.
* Package `Version` and `InformationalVersion` should be updated to `1.2.0`.
* Event Hubs, Purview, Azure-specific SDK adapters, robotics, immutable-storage, and additional provider packages remain outside the stable package contract unless separately reviewed and released.

## [1.1.1] - 2026-06-20

### Release summary

`1.1.1` is a compatible patch release for the stable `1.1.x` package family.

This release focuses on post-`1.1.0` hardening, documentation clarity, endpoint-governance safety, telemetry bounds, allocation reduction, coverage enforcement, and test expansion. No breaking public API changes are intended.

`AssemblyVersion` remains fixed at `1.0.0.0` for the compatible stable `1.x` line. Package `Version`, `FileVersion`, and `InformationalVersion` move to `1.1.1`.

### Added

* Added optional ASP.NET Core endpoint-governance strict mode through `RequireGovernanceMetadata`.
* Added explicit public/excluded endpoint metadata so hosts can allow known public endpoints when strict endpoint-governance mode is enabled.
* Added `AllowMissingGovernanceMetadataAttribute` for explicit host-owned endpoint exclusions.
* Added Core-only branch coverage enforcement with a stricter branch coverage expectation for `AsiBackbone.Core`.
* Added focused test coverage for endpoint governance route-builder behavior.
* Added additional branch and behavior tests for capability grants, signing and verification, canonical payloads, governance emission, outbox behavior, DLP/classification policy, audit integrity, and operation/decision results.
* Added documentation for progressive adoption, API-gating, host-owned execution, DLP/classification scanner seams, safe audit/telemetry data, outbox reliability, outbox concurrency, and terminology mapping.

### Changed

* Clarified stable `1.x` API compatibility and semantic versioning guidance.
* Clarified that `1.1.1` is a patch release that preserves the stable `1.x` binary identity by keeping `AssemblyVersion` at `1.0.0.0`.
* Reorganized documentation navigation to emphasize current stable usage, package-selection guidance, provider boundaries, quality reports, and historical records.
* Reframed alpha-era documents as historical records rather than current release guidance.
* Reconciled production wording around signing, verification, and tamper-evidence boundaries.
* Clarified design-only provider documentation boundaries for Event Hubs, Purview, Azure-specific adapters, robotics, immutable-storage, and future provider packages.
* Updated quality documentation to distinguish repository-wide line coverage, Core-only branch coverage, and targeted mutation analysis.
* Updated release validation and readiness guidance so it can be reused for future stable `1.x` patch and minor releases.

### Fixed

* Bounded normalized `GovernanceDecision` correlation and trace identifiers to deterministic 256-character maximums.
* Preserved existing blank-to-null telemetry identifier behavior while trimming and truncating overlong telemetry identifiers safely.
* Corrected stale namespace references from earlier capability-token documentation.
* Reduced ambiguity in API compatibility documentation around released stable provider packages versus future/design-only provider plans.
* Clarified that endpoint governance does not replace ASP.NET Core authentication, authorization, routing, middleware enforcement, persistence, UI, or execution controls.

### Performance

* Reduced default ASP.NET Core middleware 403 allocation pressure by using a cached, bodyless forbidden result by default.
* Added an opt-in forbidden-result factory so hosts can still return richer safe responses when desired.
* Reduced avoidable Core allocation overhead in reason handling for `GovernanceDecision`, `ConstraintEvaluationResult`, and `OperationResult`.

### Security and governance hardening

* Added optional fail-closed endpoint governance behavior for hosts that require every selected endpoint to carry explicit AsiBackbone governance metadata.
* Preserved existing default endpoint-governance behavior for backward compatibility.
* Reinforced host-owned execution boundaries.
* Clarified DLP/classification scanner integration boundaries and fail-open/fail-closed behavior by risk level.
* Replaced the vulnerable/deprecated SQLite native transitive restore path used by SQLite-backed sample and test projects.

### Compatibility notes

* Existing `1.1.0` consumers should be able to upgrade to `1.1.1` without required source-code changes.
* Endpoint-governance strict mode is opt-in.
* Event Hubs, Purview, Azure-specific SDK adapters, robotics, immutable-storage, and additional provider packages remain outside the stable package contract unless separately reviewed and released.

## [1.1.0] - 2026-06-16

### Added

* Added provider-neutral governance emission contracts and envelope/result/error primitives for downstream observability and governance projection.
* Added durable governance outbox and audit residue lifecycle surfaces so local accountability records can be preserved before provider emission is attempted.
* Added `AsiBackbone.OpenTelemetry` as the first concrete governance emission provider package for projecting governance envelopes into `ActivitySource` and `Meter` diagnostics.
* Added `AsiBackbone.Analyzers` as Roslyn analyzer safety rails for governance persistence and continuation flows.
* Added signing-ready receipt, canonical hashing/signing, and verification-policy seams while keeping Core provider-neutral.
* Added `AsiBackbone.Signing.LocalDevelopment` for local-development RSA signing and verification in tests, samples, and wiring proof paths.
* Added `AsiBackbone.Signing.ManagedKey` as a provider-neutral managed-key signing adapter boundary where the host supplies the actual managed-key client and operational policy.
* Added ASP.NET Core endpoint governance metadata and hosted outbox drain integration.

### Changed

* Promoted central package version metadata from `1.0.0` to `1.1.0` while preserving `AssemblyVersion` as `1.0.0.0` for the compatible `1.x` line.
* Updated release-facing README, citation metadata, Zenodo metadata, release notes, package validation anchors, and release guidance for the `1.1.0` package family.
* Expanded package validation to include Analyzers, OpenTelemetry, Signing.LocalDevelopment, and Signing.ManagedKey package artifacts.
* Clarified that OpenTelemetry is a provider projection path and that Azure Monitor remains host-configured through the host OpenTelemetry pipeline.
* Clarified signing language so provider signing is described as one part of an operational trust model, not production tamper-evidence or non-repudiation by itself.

### Validation

* Version-consistency validation is expected to pass for `1.1.0`, including `Directory.Build.props`, `CITATION.cff`, `.zenodo.json`, tag version checks, and generated package filenames when package artifacts are supplied.
* NuGet metadata validation now expects the `1.1.0` stable package-family README wording and all eight package artifacts.
* Release validation and package publish workflows should restore, build, format-check, test, build DocFX, pack packages, validate package metadata, and run package smoke checks before publication.

### Boundary Notes

* `1.1.0` remains Accountable Systems Infrastructure and governance spine infrastructure, not an artificial superintelligence implementation, AI model host, robot controller, legal/compliance guarantee, or production tamper-evident ledger provider.
* Event Hubs, Purview, Azure-specific SDK adapters, robotics/physical execution, immutable storage, and external anchoring remain outside the `1.1.0` stable package boundary unless separately released as stable packages.
* The managed-key signing adapter does not ship live Azure Key Vault, Managed HSM, cloud KMS, HSM, certificate-store, or credential-provider implementation by default.

## [1.0.0] - 2026-06-14

### Added

* Finalized the first stable package-family release boundary for `AsiBackbone.Core`, `AsiBackbone.Storage.InMemory`, `AsiBackbone.EntityFrameworkCore`, and `AsiBackbone.AspNetCore`.
* Added release-ready metadata alignment across `Directory.Build.props`, `CITATION.cff`, `.zenodo.json`, release notes, and version-consistency validation.
* Added schema-version stamping for durable or exported governance artifacts that require stable migration anchors.
* Added stable package integration and external consumer smoke-test coverage to validate consumer-style package usage before publication.
* Added generated `.nupkg` metadata validation for package IDs, descriptions, tags, license metadata, project URL, repository URL, and packaged README files.

### Changed

* Promoted package version metadata from `0.4.0-alpha.3` to `1.0.0` without a preview suffix.
* Updated citation and Zenodo metadata to describe the `1.0.0` release consistently.
* Updated documentation that described the package family as alpha so current release-facing pages describe the stable package line instead.
* Updated NuGet package descriptions and tags to use bounded Accountable Systems Infrastructure wording and package-specific stable roles.

### Validation

* Version-consistency validation now checks central MSBuild version metadata, package project metadata, `CITATION.cff`, `.zenodo.json`, optional release tag metadata, and generated package names when package artifacts are supplied.
* NuGet metadata validation now inspects generated package artifacts before stable release validation and package publication continue.
* Release notes, schema-version guidance, API compatibility expectations, privacy/signing boundaries, and known limitations are documented for the `1.0.0` release path.

### Boundary Notes

* `1.0.0` remains Accountable Systems Infrastructure and governance spine infrastructure, not an artificial superintelligence implementation, AI model host, robot controller, legal/compliance guarantee, or tamper-evident ledger provider.
* Future signing, gateway, cloud observability, and robotics/provider packages remain outside the stable `1.0.0` contract unless separately released as stable.

## [0.4.0-alpha.3] - 2026-06-13

### Added

* Added an external consumer smoke-test workflow for package ergonomics validation.
* Added `eng/smoke-tests/external-consumer-smoke.sh` to pack local package artifacts, generate a clean temporary xUnit consumer project, install AsiBackbone packages from a local NuGet source, and run HTTP-based smoke assertions.
* Added external consumer validation for:

  * Core + ASP.NET Core adapter registration through `AddAsiBackboneAspNetCore()`.
  * host-owned `DbContext` integration for the EF Core audit ledger path.
  * in-memory audit storage for minimal non-durable hosts.
  * allow, deny, and acknowledgment-required HTTP decision flows.
* Added external consumer smoke-test documentation under Quality Reports.
* Added the external consumer smoke test to quality documentation navigation.
* Added focused Core mutation-survivor triage documentation covering evaluator, decision, audit, and handshake behavior.
* Added mutation-focused Core tests for:

  * deny-wins evaluator composition and reason aggregation.
  * warning-only evaluator composition.
  * full constraint-result visibility to decision policy.
  * cancellation between evaluator constraints.
  * non-allow governance decision factory propagation.
  * read-only decision reason snapshots.
  * audit residue trace, policy, metadata, and actor propagation.
  * liability handshake request and acknowledgment boundary behavior.
* Added governance tool comparison documentation comparing Azure Policy, Open Policy Agent, Microsoft Agent Governance Toolkit, and AsiBackbone as complementary governance layers.

### Changed

* Updated quality documentation to include external consumer smoke-test guidance.
* Updated quality documentation to link Core test triage from the Quality Reports index.
* Updated the external consumer smoke-test workflow to run on pull requests, pushes to `main`, and manual dispatch.
* Updated the external consumer smoke-test script to run the generated consumer project outside the repository tree, preventing inheritance of repository Central Package Management settings.
* Updated the external consumer smoke-test script to normalize package output paths before use.
* Updated PR validation expectations to include the external consumer package smoke test as a required confidence check.

### Fixed

* Fixed CI formatting failures in Core mutation-focused test files.
* Fixed external consumer smoke-test path resolution so generated project paths remain valid after directory changes.
* Fixed external consumer smoke-test package installation failures caused by generated projects inheriting repository `Directory.Packages.props` settings.

### Validation

* Confirmed the Release test suite passes with 385 tests, 0 failed, and 0 skipped after formatting cleanup.
* Added package-shaped validation that verifies a clean consumer-style host can wire AsiBackbone packages without project references.
* Verified the smoke-test design exercises allow, deny, and acknowledgment-required flows through HTTP.
* Verified host-owned persistence boundaries remain explicit for the EF Core ledger path.

### Boundary Notes

* The external consumer smoke test is a package-consumer ergonomics check, not a production host template.
* The generated smoke project intentionally avoids repository project references.
* EF Core persistence remains host-owned: the host supplies the `DbContext`, provider, connection string, schema lifecycle, and migration strategy.
* In-memory audit storage remains non-durable and intended only for tests, samples, and local validation.
* AsiBackbone remains Accountable Systems Infrastructure and governance spine infrastructure, not an intelligence engine, AI model host, or artificial superintelligence implementation.

## [0.4.0-alpha.2] - 2026-06-12

### Added

* Added Stryker.NET as a local .NET tool for mutation-analysis validation.
* Added an initial Core test-project Stryker configuration for evaluator and policy-pipeline mutation testing.
* Added an ASP.NET Core test-project Stryker configuration for acknowledgment challenge mutation testing.
* Added mutation-focused ASP.NET Core acknowledgment challenge tests covering safe-default challenge shaping and response conversion.
* Added a Quality Reports landing page for coverage and mutation-analysis reports.
* Added Quality to the DocFX top navigation.

### Changed

* Updated the documentation publishing workflow to generate and publish the Core mutation report alongside the existing coverage report.
* Updated the release/manual quality workflow to generate separate Core and ASP.NET Core mutation reports.
* Updated DocFX content configuration so the Quality Reports landing page is included in the documentation site.

### Documentation

* Clarified that ASI means **Accountable Systems Infrastructure** within the AsiBackbone software project.
* Updated the README, documentation index, Getting Started guide, Why AsiBackbone article, and Core domain language article to frame AsiBackbone as governance infrastructure rather than artificial superintelligence.
* Added Accountable Systems Infrastructure to the core domain language and alignment guidance.

### Boundary Notes

* Reinforced that AsiBackbone does not implement artificial superintelligence, host or train AI models, control robots, or prove the Eden/Backbone framework.
* Clarified that broader Eden/Backbone concepts may inspire the package while implementation claims remain limited to practical software governance.

## [0.4.0-alpha.1] - 2026-06-11

### Samples and Host Validation

### Added

* Added `samples/PlainAspNetCoreHost` as the canonical in-repository ASP.NET Core validation sample.
* Added a plain ASP.NET Core sample project demonstrating:

  * `AddAsiBackboneAspNetCore()` registration.
  * host-defined constraint evaluation.
  * host-defined decision policy behavior.
  * acknowledgment-required decision flow.
  * in-memory audit residue capture.
  * EF Core audit ledger persistence through a host-owned `DbContext`.
  * SQLite-based local validation.
* Added sample documentation for the plain ASP.NET Core host.
* Added DocFX article for the plain ASP.NET Core host sample.
* Added DocFX article documenting `NetCoreApplicationTemplate` as an optional external local validation host.
* Added package-reference and local project-reference guidance for validating AsiBackbone against a `NetCoreApplicationTemplate`-generated host.
* Added host-owned EF Core integration guidance showing `ApplyAsiBackboneConfigurations()`.
* Added temporary validation endpoint sketch for external host validation.
* Added targeted branch coverage tests for:

  * ASP.NET Core options.
  * request correlation resolution.
  * acknowledgment challenge handling.
  * EF Core audit ledger edge paths.

### Changed

* Updated documentation navigation to include sample and host-validation guidance.
* Updated README links to reference the new sample and host-validation documentation.
* Clarified that the plain ASP.NET Core host sample is the canonical in-repository validation baseline.
* Clarified that `NetCoreApplicationTemplate` is a preferred external validation host, not a required dependency or parent framework.

### Validation

* Confirmed the solution builds successfully in Release configuration.
* Confirmed the full test suite passes in Release configuration.
* Regenerated local coverage after targeted branch coverage additions.

### Boundary Notes

* No AsiBackbone project references `NetCoreApplicationTemplate`.
* No in-repository `NetCoreApplicationTemplate` sample was added.
* `NetCoreApplicationTemplate` remains optional and external.
* AsiBackbone remains governance infrastructure, not an intelligence engine, AI model host, or ASI implementation.

## [0.3.0-alpha.1] - 2026-06-11

### Added

* Added the initial `AsiBackbone.AspNetCore` alpha integration package.
* Added ASP.NET Core service registration extensions through `AddAsiBackboneAspNetCore(...)`.
* Added configurable ASP.NET Core integration options with startup validation.
* Added an HTTP actor context adapter for resolving Core-compatible actor context from `HttpContext.User`.
* Added configurable claim mapping for actor identifiers, display names, and actor type.
* Added safe unauthenticated actor handling without throwing during normal request flow.
* Added ASP.NET Core request correlation support for resolving correlation identifiers, trace identifiers, and safe request metadata.
* Added audit enrichment helpers for creating Core audit residue from HTTP request correlation data.
* Added HTTP result mapping helpers for Core `GovernanceDecision` and `OperationResult` values.
* Added host-overridable HTTP result mapping options for allowed, warning, denied, deferred, acknowledgment-required, escalation-recommended, and failed operation outcomes.
* Added Problem Details-style responses for non-success governance and operation outcomes.
* Added safe default response behavior that preserves reason codes and correlation identifiers while hiding reason messages, trace identifiers, policy versions, and policy hashes unless explicitly enabled.
* Added ASP.NET Core acknowledgment challenge models and service support for Core `AcknowledgmentRequired` governance decisions.
* Added acknowledgment challenge response handling that round-trips accepted or rejected responses into Core `LiabilityHandshakeAcknowledgment` values.
* Added tests for service registration, actor context resolution, request correlation, audit enrichment, HTTP result mapping, and acknowledgment challenge handling.

### Documentation

* Added ASP.NET Core integration boundary documentation.
* Added ASP.NET Core package README guidance for service registration, request correlation, audit enrichment, HTTP result mapping, and acknowledgment challenge usage.
* Documented the package as a thin web-host adapter around Core governance primitives.
* Documented that hosts remain responsible for authentication, authorization, persistence, routing, UI rendering, endpoint exposure, and operational execution.

### Boundaries

* The ASP.NET Core package keeps Core framework-neutral and free of ASP.NET Core dependencies.
* The ASP.NET Core package does not register EF Core, persistence stores, authentication handlers, MVC, Razor Pages, Minimal API endpoints, middleware enforcement, policy evaluators, or NetCoreApplicationTemplate dependencies by default.
* HTTP result mapping and acknowledgment challenge helpers are explicit host adapters and do not enforce decisions automatically.
* Hosts choose how to render, store, protect, and round-trip acknowledgment challenge state.

### Notes

* This alpha release establishes the first web-host integration layer for the AsiBackbone package family.
* The implementation is intentionally adapter-focused: it translates ASP.NET Core request context into Core governance language and translates Core outcomes into HTTP-friendly shapes when explicitly used by the host.

## [0.2.0-alpha.1] - 2026-06-10

### Added

* Added EF Core `ModelBuilder` extension support for host-owned persistence integration.
* Added `ApplyAsiBackboneConfigurations(this ModelBuilder modelBuilder)` for applying AsiBackbone EF Core model contributions from a consuming application's `DbContext`.
* Added tests proving the extension can be called from a host-owned `DbContext`.
* Added argument validation for null `ModelBuilder` usage.
* Added provider-neutral EF Core persistence entities and configurations for audit ledger records, reason codes, metadata, handshake requests, and handshake acknowledgments.
* Added an EF Core-backed audit ledger store for append-oriented accountability persistence through a host-owned `DbContext`.
* Added EF Core tests proving host-owned DbContext integration, model metadata, keys, relationships, indexes, enum conversion, and basic persistence behavior.
* Added SQLite-backed EF Core integration tests proving relational schema creation, persistence/readback, and query behavior without package-owned migrations.
* Added EF Core host ownership and migration guidance documentation.
* Added package-specific README files for the EF Core and in-memory storage packages.

### Fixed

* Updated EF Core documentation samples to show host applications calling the extension from `OnModelCreating`.
* Aligned EF Core documentation with the implemented `AsiBackbone.EntityFrameworkCore` package name.
* Normalized EF Core configuration folder and file paths.
* Updated the root README to describe the current 0.2 persistence package status.
* Cleared the EF Core change tracker after audit ledger append failures so failed append entities do not remain tracked in the host-owned context.

### Notes

* The EF Core integration preserves host ownership of the `DbContext`, database provider, connection string, migrations, deployment process, and schema lifecycle.
* AsiBackbone contributes model configuration; the consuming application remains the persistence composition root.
* Wired the configurations through the existing `ApplyAsiBackboneConfigurations` `ModelBuilder` extension.
* The in-memory storage package remains non-durable and intended only for tests, samples, and local validation hosts.

## [0.1.0-alpha.2] - 2026-06-09

### Added

* Added a host-neutral Core policy evaluator contract and default policy evaluator implementation.
* Added a decision policy extension point for raising composed decisions to deferred, acknowledgment-required, or escalation-recommended outcomes.
* Added an audit sink contract for writing audit residue without requiring a database or web host.
* Added an in-memory audit ledger project for local validation, samples, and tests.
* Added branch-focused unit tests for `AuditLedgerRecord.FromResidue`.
* Added branch-focused unit tests for `DefaultAsiBackbonePolicyEvaluator<TContext>`.
* Added end-to-end policy evaluator tests covering allow, deny, warning, acknowledgment-required, escalation-recommended, deferred, and not-applicable constraint scenarios.
* Added policy evaluator pipeline documentation with a minimal in-memory usage example.

### Fixed

* Aligned policy evaluator tests with the intended constraint-versus-decision policy boundary.
* Preserved elevated-risk warnings as constraint-layer results instead of replacing them in the decision policy layer.
* Updated test expectations to match current `AsiBackbone` assembly casing.
* Added explicit switch handling for low-risk and elevated-risk document policy scenarios.

### Boundaries

* The evaluator remains framework-neutral and does not depend on ASP.NET Core, Entity Framework Core, robotics packages, database providers, or AI model hosting.
* The in-memory ledger is non-durable and intended only for tests, samples, and local validation hosts.

## [0.1.0-alpha.1] - 2026-06-04

### Added

* Introduced the initial `AsiBackbone.Core` alpha package boundary.
* Added framework-neutral domain primitives for governance-oriented decision flow.
* Added actor context primitives for describing who or what is requesting an operation.
* Added entity identity and optimistic-concurrency abstractions.
* Added operation result primitives for package execution outcomes.
* Added reason code primitives for explainable result and decision handling.
* Added constraint evaluation primitives for allow, deny, warning, and not-applicable outcomes.
* Added governance decision primitives for allowed, warning, denied, deferred, acknowledgment-required, and escalation-recommended outcomes.
* Added audit residue primitives for capturing decision traces, reason codes, policy version/hash, correlation data, timestamps, actor data, and metadata.
* Added persistent audit ledger record shape and framework-neutral storage contract.
* Added liability/responsibility handshake primitives for acknowledgment before consequential execution.
* Added capability-token abstractions for scoped, time-bound, traceable permission grants.
* Added assembly marker support for discovery-friendly package references.
* Added XML documentation coverage for public Core types and members.
* Added unit tests for introduced Core primitives.

### Documentation

* Added README documentation describing AsiBackbone as a governance spine rather than an intelligence engine.
* Added Core domain language documentation for the initial alpha boundary.
* Added package boundary documentation clarifying what belongs in Core versus future integration packages.
* Added EF Core integration boundary documentation for future host-owned persistence support.
* Added alpha readiness review documentation.

### Boundaries

* Core does not implement artificial superintelligence.
* Core does not host, train, or run AI models.
* Core does not prove the ASI Backbone concept or the Eden Hypothesis.
* Core does not depend on ASP.NET Core, Entity Framework Core, NetCoreApplicationTemplate, robotics packages, or AI model dependencies.
* Core does not provide middleware, endpoint mapping, database storage, signing implementation, robotics control, or provider-specific persistence behavior.

### Notes

* This alpha release establishes the foundational language and primitives for the AsiBackbone package family.
* Future packages may provide ASP.NET Core integration, EF Core persistence integration, in-memory storage, signing support, samples, and later gateway or robotics examples.
