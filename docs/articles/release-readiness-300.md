# AsiBackbone 3.0.0 Release Readiness Record

This record defines the release-candidate posture for `3.0.0`, the first stable release on the `3.x` package family.

## Release intent

`3.0.0` establishes the current `3.x` stable release line while preserving the existing `AsiBackbone.*` package IDs and public namespaces.

The release advances AsiBackbone as provider-neutral Accountable Systems Infrastructure for governed .NET decision flow. It updates the binary assembly identity to `3.0.0.0`, intentionally targets `net10.0`, makes empty-policy and eligible policy-exception behavior fail closed by default, and incorporates governance, outbox, signing, verification, metadata-boundary, and release-quality hardening.

## Release classification

| Field | Value |
| --- | --- |
| Release | `3.0.0` |
| Type | Major release / new stable major line |
| Stable line | `3.x` |
| Target framework | `net10.0` |
| Assembly identity | `3.0.0.0` |
| File version | `3.0.0.0` |
| Package ID changes | No |
| Namespace changes | No |
| Runtime default changes | Yes: empty policies and eligible constraint exceptions fail closed by default |
| Package signing | Deferred while the project remains solo-maintained |

## Included release surfaces

- `Directory.Build.props` uses `VersionPrefix` `3.0.0`, `AssemblyVersion` `3.0.0.0`, and `FileVersion` `3.0.0.0`.
- `CITATION.cff`, `.zenodo.json`, `CHANGELOG.md`, release notes, and release readiness documentation identify `3.0.0` as the current release.
- Template fallback package references and Source Link post-publish validation target `3.0.0`.
- The package family intentionally targets `.NET 10` through `net10.0`.
- `DenyWhenNoConstraints` defaults to `true`.
- `TreatConstraintExceptionAsDenial` defaults to `true`.
- Threat-contributor failures eligible for conversion default to denied governance outcomes.
- Governance metadata sanitation, regulated and strict governance profiles, claim-based outbox processing, explicit claim-transition outcomes, managed-key retry hardening, provider metadata filtering, client correlation sanitation, and local-development signing key-size validation are included.
- Public API XML-documentation debt and package-specific coverage floors remain tracked, bounded release debt rather than closed debt.
- Consumer verification, target-framework support, production managed-key integration, governance standards crosswalk, and release-boundary documentation are present.

## Compatibility boundary

- Existing package IDs and public namespaces remain unchanged.
- Consumers should update references to `3.0.0`, rebuild, and validate host deployments.
- Consumers must use a .NET 10 SDK/runtime or later for the `3.0.x` line.
- Applications relying on strict assembly loading, plugin discovery, binding redirects, or binary identity assumptions should validate the move to `AssemblyVersion` `3.0.0.0`.
- Hosts that intentionally require permissive empty-policy behavior must set `DenyWhenNoConstraints = false`.
- Hosts that intentionally require propagated ordinary constraint exceptions must set `TreatConstraintExceptionAsDenial = false`.
- Local-development RSA key sizes below 2048 bits now fail configuration validation.
- Existing outbox claim convenience APIs remain available; consumers needing worker-attribution guarantees can use the outcome-aware transition contract.

## Required release-candidate checks

Before tagging `v3.0.0`, confirm:

