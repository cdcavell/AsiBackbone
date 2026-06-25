# Stable Release Validation

This article documents the reusable release-blocking validation path for the stable `1.x` release line. The current released stable package family is `1.2.x`, with `1.2.1` as the current patch release on the `1.2.0` minor-release boundary; future `1.x` maintenance releases should continue to use the same validation posture unless a later release note supersedes it.

In this software project, **ASI** means **Accountable Systems Infrastructure**. Release validation should confirm that the package family remains practical governance infrastructure and that implementation claims stay within the documented software boundary.

The [1.2.1 Release Readiness Record](release-readiness-121.md) is the current release-candidate control sheet for the `1.2.1` release. The [1.2.0 Release Readiness Record](release-readiness-120.md) and [Historical 1.1.0 Release Readiness Record](release-readiness-checklist.md) are retained for traceability and checklist-shape history.

## Required checks before tagging a stable release

Before cutting a stable release tag, confirm the following checks have passed on the release-candidate commit:

| Check | Where it runs | Release purpose |
| --- | --- | --- |
| Version metadata validation | stable release validation, package publish | Confirms central MSBuild version metadata, citation metadata, Zenodo metadata, optional tag metadata, and generated package filenames align with the intended release version. |
| Restore | CI, stable release validation, package publish | Confirms package dependencies resolve for the solution. |
| Build | CI, stable release validation, package publish | Confirms all release projects compile in Release configuration and configured analyzers run through the build. |
| Formatting | CI, stable release validation, package publish | Confirms source formatting is stable before release. |
| Tests | CI, stable release validation, package publish | Confirms the solution test suite passes before packaging or publishing. |
| Package creation | CI, stable release validation, package publish | Confirms every package project under `src`, excluding template-content projects, can be packed. |
| Package version validation | stable release validation, package publish | Confirms generated package versions and, on tag builds, tag identity align with repository version metadata. |
| NuGet metadata validation | stable release validation, package publish | Confirms generated `.nupkg` metadata, README files, IDs, descriptions, tags, license metadata, project URL, repository URL, repository commit metadata, and stable package boundary wording align before publication. |
| Template package smoke validation | CI, stable release validation | Confirms the packed `AsiBackbone.Templates` package can be installed, generate supported host styles, restore, and build. |
| Documentation build | publish docs, stable release validation, package publish | Confirms DocFX can build the documentation included in the release posture. |
| External consumer smoke tests | external consumer smoke workflow, stable release validation | Confirms clean consumer-style projects can reference and wire the package family. |
| CodeQL and dependency review | CI | Confirms configured static/security checks run on pull requests where applicable. |
| Source Link metadata validation | manual post-publish validation | Confirms published NuGet packages include expected repository type, repository URL, and Source Link repository commit metadata after packages are available from NuGet. |

## Release-blocking workflows

The following workflows form the reusable gate for stable `1.x` release candidates:

- `CI` validates dependency review for pull requests, solution restore/build/test, formatting, package creation, template package smoke validation, coverage output, and CodeQL analysis.
- `External Consumer Smoke Test` validates package-consumer wiring through the external consumer and stable package integration smoke scripts.
- `Publish Documentation` validates the DocFX build used for the documentation site.
- `Stable Release Validation` provides a single release-candidate gate for version metadata, restore, build, formatting, tests, DocFX, package creation, generated package version validation, generated NuGet metadata validation, template package smoke validation, and smoke checks.
- `Publish AsiBackbone Packages` repeats release-critical validation before package publish. The publish job depends on the validation-and-pack job, so a failed validation step blocks package publication.

## Tagging rule

Do not cut a stable release tag until the release-candidate commit has passed the release-blocking workflows above, or until any intentionally deferred check is documented in the release notes or release readiness record for that release.

If a tag is pushed and package validation fails, do not publish replacement packages by hand. Fix the release candidate, document the reason, and repeat the release process with a corrected tag or clearly documented follow-up plan.

## Stable Release Validation workflow

