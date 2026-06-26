# AsiBackbone 2.0.2 Release Readiness Record

This record tracks the release-candidate posture for `2.0.2`, a compatible patch release on the stable `2.0.x` package family.

## Release intent

`2.0.2` corrects the package-facing icon presentation issue discovered after `2.0.1` and aligns release metadata/documentation with the corrected package release.

The release preserves the `2.0.0` public package and namespace boundary. It does not intentionally change runtime behavior, expand public APIs, rename packages, or alter namespaces.

## Release classification

| Field | Value |
| --- | --- |
| Release | `2.0.2` |
| Type | Compatible patch |
| Stable line | `2.0.x` |
| Assembly identity | `2.0.0.0` |
| File version | `2.0.2.0` |
| Primary purpose | Correct package icon presentation metadata/assets |
| Public API expansion | No |
| Runtime behavior change | No intentional runtime behavior changes |
| Package ID changes | No |
| Namespace changes | No |

## Required release-candidate checks

Before tagging `v2.0.2`, confirm:

- [ ] `Directory.Build.props` uses `VersionPrefix` `2.0.2`.
- [ ] `AssemblyVersion` remains `2.0.0.0`.
- [ ] `FileVersion` is `2.0.2.0`.
- [ ] `CITATION.cff` references `2.0.2`.
- [ ] `.zenodo.json` references `2.0.2`.
- [ ] `PACKAGE-ICON.png` is regenerated from the source SVG and renders correctly as a complete icon.
- [ ] `2.0.2` release notes exist and clearly identify the icon correction as a package presentation fix.
- [ ] README, documentation home, article index, DocFX article TOC, release validation, and API compatibility / SemVer guidance reference `2.0.2` where current-release guidance is expected.
- [ ] Source Link post-publish validation defaults to `2.0.2`.
- [ ] Release notes state that no runtime behavior changes, public API expansion, package ID changes, or namespace changes are included.
- [ ] CI passes on the release-candidate commit.
- [ ] Stable Release Validation passes on the release-candidate commit.
- [ ] Package metadata validation passes for generated `.nupkg` artifacts.
- [ ] Package SBOM generation passes for generated `.nupkg` artifacts.
- [ ] Template package smoke validation passes.
- [ ] External consumer smoke tests pass.
- [ ] DocFX documentation build passes.

## Package icon validation

Because `2.0.2` exists to correct package presentation, verify the package icon before publication:

- [ ] `PACKAGE-ICON.png` is rebuilt from the source SVG.
- [ ] The icon renders as a complete image at small sizes.
- [ ] The icon is included in generated `.nupkg` artifacts at the expected package path.
- [ ] Generated NuGet metadata still references `PACKAGE-ICON.png`.
- [ ] Local package inspection or NuGet staging review confirms the icon is not partially rendered.

## Compatibility notes

Existing `2.0.0` and `2.0.1` consumers should be able to upgrade to `2.0.2` without required source-code changes.

`2.0.2` does not expand the stable package surface. Event Hubs, Purview, Azure-specific SDK adapters, Aspire runtime packages, robotics, immutable-storage, and additional provider packages remain outside the stable package contract unless separately reviewed and released.

## Post-publish checks

After packages are published and visible on NuGet, validate Source Link repository commit metadata:

```powershell
./scripts/Validate-Source-Link-commit-metadata.ps1 -Version 2.0.2
```

If package metadata, icon rendering, Source Link metadata, SBOM artifacts, or provenance artifacts are incorrect after publish, document the failure and prepare a follow-up patch rather than attempting to overwrite immutable NuGet package metadata.
