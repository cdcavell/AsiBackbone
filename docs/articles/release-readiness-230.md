# AsiBackbone 2.3.0 Release Readiness Record

This record tracks the release-candidate posture for `2.3.0`, a compatible minor release on the stable `2.x` package family.

## Release intent

`2.3.0` promotes host-facing governance guardrails, safer signing defaults, diagnostic policy signals, outbox/query hardening, endpoint-governance validation cleanup, and documentation alignment into the current stable `2.x` line.

The release preserves the `2.0.0` public package and namespace boundary. It does not intentionally remove or rename public APIs, rename packages, or alter namespaces.

## Release classification

| Field | Value |
| --- | --- |
| Release | `2.3.0` |
| Type | Compatible minor |
| Stable line | `2.x` |
| Assembly identity | `2.0.0.0` |
| File version | `2.3.0.0` |
| Primary purpose | Governance guardrails, fail-closed signing defaults, outbox/query hardening, and documentation alignment |
| Public API expansion | Yes; optional helper APIs, options, validators, and local-validation registration helpers |
| Runtime behavior change | Yes; managed-key signing fails closed by default in production-oriented paths |
| Package ID changes | No |
| Namespace changes | No |

## Included release surfaces

- Metadata budget validation helper APIs are present and documented for host-owned audit, telemetry, and signing metadata boundaries.
- Constraint exception conversion to denied governance decisions is opt-in and configurable.
- Managed-key signing defaults fail closed in production-oriented paths, with explicit local-validation opt-in helpers for unsigned failure metadata.
- Empty-policy permissive evaluation emits an optional warning signal while preserving existing default behavior.
- EF Core governance outbox selection query filtering, ordering, deterministic tie-breakers, and index guidance are updated.
- Endpoint-governance request-time option validation is removed while startup/configured-options validation remains the fail-closed production posture.
- Template fallback package versions are aligned with `2.3.0`.
- Outbox claim/lease design direction is documented as future opt-in work.
- Governance decision collection-backed reason normalization uses a single enumeration pass for non-empty `ICollection<OperationReason>` inputs while preserving fallback behavior.
- Dependency/tooling updates are incorporated for BenchmarkDotNet, dotnet-stryker, Roslyn analyzer packages, and CodeQL actions.

## Required release-candidate checks

Before tagging `v2.3.0`, confirm:

- `Directory.Build.props` uses `VersionPrefix` `2.3.0`.
- `AssemblyVersion` remains `2.0.0.0`.
- `FileVersion` is `2.3.0.0`.
- `CITATION.cff` references `2.3.0`.
- `.zenodo.json` references `2.3.0`.
- `2.3.0` release notes exist and identify the release as a compatible minor release.
- `CHANGELOG.md` includes a `2.3.0` entry.
- README, documentation home, article index, DocFX article TOC, release validation, templates guidance, and API compatibility / SemVer guidance reference `2.3.0` where current-release guidance is expected.
- Source Link post-publish validation defaults to `2.3.0`.
- Template fallback `PackageReference` versions use `2.3.0`.
- Release notes state that no package ID or namespace changes are included.
- Release notes document the managed-key signing fail-closed default and the explicit local-validation opt-in path.
- Release notes state that NuGet package signing remains deferred and that `2.3.0` packages should not be described as maintainer-signed, repository-signed, or Authenticode-signed.
- Release notes state that existing APIs should continue to compile, while hosts relying on managed-key unsigned failure metadata should review the behavioral hardening note.
- CI passes on the release-candidate commit.
- Stable Release Validation passes on the release-candidate commit.
- Package metadata validation passes for generated `.nupkg` artifacts.
- Package SBOM generation passes for generated `.nupkg` artifacts.
- Template package smoke validation passes.
- External consumer smoke tests pass.
- DocFX documentation build passes.

## Package signing readiness

NuGet package signing remains an open supply-chain readiness item for `2.3.0`. This release may include NuGet repository metadata, Source Link commit metadata, package SBOMs, and package/SBOM provenance artifacts where supported by the workflow event, but it does not introduce maintainer-signed, repository-signed, or Authenticode-signed package artifacts.

If package signing becomes available in a later release, the release-preparation PR should update `SECURITY.md`, `Stable Release Validation`, the current release-readiness record, the release notes, and consumer verification guidance before public wording claims signed package artifacts.

## Compatibility notes

Existing `2.0.x`, `2.1.x`, `2.2.0`, and `2.2.1` consumers should continue to compile against existing APIs.

`2.3.0` is a minor release because it includes optional public/helper APIs, host-facing option surfaces, validation helpers, tests, documentation, and release metadata alignment. The managed-key signing provider now fails closed by default in production-oriented registration paths; hosts that intentionally require unsigned failure metadata should opt into local-validation or explicit fallback behavior.

Event Hubs, Purview, Azure-specific SDK adapters, Aspire runtime packages, robotics, immutable-storage, and additional provider packages remain outside the stable package contract unless separately reviewed and released.

## Post-publish checks

After packages are published and visible on NuGet, validate Source Link repository commit metadata:

```powershell
./scripts/Validate-Source-Link-commit-metadata.ps1 -Version 2.3.0
```

If package metadata, Source Link metadata, SBOM artifacts, provenance artifacts, or package-signing documentation are incorrect after publish, document the failure and prepare a follow-up patch rather than attempting to overwrite immutable NuGet package metadata.