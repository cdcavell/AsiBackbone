# 2.0.1 Release Notes

`2.0.1` is a compatible patch release for the stable `2.0.x` AsiBackbone package family.

This release preserves the `2.0.0` public package and namespace boundary while tightening post-`2.0.0` documentation currency, release-path SBOM/provenance artifacts, repository/package icon branding, and release metadata. No breaking public API changes or runtime behavior changes are intended.

## Release summary

`2.0.1` keeps the simplified `AsiBackbone.*` package and namespace identity established by `2.0.0`. It promotes the post-`2.0.0` documentation review, package SBOM/provenance workflow hardening, refreshed repository/package icon assets, citation metadata, Zenodo metadata, and Source Link validation default into the current patch release posture.

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

## Added

* Added package-level SBOM generation for produced `.nupkg` artifacts, including SPDX JSON output and an SBOM manifest.
* Added package and SBOM artifact provenance attestation steps where GitHub artifact attestations are available for the workflow event.
* Added refreshed text-free repository/package icon assets for README, DocFX, favicon, and NuGet package metadata.
* Added this `2.0.1` release notes page and a dedicated `2.0.1` release readiness record.

## Changed

* Promoted central package version metadata to `2.0.1`.
* Kept `AssemblyVersion` at `2.0.0.0` for the compatible stable `2.x` line and moved `FileVersion` to `2.0.1.0`.
* Updated `CITATION.cff` and `.zenodo.json` to `2.0.1`.
* Updated README, documentation home, article index, table of contents, release validation, API compatibility / SemVer guidance, and Source Link validation default package version for the `2.0.1` package family.
* Updated stable release workflows and release validation guidance to include package SBOM generation and package/SBOM provenance artifact handling.

## Fixed

* Corrected stale post-`2.0.0` documentation references in current-family guidance and release-validation pages.
* Replaced the previous repository/package branding assets with a small-size-friendly governance spine icon that works in README, documentation, favicon, and package metadata contexts.
* Preserved bounded project language around Accountable Systems Infrastructure, host-owned execution, package-signing status, and non-claims.

## Compatibility notes

* Existing stable `2.0.0` consumers should be able to upgrade to `2.0.1` without required source-code changes.
* `2.0.1` is a patch release focused on release readiness, documentation alignment, package SBOM/provenance hardening, repository/package branding, and metadata updates.
* `AssemblyVersion` remains `2.0.0.0` for the compatible stable `2.x` line.
* `FileVersion` moves to `2.0.1.0`.
* Package `Version` and `InformationalVersion` move to `2.0.1`.
* Future Event Hubs, Purview, Azure-specific SDK adapters, Aspire runtime packages, robotics, immutable-storage, and additional provider packages remain outside the stable package contract unless separately reviewed and released.

## Validation

Before tagging `v2.0.1`, confirm the release-candidate commit passes:

* CI restore, build, formatting, tests, coverage gates, package creation, package SBOM generation, template smoke validation, and CodeQL.
* Stable Release Validation.
* Publish Documentation / DocFX build.
* External Consumer Smoke Test.
* Generated NuGet package metadata validation.
* Package and SBOM provenance handling where supported by the workflow event.
* Version consistency validation for `2.0.1`, including `Directory.Build.props`, `CITATION.cff`, `.zenodo.json`, optional tag `v2.0.1`, and generated package filenames.

After packages are published and visible on NuGet, validate Source Link repository commit metadata with:

```powershell
./scripts/Validate-Source-Link-commit-metadata.ps1 -Version 2.0.1
```

## Release boundary

`2.0.1` does not expand the stable public API surface or change the project boundary. AsiBackbone remains a governance spine, not an intelligence engine. See [Project Boundaries and Non-Claims](project-boundaries.md) for the full scope statement.
