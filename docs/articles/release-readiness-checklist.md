# 1.0.0 Release Readiness Checklist

This checklist is the release-candidate control sheet for the first stable AsiBackbone `1.0.0` NuGet package release.

In this software project, **ASI** means **Accountable Systems Infrastructure**. This checklist verifies the package family as governance infrastructure for accountable decision flow. It does not treat AsiBackbone as an artificial superintelligence implementation, AI model host, robot controller, legal/compliance guarantee, signing system, or tamper-evident ledger provider.

## Stable package family

The first stable release covers only the implemented packages below.

| Package | Stable role | Release boundary |
| --- | --- | --- |
| `CDCavell.AsiBackbone.Core` | Framework-neutral governance primitives, decisions, constraints, audit contracts, acknowledgment contracts, capability-token abstractions, and operation results. | Core remains independent of ASP.NET Core, EF Core, cloud, robotics, AI model, and host-template dependencies. |
| `CDCavell.AsiBackbone.Storage.InMemory` | Non-durable in-memory storage helpers for tests, samples, and local validation. | Not durable storage, not production audit storage, and not a compliance archive. |
| `CDCavell.AsiBackbone.EntityFrameworkCore` | EF Core model configuration and host-owned persistence helpers. | Host owns `DbContext`, provider, migrations, deployment, schema lifecycle, retention, and access controls. |
| `CDCavell.AsiBackbone.AspNetCore` | Thin ASP.NET Core host adapters for actor context, request correlation, HTTP result mapping, and acknowledgment challenge flows. | Does not own host authentication, authorization, routing, policy enforcement, persistence, UI, or execution behavior. |

Future signing, key-management, durable outbox, cloud observability/enrichment, robotics, external gateway, and provider-specific packages remain outside the `1.0.0` stable contract unless separately reviewed and released as stable.

## Release-blocker issue status

| Item | Status | Evidence |
| --- | --- | --- |
| Stable API review | Complete | Issue #13 documents the public API and package-boundary review. |
| Version metadata and release artifacts | Complete | Issue #173 aligned `Directory.Build.props`, `CHANGELOG.md`, release notes, `CITATION.cff`, `.zenodo.json`, and stable wording. |
| `1.x` assembly-version strategy | Complete | Issue #174 documents the stable `AssemblyVersion` `1.0.0.0` strategy for compatible `1.x` releases. |
| Durable artifact schema stamping | Complete | Issue #175 added concrete schema-version stamping for package-owned durable or exported artifacts. |
| Public wording and NuGet metadata sweep | Complete | Issue #176 added generated `.nupkg` metadata validation and tightened package README/metadata wording. |

## Pre-tag hard gate

Do not cut the `v1.0.0` tag until every item in this section is complete for the exact release-candidate commit.

- [x] PR for issue #14 is merged to `main`.
- [x] `CI` passes on `main` after the issue #14 merge.
- [x] `Stable Release Validation` passes on `main` after the issue #14 merge.
- [x] `External Consumer Smoke Test` passes against the package-shaped artifacts for the release candidate.
- [x] `Publish Documentation` passes and the DocFX site reflects the stable `1.0.0` boundary.
- [x] `Version Consistency` passes for the release candidate.
- [x] No open release-blocking issues remain in the `1.0.0` milestone.
- [x] Any intentionally deferred release-critical check is documented in this checklist, the release notes, or a follow-up issue before tagging.

## Local release-candidate validation

Before relying only on hosted workflows, a maintainer may run the following commands from a clean checkout of the release-candidate commit.

```bash
dotnet restore AsiBackbone.slnx
dotnet build AsiBackbone.slnx --configuration Release
dotnet test AsiBackbone.slnx --configuration Release --no-build --no-restore
dotnet format AsiBackbone.slnx --verify-no-changes --verbosity minimal
dotnet tool restore
dotnet tool run docfx -- docs/docfx.json
```

Pack local release artifacts for inspection:

```bash
mkdir -p artifacts/packages
find ./src -name '*.csproj' -type f | sort | while read project; do
  dotnet pack "$project" --configuration Release --output artifacts/packages /p:ContinuousIntegrationBuild=true
done
```

Validate release identity and generated NuGet metadata:

```powershell
./scripts/Validate-VersionConsistency.ps1 -PackageDirectory artifacts/packages
./scripts/Validate-NuGetPackageMetadata.ps1 -PackageDirectory artifacts/packages
```

Run package-consumer smoke checks when the host environment can execute them:

```bash
bash ./eng/smoke-tests/external-consumer-smoke.sh
bash ./eng/smoke-tests/stable-package-integration-smoke.sh
```

## Generated `.nupkg` metadata inspection

The release candidate must inspect actual packed `.nupkg` files, not only project files.

`Validate-NuGetPackageMetadata.ps1` validates:

- package ID casing;
- package version;
- package descriptions;
- package tags;
- MIT license metadata;
- project URL and repository URL metadata;
- README metadata;
- packaged README presence;
- package-specific README wording anchors.

Expected package artifacts:

