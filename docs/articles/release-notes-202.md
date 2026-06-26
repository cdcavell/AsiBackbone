# AsiBackbone 2.0.2 Release Notes

`2.0.2` is a compatible patch release for the stable `2.0.x` AsiBackbone package family.

This release corrects the package-facing icon presentation issue discovered after `2.0.1`. The previous package icon asset could render incompletely in package-list and package-detail contexts. `2.0.2` preserves the `2.0.0` public package and namespace boundary while updating release metadata and documentation to identify the corrected package presentation release.

## Release summary

`2.0.2` keeps the simplified `AsiBackbone.*` package and namespace identity established in `2.0.0`.

This is a patch release focused on package/repository presentation assets and release metadata. It does not intentionally change runtime behavior, expand public APIs, rename packages, or alter namespaces.

## Fixed

* Corrects the package icon asset used for NuGet and repository/package presentation.
* Rebuilds the package icon from the source SVG so the icon renders as a complete image rather than an incomplete or partially rendered PNG.
* Aligns release-facing metadata and documentation to the corrected `2.0.2` package release.

## Changed

* Promotes central package version metadata from `2.0.1` to `2.0.2` while preserving `AssemblyVersion` as `2.0.0.0` for the compatible stable `2.x` line.
* Updates `FileVersion` to `2.0.2.0`.
* Updates `CITATION.cff` and `.zenodo.json` for the `2.0.2` release.
* Updates README, documentation home, article index, DocFX article navigation, release validation guidance, API compatibility / SemVer guidance, and Source Link validation defaults for the `2.0.2` package family.

## Compatibility

Existing stable `2.0.0` and `2.0.1` consumers should be able to upgrade to `2.0.2` without required source-code changes.

No intentional runtime behavior changes are included.

No public API expansion is claimed.

No package ID or namespace changes are included.

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

AsiBackbone remains **Accountable Systems Infrastructure** for governed .NET decision flow.

This release does not change AsiBackbone into an intelligence engine, compliance product, robot controller, signing appliance, immutable ledger, or operational enforcement system. Hosts remain responsible for authentication, authorization, execution, persistence, key management, privacy review, operational monitoring, and legal/compliance interpretation.

## Validation

Before tagging `v2.0.2`, the release candidate should pass the repository release gates, including:

* CI restore, build, formatting, tests, and coverage gates.
* Stable Release Validation.
* DocFX documentation build.
* Package creation.
* Generated NuGet metadata validation.
* Package SBOM generation.
* Template package smoke validation.
* External consumer smoke tests.
* Version consistency validation for `2.0.2`.
* Package/SBOM provenance handling where supported by the workflow event.

The release candidate should also confirm that `PACKAGE-ICON.png` renders correctly in package-list and package-detail contexts before publication.

After packages are published and visible on NuGet, validate Source Link repository commit metadata with:

```powershell
./scripts/Validate-Source-Link-commit-metadata.ps1 -Version 2.0.2
```