The `Stable Release Validation` workflow runs on:

- pull requests to `main`;
- pushes to `main`;
- `v*.*.*` tags;
- manual dispatch.

The workflow validates:

1. .NET SDK setup from `global.json`.
2. Version metadata before restore/build.
3. Solution restore.
4. Release build with `ContinuousIntegrationBuild=true`.
5. Source formatting.
6. Full solution tests.
7. .NET tool restore.
8. DocFX documentation build.
9. Package creation for package projects under `src`, excluding template-content projects under `*/templates/*`.
10. Generated package version metadata, including tag matching on tag builds.
11. Generated NuGet package metadata.
12. Template package installation, generation, restore, and build smoke validation.
13. External consumer smoke test.
14. Stable package integration smoke test.
15. Package artifact upload.

## Package publish validation

The package publish workflow performs release-critical validation before publishing packages:

1. Validate version metadata.
2. Restore dependencies.
3. Build the solution.
4. Verify formatting.
5. Run tests.
6. Restore .NET tools.
7. Build DocFX documentation.
8. Pack every package project under `src`, excluding template-content projects under `*/templates/*`.
9. Validate generated package versions.
10. Validate generated NuGet metadata.
11. Upload package artifacts.
12. Publish only after the validation-and-pack job succeeds.

This keeps package publication behind restore, build, test, documentation, package creation, version checks, and generated package metadata checks.

## NuGet metadata validation

`Validate-NuGetPackageMetadata.ps1` inspects generated `.nupkg` files rather than only project files. It validates:

- package ID casing;
- package version;
- package descriptions;
- package tags;
- MIT license metadata;
- project URL and repository URL metadata;
- repository commit metadata when repository metadata is present;
- README metadata and packaged README presence;
- package-specific README wording anchors, such as non-durable storage language for `Storage.InMemory`, provider-neutral wording for `OpenTelemetry`, local-development/managed-key boundaries for signing packages, test-harness boundaries for `Testing`, and template-scaffold boundaries for `Templates`.

This check catches release-blocking NuGet metadata mistakes before package publication because NuGet package metadata for a published version cannot be overwritten.

## Source Link metadata validation

After `1.2.1` packages are published and visible on NuGet, maintainers should run:

```powershell
./scripts/Validate-Source-Link-commit-metadata.ps1 -Version 1.2.1
```

This post-publish check downloads the published packages and confirms the expected repository type, repository URL, and non-empty repository commit metadata are present.

## Checks intentionally not owned by the package

These checks do not turn AsiBackbone into a compliance product or operational enforcement system. The host remains responsible for:

- production authentication and authorization;
- database provider and migration strategy;
- environment-specific deployment checks;
- privacy review and metadata handling;
- signing and key-management providers;
- verification policy;
- storage immutability or tamper-evidence;
- exporter/backend configuration;
- legal, regulatory, or audit-framework certification.

## Deferred checks

If a release-critical check is intentionally deferred, document:

- the deferred check;
- why it was deferred;
- the risk accepted;
- the follow-up issue or milestone;
- whether the release notes need to mention it.

Deferred checks should be rare for a stable release.

## Related documentation

- [1.2.1 Release Readiness Record](release-readiness-121.md)
- [1.2.1 Release Notes](release-notes-121.md)
- [1.2.0 Release Readiness Record](release-readiness-120.md)
- [1.2.0 Release Notes](release-notes-120.md)
- [Historical 1.1.0 Release Readiness Record](release-readiness-checklist.md)
- [1.1.x Release Notes](release-notes-110.md)
- [Upgrade Guide: 1.0.0 to 1.1.0](upgrade-100-to-110.md)
- [Developer Checklist](developer-checklist.md)
- [API Compatibility and SemVer](api-compatibility-and-semver.md)
- [Schema Versioning](schema-versioning.md)
- [Privacy and Signing Boundaries](privacy-and-signing-boundaries.md)
- [dotnet new Templates](templates.md)
