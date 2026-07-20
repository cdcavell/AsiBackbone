# AsiBackbone 3.1.0 Release Readiness Record

Release candidate date: 2026-07-20

## Release intent

`3.1.0` is a backward-compatible minor release for the stable `3.x` package family. It packages the post-`3.0.1` host-accountability, capability-proof trust, actor-claim trust-boundary, audit-integrity, reliability, and supply-chain hardening work while preserving the established package, namespace, target-framework, binary-identity, and host-ownership boundaries.

This record is a pre-tag checklist. Do not create `v3.1.0` or publish packages until every required validation is complete on the final release-candidate commit.

## Included scope

- Add provider-neutral governed execution completion receipts and persistence outcomes.
- Add canonical execution-receipt payload construction, host-accountability metadata keys, and lifecycle helpers.
- Add optional capability-proof trust pins for key, policy, provider, and hash-algorithm metadata.
- Add a conservative actor-type claim allow-list and authenticated fallback behavior.
- Add an isolated, non-packable NCAT audit-completion reference adapter and tests.
- Clarify audit-integrity sequence ownership and duplicate/fork precedence.
- Cancel losing hosted-outbox delay tasks when runtime options change.
- Expand property-based tests and repository supply-chain/security workflows.

## Compatibility boundary

- Package IDs remain unchanged.
- Public namespaces remain unchanged.
- The target framework remains `net10.0`.
- `AssemblyVersion` remains `3.0.0.0` for the compatible `3.x` binary line.
- `FileVersion`, package version, informational version, citation metadata, and release metadata advance to `3.1.0`.
- New Core and ASP.NET Core APIs are additive.
- Privileged actor types from HTTP claims require explicit host opt-in; the conservative default accepts only `Human`.
- Capability-proof trust pins remain optional.
- The NCAT adapter remains sample-only, non-packable, and outside the required package graph.
- NuGet package signing remains deferred while the project is independently maintained.

## Version and metadata checklist

- [ ] `Directory.Build.props` resolves package version `3.1.0`.
- [ ] `AssemblyVersion` remains `3.0.0.0`.
- [ ] `FileVersion` is `3.1.0.0`.
- [ ] `CITATION.cff` reports version `3.1.0` and the release date.
- [ ] `.zenodo.json` reports version `3.1.0` and minor-release scope.
- [ ] Template fallback package references use `3.1.0`.
- [ ] Source Link post-publication validation defaults to `3.1.0`.
- [ ] Lock files are regenerated after the version bump and locked restore succeeds.
- [ ] Changelog and release notes describe the same change set and compatibility boundary.
- [ ] Evergreen documentation identifies `3.1.0` as the current minor release without rewriting historical release records.

## Required validation before tag

- [ ] Restore succeeds in locked mode using the repository SDK and package configuration.
- [ ] Debug solution build succeeds.
- [ ] Release solution build succeeds.
- [ ] `dotnet format --verify-no-changes` succeeds.
- [ ] All test projects pass.
- [ ] Repository-wide line-coverage gate passes.
- [ ] Package-specific coverage gates pass.
- [ ] Core branch-coverage gate passes.
- [ ] XML-documentation inventory ceiling passes.
- [ ] API baseline and compatibility checks pass.
- [ ] Version consistency validation passes for `3.1.0` and tag `v3.1.0`.
- [ ] Package creation succeeds for the complete publishable package set.
- [ ] Generated package IDs, versions, dependencies, repository metadata, symbols, and README content are correct.
- [ ] Template smoke tests succeed against repository projects and package fallback references.
- [ ] External-consumer and stable-package smoke tests succeed.
- [ ] DocFX build and documentation release-claim validation succeed.
- [ ] CodeQL and dependency review report no blocking findings.
- [ ] OpenSSF Scorecard, workflow-security, actionlint/Zizmor, and OWASP Dependency-Check results have no unexplained blocking findings.
- [ ] Reviewed OWASP suppressions remain narrowly scoped, documented, and unexpired.
- [ ] SBOM and provenance artifacts are produced where supported.
- [ ] No package-signing claim is made for unsigned packages.

## Release sequence

1. Add the prepared `3.1.0` entry to `CHANGELOG.md` and confirm consistency with the release notes.
2. Regenerate and commit all NuGet lock files after the central package-version change.
3. Merge the release-preparation pull request after required checks pass.
4. Confirm `main` contains the final `3.1.0` metadata and release documentation.
5. Create the annotated release tag `v3.1.0` from the validated commit.
6. Run the stable release workflow against that tag.
7. Confirm all expected NuGet and symbol packages are published from the official source.
8. Confirm GitHub release assets, SBOMs, and provenance artifacts are attached where supported.
9. Confirm documentation deployment succeeds.
10. Run post-publication Source Link validation:

```powershell
./scripts/Validate-Source-Link-commit-metadata.ps1 -Version 3.1.0
```

11. Verify that package repository commit metadata resolves to the tagged source commit.
12. Record any release exception explicitly rather than silently weakening the release claim.

## Final scope statement

AsiBackbone remains Accountable Systems Infrastructure for governed .NET decision flow. This minor release strengthens the evidence chain between governance decisions and host-owned execution outcomes and narrows selected trust boundaries; it does not make AsiBackbone an intelligence engine, host executor, robot controller, compliance certification, complete tamper-evidence platform, production key-management system, or production replay-protection system by default.
