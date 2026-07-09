# AsiBackbone 3.0.0 Release Readiness Record

This record tracks the release-candidate posture for `3.0.0`, the first stable release on the current `3.x` package family.

## Release intent

`3.0.0` establishes the current `3.x` stable release line for AsiBackbone while preserving the existing `AsiBackbone.*` package IDs and public namespaces.

The release updates the binary assembly identity to `3.0.0.0`, aligns package metadata and documentation around the 3.0.0 release posture, refreshes stale 2.x-current wording, and keeps the project bounded as Accountable Systems Infrastructure for governed .NET decision flow.

## Release classification

| Field | Value |
| --- | --- |
| Release | `3.0.0` |
| Type | Major release / new stable major line |
| Stable line | `3.x` |
| Assembly identity | `3.0.0.0` |
| File version | `3.0.0.0` |
| Primary purpose | Current major-line alignment, binary identity update, release documentation refresh, template fallback alignment, and stale documentation correction |
| Public API expansion | Current code includes additive surfaces carried into the 3.x line, including threat-model contributor hooks and strict-governance profile helpers. |
| Runtime default behavior change | No broad fail-closed default flip is intentionally documented for 3.0.0; strict posture remains explicit host opt-in. |
| Package ID changes | No |
| Namespace changes | No |

## Included release surfaces

- `Directory.Build.props` uses `VersionPrefix` `3.0.0`.
- `AssemblyVersion` moves to `3.0.0.0` for the new major line.
- `FileVersion` moves to `3.0.0.0`.
- `CITATION.cff` and `.zenodo.json` reference `3.0.0`.
- Template fallback package references use `3.0.0`.
- Source Link post-publish validation defaults to `3.0.0`.
- 3.0.0 release notes and this 3.0.0 release readiness record are present.
- Local release-hardening commands are documented in the developer checklist, and Debug solution builds include all first-party package and test projects.
- README, documentation home, article index, DocFX article navigation, release validation, release cadence, API compatibility / SemVer guidance, security posture, governance wording, and template guidance are aligned to the `3.x` current-release posture.
- Historical release notes and readiness records remain available for traceability.

## Required release-candidate checks

Before tagging `v3.0.0`, confirm:

- `Directory.Build.props` uses `VersionPrefix` `3.0.0`.
- `AssemblyVersion` is `3.0.0.0`.
- `FileVersion` is `3.0.0.0`.
- `CITATION.cff` references `3.0.0`.
- `.zenodo.json` references `3.0.0`.
- `3.0.0` release notes exist and identify the release as the start of the current `3.x` stable line.
- `CHANGELOG.md` includes a `3.0.0` entry.
- README, documentation home, article index, DocFX article TOC, release validation, templates guidance, API compatibility / SemVer guidance, security posture, and governance wording reference `3.0.0` or `3.x` where current-release guidance is expected.
- Source Link post-publish validation defaults to `3.0.0`.
- Template fallback `PackageReference` versions use `3.0.0`.
- Release notes state that no package ID or namespace changes are included.
- Release notes state that the new major line changes binary assembly identity and consumers should rebuild and validate host deployments.
- Release notes preserve the project boundary: governance spine, not intelligence engine, AI model host, robot controller, compliance certification, or production tamper-evident ledger by default.
- Release notes state that NuGet package signing remains deferred unless a reviewed package-signing process is adopted before release.
- The developer checklist states the canonical local release-hardening commands: restore, Release build, and Release test of `AsiBackbone.slnx`.
- `./scripts/Validate-DebugSolutionBuildCoverage.ps1` passes, confirming no unreviewed `Debug|*` solution exclusions and keeping all first-party package/test projects enabled for Debug solution builds.
- CI passes on the release-candidate commit.
- Stable Release Validation passes on the release-candidate commit.
- Package metadata validation passes for generated `.nupkg` artifacts.
- Package SBOM generation passes for generated `.nupkg` artifacts.
- Template package smoke validation passes.
- External consumer smoke tests pass.
- DocFX documentation build passes.

## Package signing readiness

NuGet package signing remains an open supply-chain readiness item for `3.0.0` unless the release-preparation PR explicitly adopts and documents a reviewed signing process.

The release may include NuGet repository metadata, Source Link commit metadata, package SBOMs, and package/SBOM provenance artifacts where supported by the workflow event, but it should not describe packages as maintainer-signed, repository-signed, Authenticode-signed, tamper-evident, or legally non-repudiable by default.

If package signing becomes available in a later release, the release-preparation PR should update `SECURITY.md`, `Stable Release Validation`, the current release-readiness record, release notes, and consumer verification guidance before public wording claims signed package artifacts.

## Compatibility notes

Consumers already using `AsiBackbone.*` package IDs and namespaces should not need package rename or namespace migration work for `3.0.0`.

Because `AssemblyVersion` moves to `3.0.0.0`, consumers should update package references, rebuild, and validate host deployments, especially where strict assembly loading, plugin discovery, binding redirects, or binary compatibility assumptions are present.

Event Hubs, Purview, Azure-specific SDK adapters, Aspire runtime packages, robotics, immutable-storage, and additional provider packages remain outside the stable package contract unless separately reviewed and released.

## Post-publish checks

After packages are published and visible on NuGet, validate Source Link repository commit metadata:

```powershell
./scripts/Validate-Source-Link-commit-metadata.ps1 -Version 3.0.0
```

If package metadata, Source Link metadata, SBOM artifacts, provenance artifacts, or package-signing documentation are incorrect after publish, document the failure and prepare a follow-up patch rather than attempting to overwrite immutable NuGet package metadata.