```text
CDCavell.AsiBackbone.Core.1.0.0.nupkg
CDCavell.AsiBackbone.Storage.InMemory.1.0.0.nupkg
CDCavell.AsiBackbone.EntityFrameworkCore.1.0.0.nupkg
CDCavell.AsiBackbone.AspNetCore.1.0.0.nupkg
```

## Documentation checklist

- [x] Root `README.md` describes the project as stable Accountable Systems Infrastructure and does not describe the stable package family as early alpha.
- [x] Package README files describe their package-specific boundaries.
- [x] `Storage.InMemory` is clearly non-durable and local-validation focused.
- [x] EF Core docs preserve host-owned persistence language.
- [x] ASP.NET Core docs preserve host-adapter language and do not imply automatic enforcement.
- [x] `1.0.0 Release Notes` list the stable package family and known limitations.
- [x] `API Compatibility and SemVer` documents the stable package contract and `AssemblyVersion` strategy.
- [x] `Schema Versioning` documents the current stable artifact schema and durable-artifact rationale.
- [x] `Privacy and Signing Boundaries` bounds signing, privacy, tamper, and compliance claims.
- [x] `Release Validation` documents the workflow gates and generated package metadata validation.

## Public wording checklist

Use bounded language such as:

- `Accountable Systems Infrastructure`;
- `governance spine`;
- `host-owned persistence`;
- `signing-ready fields`;
- `future provider work`;
- `non-durable in-memory storage`;
- `host-owned execution`.

Avoid unsupported claims such as:

- `tamper-proof`;
- `tamper-evident`, unless implemented by a documented signing/storage path;
- `non-repudiation`;
- `compliance guarantee`;
- `legal certification`;
- production audit guarantees not backed by implementation;
- language implying artificial superintelligence implementation, AI model hosting, or robot/physical-system control.

## Publish workflow gate

The `Publish AsiBackbone Packages` workflow must block package publication unless the validation-and-pack job succeeds.

Before using the workflow to publish to NuGet.org, confirm that the validation-and-pack job performs:

1. version metadata validation;
2. restore;
3. build;
4. formatting verification;
5. tests;
6. .NET tool restore;
7. DocFX build;
8. package creation;
9. generated package version validation;
10. generated NuGet metadata validation;
11. artifact upload.

The publish job should only run after those checks complete successfully.

## Tagging and publish sequence

Use this sequence for the final stable release:

1. Merge all release-readiness PRs to `main`.
2. Confirm release-blocking workflows pass on `main`.
3. Confirm generated `.nupkg` metadata validation passes for the release-candidate artifacts.
4. Confirm docs and release notes reflect the exact `1.0.0` boundary.
5. Create the `v1.0.0` tag only after the release candidate is accepted.
6. Let tag-triggered package validation run.
7. Publish only if the tag-triggered validation-and-pack job succeeds.
8. Do not manually replace published NuGet packages. If validation fails before publish, fix the release candidate and repeat the release process with documented rationale.

## Accepted mutation coverage deferral

Issue #178 records the accepted pre-`1.0.0` mutation-scope deferral. The current mutation reports remain limited to the Core governance path and the targeted ASP.NET Core acknowledgment challenge adapter path. The expanded gap list and `1.x` follow-up priority are documented in [Mutation Coverage Scope and Deferrals](../quality/mutation-coverage-scope.md).

The accepted `1.0.0` deferral includes correlation helpers outside the targeted challenge path, broader ASP.NET Core result mapping, additional acknowledgment challenge adapters, EF Core persistence edge cases, in-memory storage edge cases, and other integration-layer adapters that remain test-covered or smoke-validated but not mutation-validated.

This deferral is non-blocking while the regular test suite, coverage publication, external consumer smoke tests, stable release validation, generated package validation, and documentation build remain passing. Follow-up mutation expansion should be prioritized for the `1.x` line instead of broadening the pre-tag stable release gate.

## Non-blocking follow-up tracking

The following items are visible for post-`1.0.0` work and should not silently disappear, but they do not block `1.0.0` unless a new release-critical dependency is discovered.

| Follow-up | Status | Release decision |
| --- | --- | --- |
| Post-`1.0` public API baseline and architecture boundary checks | Tracked by issue #177. | Non-blocking for `1.0.0` if release notes and API review remain accurate. |
| Mutation coverage expansion after stabilization | Recorded by issue #178 and [Mutation Coverage Scope and Deferrals](../quality/mutation-coverage-scope.md). | Accepted pre-`1.0.0` deferral; non-blocking for `1.0.0`; prioritize Core carry-forward, ASP.NET Core result/correlation, EF Core edge-case, and integration-adapter mutation work in `1.x`. |
| Separate `CDCavell.AsiBackbone.Abstractions` package | Deferred. | Keep contracts in Core unless a concrete provider dependency problem appears. |
| Generated public API baseline files | Deferred. | Consider after `1.0.0` to detect unapproved public surface changes. |
| Architecture tests for package dependency boundaries | Deferred. | Consider after `1.0.0` to guard Core against accidental framework/provider dependencies. |

## Final release note

This checklist is a release-readiness aid, not a compliance certification. Passing it means the project has completed its documented software release gates for the implemented `1.0.0` package family. It does not certify legal compliance, regulatory approval, tamper-evidence, non-repudiation, production security, or operational safety for a consuming host application.
