# AsiBackbone 2.2.0 Release Readiness Record

This record tracks the release-candidate posture for `2.2.0`, a compatible minor release on the stable `2.x` package family.

## Release intent

`2.2.0` promotes backward-compatible endpoint-governance metadata controls and hot-path allocation refinements into the current stable `2.x` line.

The release preserves the `2.0.0` public package and namespace boundary. It does not intentionally remove or rename public APIs, rename packages, or alter namespaces.

## Release classification

| Field | Value |
| --- | --- |
| Release | `2.2.0` |
| Type | Compatible minor |
| Stable line | `2.x` |
| Assembly identity | `2.0.0.0` |
| File version | `2.2.0.0` |
| Primary purpose | Backward-compatible endpoint-governance metadata control and hot-path allocation refinement |
| Public API expansion | Yes, optional and backward-compatible |
| Runtime behavior change | Existing defaults preserved; reduced metadata behavior is opt-in |
| Package ID changes | No |
| Namespace changes | No |

## Included release surfaces

- [ ] `AsiBackboneEndpointGovernanceMetadataMode.Full` remains the default metadata behavior.
- [ ] `AsiBackboneEndpointGovernanceMetadataMode.Reduced` forwards only `endpoint.operation_name` through the endpoint-governance metadata dictionary.
- [ ] `AsiBackboneEndpointGovernanceOptions.MetadataMode` is optional and defaults to `Full`.
- [ ] Endpoint descriptor metadata projection supports both full and reduced metadata modes.
- [ ] Endpoint governance evaluation, audit residue metadata, acknowledgment challenge metadata, and development diagnostics honor the configured metadata mode.
- [ ] Development diagnostics include the configured `metadataMode` field.
- [ ] Endpoint governance optional service lookups remain optional and are resolved at most once per evaluation.
- [ ] `GovernanceDecision.NormalizeReasons` preserves null filtering, fallback reasons, and read-only exposure while reducing temporary allocation churn.

## Required release-candidate checks

Before tagging `v2.2.0`, confirm:

- [ ] `Directory.Build.props` uses `VersionPrefix` `2.2.0`.
- [ ] `AssemblyVersion` remains `2.0.0.0`.
- [ ] `FileVersion` is `2.2.0.0`.
- [ ] `CITATION.cff` references `2.2.0`.
- [ ] `.zenodo.json` references `2.2.0`.
- [ ] `2.2.0` release notes exist and identify the release as a compatible minor release.
- [ ] `CHANGELOG.md` includes a `2.2.0` entry.
- [ ] README, documentation home, article index, DocFX article TOC, release validation, and API compatibility / SemVer guidance reference `2.2.0` where current-release guidance is expected.
- [ ] Source Link post-publish validation defaults to `2.2.0`.
- [ ] Release notes state that no package ID or namespace changes are included.
- [ ] Release notes state that existing `2.0.x` and `2.1.x` consumers should be able to upgrade without required source-code changes for existing APIs.
- [ ] CI passes on the release-candidate commit.
- [ ] Stable Release Validation passes on the release-candidate commit.
- [ ] Package metadata validation passes for generated `.nupkg` artifacts.
- [ ] Package SBOM generation passes for generated `.nupkg` artifacts.
- [ ] Template package smoke validation passes.
- [ ] External consumer smoke tests pass.
- [ ] DocFX documentation build passes.

## Compatibility notes

Existing `2.0.x` and `2.1.x` consumers should be able to upgrade to `2.2.0` without required source-code changes for existing APIs.

`2.2.0` expands the stable package surface in a compatible way. Event Hubs, Purview, Azure-specific SDK adapters, Aspire runtime packages, robotics, immutable-storage, and additional provider packages remain outside the stable package contract unless separately reviewed and released.

## Post-publish checks

After packages are published and visible on NuGet, validate Source Link repository commit metadata:

```powershell
./scripts/Validate-Source-Link-commit-metadata.ps1 -Version 2.2.0
```

If package metadata, Source Link metadata, SBOM artifacts, or provenance artifacts are incorrect after publish, document the failure and prepare a follow-up patch rather than attempting to overwrite immutable NuGet package metadata.
