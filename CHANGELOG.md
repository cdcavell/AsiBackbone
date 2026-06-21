# Changelog

All notable changes to this project are documented in this file.

This project follows the spirit of [Keep a Changelog](https://keepachangelog.com/) and adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.2.0] - 2026-06-20

### Release summary

`1.2.0` is a compatible minor release for the stable `1.x` package family.

This release promotes post-`1.1.x` adoption, diagnostics, testing, templates, samples, documentation alignment, and project-governance work into the current stable release line. No breaking public API changes are intended.

`AssemblyVersion` remains fixed at `1.0.0.0` for the compatible stable `1.x` line. Package `Version`, `FileVersion`, and `InformationalVersion` move to `1.2.0`.

### Added

* Added `CDCavell.AsiBackbone.DependencyInjection` with an explicit `AddAsiBackbone(...)` builder facade for host-selected provider registration.
* Added `CDCavell.AsiBackbone.Testing` as a test-only package for deterministic endpoint-governance harnesses, policy-result shaping, in-memory inspection, and no-signature signing seams.
* Added `CDCavell.AsiBackbone.Templates` with `dotnet new asibackbone-webapi` scaffolding for governed ASP.NET Core hosts.
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
* Added Core-only branch coverage enforcement with a stricter branch coverage expectation for `CDCavell.AsiBackbone.Core`.
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
* Added `CDCavell.AsiBackbone.OpenTelemetry` as the first concrete governance emission provider package for projecting governance envelopes into `ActivitySource` and `Meter` diagnostics.
* Added `CDCavell.AsiBackbone.Analyzers` as Roslyn analyzer safety rails for governance persistence and continuation flows.
* Added signing-ready receipt, canonical hashing/signing, and verification-policy seams while keeping Core provider-neutral.
* Added `CDCavell.AsiBackbone.Signing.LocalDevelopment` for local-development RSA signing and verification in tests, samples, and wiring proof paths.
* Added `CDCavell.AsiBackbone.Signing.ManagedKey` as a provider-neutral managed-key signing adapter boundary where the host supplies the actual managed-key client and operational policy.
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

* Finalized the first stable package-family release boundary for `CDCavell.AsiBackbone.Core`, `CDCavell.AsiBackbone.Storage.InMemory`, `CDCavell.AsiBackbone.EntityFrameworkCore`, and `CDCavell.AsiBackbone.AspNetCore`.
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

## Earlier alpha releases

Earlier alpha entries from `0.1.0-alpha.1` through `0.4.0-alpha.3` are preserved in repository history and historical design records. They covered the initial Core boundary, policy evaluator, in-memory storage, EF Core integration, ASP.NET Core adapter, sample host validation, quality-report publishing, mutation-testing setup, and external consumer smoke-test validation that led into the stable `1.0.0` release.
