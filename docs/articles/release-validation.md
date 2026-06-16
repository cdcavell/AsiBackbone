# Stable Release Validation

This article documents the release-blocking validation path for the current stable `1.x` release line. The current release target is `1.1.0`.

In this software project, **ASI** means **Accountable Systems Infrastructure**. Release validation should confirm that the package family remains practical governance infrastructure and that implementation claims stay within the documented software boundary.

Use [1.1.0 Release Readiness Checklist](release-readiness-checklist.md) as the final pre-tag control sheet that ties these workflow gates to package metadata inspection, wording boundaries, accepted deferrals, and follow-up tracking.

## Required checks before tagging `1.1.0`

Before cutting a stable release tag, confirm the following checks have passed on the release candidate commit:

| Check | Where it runs | Release purpose |
| --- | --- | --- |
| Version metadata validation | stable release validation, package publish | Confirms central MSBuild version metadata, citation metadata, Zenodo metadata, optional tag metadata, and generated package filenames align with the intended release version. |
| Restore | CI, stable release validation, package publish | Confirms package dependencies resolve for the solution. |
| Build | CI, stable release validation, package publish | Confirms all release projects compile in Release configuration and configured analyzers run through the build. |
| Formatting | CI, stable release validation, package publish | Confirms source formatting is stable before release. |
| Tests | CI, stable release validation, package publish | Confirms the solution test suite passes before packaging or publishing. |
| Package creation | CI, stable release validation, package publish | Confirms every package project under `src` can be packed. |
| Package version validation | stable release validation, package publish | Confirms generated package versions and, on tag builds, tag identity align with repository version metadata. |
| NuGet metadata validation | stable release validation, package publish | Confirms generated `.nupkg` metadata, README files, IDs, descriptions, tags, license metadata, project URL, and repository URL align with the stable package boundary. |
| Documentation build | publish docs, stable release validation, package publish | Confirms DocFX can build the documentation included in the release posture. |
| External consumer smoke tests | external consumer smoke workflow, stable release validation | Confirms clean consumer-style projects can reference and wire the package family. |
| CodeQL and dependency review | CI | Confirms configured static/security checks run on pull requests where applicable. |

## Release-blocking workflows

The following workflows form the release gate for `1.1.0`:

- `CI` validates dependency review for pull requests, solution restore/build/test, formatting, package creation, coverage output, and CodeQL analysis.
- `External Consumer Smoke Test` validates package-consumer wiring through the external consumer and stable package integration smoke scripts.
- `Publish Documentation` validates the DocFX build used for the documentation site.
- `Stable Release Validation` provides a single release-candidate gate for version metadata, restore, build, formatting, tests, DocFX, package creation, generated package version validation, generated NuGet metadata validation, and smoke checks.
- `Publish AsiBackbone Packages` repeats release-critical validation before package publish. The publish job depends on the validation-and-pack job, so a failed validation step blocks package publication.

## Tagging rule

Do not cut a stable `v1.1.0` tag until the release candidate commit has passed the release-blocking workflows above, or until any intentionally deferred check is documented in the release notes or release readiness checklist.

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
9. Package creation for all projects under `src`.
10. Generated package version metadata, including tag matching on tag builds.
11. Generated NuGet package metadata.
12. External consumer smoke test.
13. Stable package integration smoke test.
14. Package artifact upload.

## Package publish validation

The package publish workflow performs release-critical validation before publishing packages:

1. Validate version metadata.
2. Restore dependencies.
3. Build the solution.
4. Verify formatting.
5. Run tests.
6. Restore .NET tools.
7. Build DocFX documentation.
8. Pack every package project under `src`.
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
- README metadata and packaged README presence;
- package-specific README wording anchors, such as non-durable storage language for `Storage.InMemory`, provider-neutral wording for `OpenTelemetry`, and local-development/managed-key boundaries for signing packages.

This check is intended to catch release-blocking NuGet metadata mistakes before `1.1.0` is published, because NuGet package metadata for a published version cannot be overwritten.

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

- [1.1.0 Release Readiness Checklist](release-readiness-checklist.md)
- [1.1.0 Release Notes](release-notes-110.md)
- [Upgrade Guide: 1.0.0 to 1.1.0](upgrade-100-to-110.md)
- [Developer Checklist](developer-checklist.md)
- [API Compatibility and SemVer](api-compatibility-and-semver.md)
- [Schema Versioning](schema-versioning.md)
- [Privacy and Signing Boundaries](privacy-and-signing-boundaries.md)