- [ ] `Directory.Build.props` reports version `3.0.0`, assembly version `3.0.0.0`, file version `3.0.0.0`, and target framework `net10.0`.
- [ ] `CITATION.cff`, `.zenodo.json`, `CHANGELOG.md`, release notes, and release readiness records agree on `3.0.0` and the release date.
- [ ] README, documentation home, article navigation, release validation, API compatibility / SemVer guidance, templates, security posture, and governance wording identify `3.0.0` / `3.x` as current.
- [ ] The 3.0.0 Consumer Verification Guide is linked from the expected release and documentation surfaces.
- [ ] The Production Managed-Key Integration Guide remains provider-neutral and keeps concrete production key custody host-owned.
- [ ] NuGet package signing remains described as deferred; SBOMs and provenance are not presented as signed-package guarantees.
- [ ] `./scripts/Validate-DebugSolutionBuildCoverage.ps1` passes.
- [ ] `./scripts/Validate-XmlDocumentation.ps1 -Mode Inventory -Configuration Release -NoRestore` passes.
- [ ] `./scripts/Validate-PackageCoverageBaselines.ps1 -Configuration Release -NoBuild -NoRestore` passes after the Release build.
- [ ] CI passes on the release-candidate commit.
- [ ] Stable Release Validation passes on the release-candidate commit.
- [ ] Version Consistency and current-release documentation validation pass.
- [ ] CodeQL and dependency review pass.
- [ ] Package creation and generated NuGet metadata validation pass.
- [ ] Package SBOM generation and provenance handling pass where supported by the workflow event.
- [ ] Template package smoke validation passes.
- [ ] External consumer smoke tests pass.
- [ ] Stable-package integration smoke tests pass.
- [ ] DocFX documentation build and link validation pass.
- [ ] No open pull request remains intended for inclusion in `3.0.0`.

## Quality posture

`3.0.0` may ship only with the current quality debt explicit and bounded:

- repository-wide line coverage remains gated;
- Core branch coverage remains gated at 90%;
- selected adapter/provider packages produce independent package coverage artifacts and meet tracked floors;
- public API XML-documentation inventory cannot exceed tracked ceilings;
- targeted mutation reports remain inspectable quality evidence rather than hard release blockers unless the workflow explicitly says otherwise;
- release-line documentation validation prevents stale historical versions from being described as current.

## Package signing readiness

NuGet package signing is intentionally deferred while AsiBackbone remains solo-maintained. The release may publish repository metadata, Source Link commit metadata, SBOMs, and package/SBOM provenance artifacts where supported, but must not describe packages as maintainer-signed, repository-signed, Authenticode-signed, tamper-evident, or legally non-repudiable by default.

A later release may revisit signing only through a reviewed, documented process that updates security policy, release validation, consumer verification, release notes, and key/certificate custody guidance together.

## Runtime signing boundary

Runtime governance-residue signing is separate from NuGet package signing. `AsiBackbone.Signing.ManagedKey` remains provider-neutral. Hosts may connect Azure Key Vault, AWS KMS, GCP Cloud KMS, HSM, certificate-store, or enterprise key-management clients through the managed-key boundary, but the host owns credentials, concrete provider clients, key custody, rotation, verification, monitoring, incident response, and compliance interpretation.

## Release boundary

AsiBackbone remains a governance spine, not an intelligence engine, AI model host, robot controller, compliance certification, complete tamper-evidence platform, production key-management service, or production replay-protection system by default.

Event Hubs, Purview, Azure-specific non-signing adapters, Aspire runtime packages, robotics, immutable storage, and additional provider packages remain outside the stable contract unless separately reviewed and released.

## Tag and publish sequence

1. Merge the release-preparation PR after all required checks pass.
2. Confirm the final `main` commit is the intended immutable release source.
3. Create annotated tag `v3.0.0` from that commit.
4. Run the release/publish workflow from the tagged commit.
5. Confirm all expected NuGet packages are visible at version `3.0.0`.
6. Confirm GitHub release notes, SBOMs, provenance artifacts, and documentation publication are present where expected.
7. Validate Source Link repository commit metadata after NuGet publication.
8. Record any immutable publication defect and prepare a patch release rather than overwriting published package metadata.

## Post-publish checks

After packages are published and visible on NuGet, validate Source Link repository commit metadata:

```powershell
./scripts/Validate-Source-Link-commit-metadata.ps1 -Version 3.0.0
```

Consumers should then use the 3.0.0 Consumer Verification Guide to validate package source, IDs, versions, repository metadata, Source Link, SBOMs, provenance, target framework, and deferred package-signing posture.
