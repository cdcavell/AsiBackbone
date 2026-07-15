# AsiBackbone 3.0.1 Release Readiness Record

Release candidate date: 2026-07-13

## Release intent

`3.0.1` is a patch release for the stable `3.0.x` package family. It packages the post-`3.0.0` threat-model integrity corrections from issues `#612`, `#613`, and `#614` while preserving the established public and binary compatibility boundary.

This record is a pre-tag checklist. Do not create `v3.0.1` or publish packages until every required validation is complete on the final release candidate commit.

## Included corrections

- Reject undefined numeric `ThreatSeverity` and `GovernanceDecisionOutcome` values during `ThreatAssessment` construction.
- Route malformed contributor construction through the existing fail-closed contributor-exception policy when enabled.
- Reserve the entire case-insensitive, normalized `threat.*` metadata namespace for framework-generated provenance.
- Prevent contributor metadata from contradicting category, severity, confidence, contributor identity, recommended outcome, or effective outcome.
- Preserve the reason associated with the selected restrictive `Deferred`, `AcknowledgmentRequired`, or `EscalationRecommended` outcome.
- Retain the first matching reason in deterministic contributor order when multiple contributors return the same selected restrictive outcome.
- Preserve existing multi-reason denial aggregation and warning-only aggregation.

## Compatibility boundary

- Package IDs remain unchanged.
- Public namespaces remain unchanged.
- The target framework remains `net10.0`.
- `AssemblyVersion` remains `3.0.0.0` for the compatible `3.x` binary line.
- `FileVersion`, package version, informational version, citation metadata, and release metadata advance to `3.0.1`.
- No new public provider package or host execution capability is introduced.
- NuGet package signing remains deferred while the project is independently maintained.

The release contains intentional validation hardening. Consumers that construct threat assessments from unvalidated numeric or deserialized enum values, or that use the reserved `threat.*` namespace for custom metadata, must correct those inputs.

## Version and metadata checklist

- [ ] `Directory.Build.props` resolves package version `3.0.1`.
- [ ] `AssemblyVersion` remains `3.0.0.0`.
- [ ] `FileVersion` is `3.0.1.0`.
- [ ] `CITATION.cff` reports version `3.0.1` and the release date.
- [ ] `.zenodo.json` reports version `3.0.1` and patch-release scope.
- [ ] Template fallback package references use `3.0.1`.
- [ ] Source Link post-publication validation defaults to `3.0.1`.
- [ ] Changelog and release notes describe the same correction set and compatibility boundary.
- [ ] Evergreen documentation identifies `3.0.1` as the current patch release without rewriting the historical `3.0.0` major-release record.

## Required validation before tag

- [ ] Restore succeeds using the repository-locked SDK and package configuration.
- [ ] Debug solution build succeeds.
- [ ] Release solution build succeeds.
- [ ] `dotnet format --verify-no-changes` succeeds.
- [ ] All test projects pass.
- [ ] Repository-wide line-coverage gate passes.
- [ ] Package-specific coverage gates pass.
- [ ] Core branch-coverage gate passes.
- [ ] XML-documentation inventory ceiling passes.
- [ ] API baseline and compatibility checks pass.
- [ ] Version consistency validation passes for `3.0.1` and tag `v3.0.1`.
- [ ] Package creation succeeds for the complete publishable package set.
- [ ] Generated package IDs, versions, dependencies, repository metadata, symbols, and README content are correct.
- [ ] Template smoke tests succeed both against repository projects and package fallback references.
- [ ] External-consumer and stable-package smoke tests succeed.
- [ ] DocFX build and link validation succeed.
- [ ] Documentation release-claim validation succeeds.
- [ ] CodeQL and dependency review report no blocking findings.
- [ ] SBOM and provenance artifacts are produced where supported.
- [ ] No package-signing claim is made for unsigned packages.

## Release sequence

1. Merge the release-preparation pull request after required checks pass.
2. Confirm `main` contains the final `3.0.1` metadata and release documentation.
3. Create the annotated release tag `v3.0.1` from the validated commit.
4. Run the stable release workflow against that tag.
5. Confirm all expected NuGet packages and symbol packages are published from the official source.
6. Confirm GitHub release assets, SBOMs, and provenance artifacts are attached where supported.
7. Confirm documentation deployment succeeds.
8. Run post-publication Source Link validation:

```powershell
./scripts/Validate-Source-Link-commit-metadata.ps1 -Version 3.0.1
```

9. Verify that package repository commit metadata resolves to the tagged source commit.
10. Record any release exception explicitly rather than silently weakening the release claim.

## Final scope statement

AsiBackbone remains Accountable Systems Infrastructure for governed .NET decision flow. This patch improves the integrity of threat assessments and their explanatory evidence; it does not make AsiBackbone an intelligence engine, threat-detection product, robot controller, compliance certification, complete tamper-evidence platform, production key-management system, or production replay-protection system by default.
