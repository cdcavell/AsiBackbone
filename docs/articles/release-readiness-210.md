# AsiBackbone 2.1.0 Release Readiness Record

This record tracks the release-candidate posture for `2.1.0`, a compatible minor release on the stable `2.x` package family.

## Release intent

`2.1.0` promotes backward-compatible policy-pipeline and audit-residue ergonomics into the current stable `2.x` line.

The release preserves the `2.0.0` public package and namespace boundary. It does not intentionally remove or rename public APIs, rename packages, or alter namespaces.

## Release classification

| Field | Value |
| --- | --- |
| Release | `2.1.0` |
| Type | Compatible minor |
| Stable line | `2.x` |
| Assembly identity | `2.0.0.0` |
| File version | `2.1.0.0` |
| Primary purpose | Backward-compatible public/API and adoption-surface expansion |
| Public API expansion | Yes, optional and backward-compatible |
| Runtime behavior change | Existing defaults preserved; new behavior is opt-in |
| Package ID changes | No |
| Namespace changes | No |

## Included release surfaces

- [ ] Optional policy evaluator first-denial fast-abort mode remains disabled by default.
- [ ] ASP.NET Core endpoint metadata for first-denial fast-abort remains descriptive until the host maps it into evaluator configuration.
- [ ] `AuditResidueBuilder` preserves existing direct factory semantics and delegates to the same validation/normalization path on `Build()`.
- [ ] In-memory governance outbox transition hardening preserves terminal delivered/dead-lettered state behavior.
- [ ] Benchmark project remains a measurement/developer-experience surface, not a runtime package requirement.
- [ ] Custom decision-policy examples remain host-owned orchestration guidance and do not imply package-owned execution.

## Required release-candidate checks

Before tagging `v2.1.0`, confirm:

- [ ] `Directory.Build.props` uses `VersionPrefix` `2.1.0`.
- [ ] `AssemblyVersion` remains `2.0.0.0`.
- [ ] `FileVersion` is `2.1.0.0`.
- [ ] `CITATION.cff` references `2.1.0`.
- [ ] `.zenodo.json` references `2.1.0`.
- [ ] `2.1.0` release notes exist and identify the release as a compatible minor release.
- [ ] README, documentation home, article index, DocFX article TOC, release validation, and API compatibility / SemVer guidance reference `2.1.0` where current-release guidance is expected.
- [ ] Source Link post-publish validation defaults to `2.1.0`.
- [ ] Release notes state that no package ID or namespace changes are included.
- [ ] Release notes state that existing `2.0.x` consumers should be able to upgrade without required source-code changes for existing APIs.
- [ ] CI passes on the release-candidate commit.
- [ ] Stable Release Validation passes on the release-candidate commit.
- [ ] Package metadata validation passes for generated `.nupkg` artifacts.
- [ ] Package SBOM generation passes for generated `.nupkg` artifacts.
- [ ] Template package smoke validation passes.
- [ ] External consumer smoke tests pass.
- [ ] DocFX documentation build passes.

## Compatibility notes

Existing `2.0.0`, `2.0.1`, and `2.0.2` consumers should be able to upgrade to `2.1.0` without required source-code changes for existing APIs.

`2.1.0` expands the stable package surface in a compatible way. Event Hubs, Purview, Azure-specific SDK adapters, Aspire runtime packages, robotics, immutable-storage, and additional provider packages remain outside the stable package contract unless separately reviewed and released.

## Post-publish checks

After packages are published and visible on NuGet, validate Source Link repository commit metadata:

```powershell
./scripts/Validate-Source-Link-commit-metadata.ps1 -Version 2.1.0
```

If package metadata, Source Link metadata, SBOM artifacts, or provenance artifacts are incorrect after publish, document the failure and prepare a follow-up patch rather than attempting to overwrite immutable NuGet package metadata.
