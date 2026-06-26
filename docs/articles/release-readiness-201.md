# 2.0.1 Release Readiness Record

This record tracks release readiness for AsiBackbone `2.0.1`.

## Release intent

`2.0.1` is a compatible patch release for the current `2.0.x` package family.

The release preserves the `2.0.0` public package and namespace boundary while promoting post-`2.0.0` documentation currency, package SBOM/provenance workflow hardening, refreshed repository/package icon assets, and release metadata updates.

## Release classification

| Field | Value |
| --- | --- |
| Release | `2.0.1` |
| Tag | `v2.0.1` |
| Release type | Patch |
| Primary reason | Documentation, release-path, metadata, and branding hardening |
| Stable line | `2.x` |
| AssemblyVersion | `2.0.0.0` |
| FileVersion | `2.0.1.0` |
| NuGet Version | `2.0.1` |

## Stable package set

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

## Public API and behavior review

`2.0.1` is intended to preserve the stable `2.0.0` public API and package boundary.

Expected consumer impact:

* no required source-code changes for existing `2.0.0` consumers;
* no package ID changes;
* no public namespace changes;
* no runtime behavior expansion claimed by the release;
* package and documentation artifacts should reflect the `2.0.1` patch identity.

## Version metadata checklist

- [ ] `Directory.Build.props` uses `VersionPrefix` `2.0.1`.
- [ ] `Directory.Build.props` keeps `AssemblyVersion` `2.0.0.0`.
- [ ] `Directory.Build.props` uses `FileVersion` `2.0.1.0`.
- [ ] `CITATION.cff` uses version `2.0.1`.
- [ ] `.zenodo.json` uses version `2.0.1`.
- [ ] Generated package filenames use `2.0.1`.
- [ ] Tag validation passes for `v2.0.1`.

## Documentation checklist

- [ ] `CHANGELOG.md` contains a `2.0.1` entry.
- [ ] `README.md` identifies stable `2.0.x` as the current release line and `2.0.1` as the current patch release.
- [ ] Documentation home links to `2.0.1` release notes and readiness record.
- [ ] Article index links to `2.0.1` release notes and readiness record.
- [ ] Article table of contents links to `2.0.1` release notes and readiness record.
- [ ] API compatibility / SemVer guidance documents the `2.0.1` patch posture.
- [ ] Release validation guidance references the `2.0.1` Source Link validation command.
- [ ] Supply-chain provenance guidance remains linked from release validation and security navigation.

## CI and validation checklist

- [ ] CI passes on the release-candidate commit.
- [ ] Dependency review passes where applicable.
- [ ] Restore succeeds.
- [ ] Release build succeeds.
- [ ] Formatting check succeeds.
- [ ] Full test suite succeeds.
- [ ] Coverage gates succeed.
- [ ] Package creation succeeds.
- [ ] Generated package version validation succeeds.
- [ ] Generated NuGet metadata validation succeeds.
- [ ] Package SBOM generation succeeds.
- [ ] Template package smoke validation succeeds.
- [ ] DocFX build succeeds.
- [ ] External consumer smoke tests succeed.
- [ ] Stable package integration smoke tests succeed.
- [ ] Package and SBOM artifact provenance handling succeeds where supported by the workflow event.
- [ ] CodeQL analysis passes where applicable.

## Post-publish checklist

- [ ] Verify all `AsiBackbone.*` packages are visible on NuGet at version `2.0.1`.
- [ ] Confirm generated package SBOM artifacts are available from the workflow run.
- [ ] Confirm package and SBOM provenance attestations are available where supported by the workflow event.
- [ ] Run Source Link repository metadata validation:

```powershell
./scripts/Validate-Source-Link-commit-metadata.ps1 -Version 2.0.1
```

- [ ] Confirm the GitHub release notes label `2.0.1` as a compatible patch release.
- [ ] Confirm Zenodo receives or records the `2.0.1` software version metadata.

## Boundary notes

This release does not change the project into an intelligence engine, compliance product, robot controller, signing appliance, or operational enforcement system. Hosts still own authentication, authorization, execution, persistence, key management, privacy review, operational monitoring, and legal/compliance interpretation.
