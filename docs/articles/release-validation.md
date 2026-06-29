# Stable Release Validation

This article documents the reusable release-blocking validation path for stable release lines. The current released stable package family is `2.x`, with `2.1.1` as the current compatible minor release and `2.0.0` as the major release boundary for the simplified `AsiBackbone.*` package and namespace identity.

In this software project, **ASI** means **Accountable Systems Infrastructure**. Release validation should confirm that the package family remains practical governance infrastructure and that implementation claims stay within the documented software boundary. See [Release Cadence and Readiness](release-cadence-and-readiness.md) for the release-stream and stabilization guidance that complements this checklist.

The [2.1.0 Release Readiness Record](release-readiness-210.md) is the current release-candidate control sheet for the `2.1.0` release. Earlier readiness records are retained for traceability.

## Required checks before tagging a stable release

Before cutting a stable release tag, confirm the following checks have passed on the release-candidate commit:

| Check | Where it runs | Release purpose |
| --- | --- | --- |
| Release stream classification | Release PR, release readiness record | Confirms the release is correctly classified as patch, minor, major, or preview. |
| Version metadata validation | stable release validation, package publish | Confirms MSBuild version metadata, citation metadata, Zenodo metadata, optional tag metadata, and generated package filenames align. |
| Restore | CI, stable release validation, package publish | Confirms package dependencies resolve for the solution. |
| Build | CI, stable release validation, package publish | Confirms release projects compile in Release configuration. |
| Formatting | CI, stable release validation, package publish | Confirms source formatting is stable before release. |
| Tests | CI, stable release validation, package publish | Confirms the solution test suite passes before packaging or publishing. |
| Package creation | CI, stable release validation, package publish | Confirms every package project under `src`, excluding template-content projects, can be packed. |
| Package version validation | stable release validation, package publish | Confirms generated package versions and tag identity align with repository version metadata. |
| NuGet metadata validation | stable release validation, package publish | Confirms generated `.nupkg` metadata, README files, IDs, descriptions, tags, license metadata, project URL, repository URL, and repository commit metadata align before publication. |
| Package metadata asset checklist | release readiness record, generated package inspection | Confirms package icons, packaged README rendering, NuGet metadata, Source Link metadata, SBOM/provenance artifacts, and documentation links are reviewed. |
| Package SBOM generation | CI, stable release validation, package publish | Generates SPDX JSON SBOM files and an SBOM manifest for generated `.nupkg` artifacts. |
| Package provenance attestation | CI on non-PR events, stable release validation on non-PR events, package publish | Attests generated package and SBOM artifacts where supported. |
| Template package smoke validation | CI, stable release validation | Confirms the packed `AsiBackbone.Templates` package can be installed, generate supported host styles, restore, and build. |
| Documentation build | publish docs, stable release validation, package publish | Confirms DocFX can build the documentation included in the release posture. |
| Documentation link review | release readiness record, manual docs review | Confirms README links, DocFX navigation, release notes, migration guides, package documentation links, and GitHub Pages links point to current pages. |
| External consumer smoke tests | external consumer smoke workflow, stable release validation | Confirms clean consumer-style projects can reference and wire the package family. |
| Source Link metadata validation | manual post-publish validation | Confirms published NuGet packages include expected repository type, repository URL, and repository commit metadata. |

## Release-blocking workflows

The following workflows form the reusable gate for stable release candidates:

- `CI` validates dependency review for pull requests, solution restore/build/test, formatting, package creation, package SBOM generation, template package smoke validation, coverage output, and CodeQL analysis.
- `External Consumer Smoke Test` validates package-consumer wiring through the external consumer and stable package integration smoke scripts.
- `Publish Documentation` validates the DocFX build used for the documentation site.
- `Stable Release Validation` provides a single release-candidate gate for version metadata, restore, build, formatting, tests, DocFX, package creation, generated package version validation, generated NuGet metadata validation, SBOM generation, package/SBOM provenance attestation where supported, template package smoke validation, and smoke checks.
- `Publish AsiBackbone Packages` repeats release-critical validation before package publish.

## Tagging rule

Do not cut a stable release tag until the release-candidate commit has passed the release-blocking workflows above, or until any intentionally deferred check is documented in the release notes or release readiness record for that release.

If a tag is pushed and package validation fails, do not publish replacement packages by hand. Fix the release candidate, document the reason, and repeat the release process with a corrected tag or clearly documented follow-up plan.

## Stable Release Validation workflow

The `Stable Release Validation` workflow runs on pull requests to `main`, pushes to `main`, `v*.*.*` tags, and manual dispatch.

The workflow validates .NET SDK setup, version metadata, restore, Release build, formatting, tests, tool restore, DocFX, package creation, package versions, NuGet metadata, package SBOM generation, template package smoke validation, external consumer smoke tests, stable package integration smoke tests, provenance handling where supported, and artifact upload.

## Package publish validation

The package publish workflow performs release-critical validation before publishing packages. It validates version metadata, restores dependencies, builds the solution, verifies formatting, runs tests, restores .NET tools, builds DocFX documentation, packs package projects, validates generated package versions and NuGet metadata, generates SBOMs, handles provenance where supported, uploads artifacts, and publishes only after validation succeeds.

## NuGet metadata validation

`Validate-NuGetPackageMetadata.ps1` inspects generated `.nupkg` files rather than only project files. It validates package ID casing, package version, package descriptions, package tags, license metadata, project URL, repository URL metadata, repository commit metadata when available, README metadata, packaged README presence, package icon metadata, packaged icon presence, and package-specific README wording anchors.

This check catches release-blocking NuGet metadata mistakes before package publication because NuGet package metadata for a published version cannot be overwritten.

## Pre-release metadata and asset checklist

For every stable release, the release readiness record should explicitly confirm:

- package icon source, generated PNG, package inclusion, and small-size rendering are acceptable;
- packaged README files are present and render acceptably in generated packages;
- NuGet metadata is correct for package ID, version, description, tags, license, project URL, repository URL, repository type, and repository commit where available;
- Source Link repository commit metadata is generated and has a post-publish validation plan when NuGet download is required to confirm it;
- package SBOM files and `sbom-manifest.json` are generated for produced `.nupkg` artifacts;
- package and SBOM provenance artifacts are uploaded and attested where the workflow event supports attestation;
- README, DocFX navigation, release notes, migration notes, package README links, and GitHub Pages links are current;
- any intentionally deferred metadata, asset, Source Link, SBOM, provenance, or documentation-link check is recorded with risk and follow-up.

## Source Link metadata validation

After `2.1.1` packages are published and visible on NuGet, maintainers should run:

```powershell
./scripts/Validate-Source-Link-commit-metadata.ps1 -Version 2.1.1
```

This post-publish check downloads the published packages and confirms the expected repository type, repository URL, and non-empty repository commit metadata are present.

## Deferred checks

If a release-critical check is intentionally deferred, document the deferred check, the reason, the accepted risk, the follow-up issue or milestone, and whether release notes need to mention it.

Deferred checks should be rare for a stable release.

## Related documentation

- [Release Cadence and Readiness](release-cadence-and-readiness.md)
- [Supply-Chain Provenance and Package SBOMs](supply-chain-provenance.md)
- [2.1.0 Release Readiness Record](release-readiness-210.md)
- [2.1.0 Release Notes](release-notes-210.md)
- [2.0.2 Release Readiness Record](release-readiness-202.md)
- [2.0.2 Release Notes](release-notes-202.md)
- [2.0.1 Release Readiness Record](release-readiness-201.md)
- [2.0.1 Release Notes](release-notes-201.md)
- [2.0.0 Release Readiness Record](release-readiness-200.md)
- [2.0.0 Release Notes](release-notes-200.md)
- [API Compatibility and SemVer](api-compatibility-and-semver.md)
- [Schema Versioning](schema-versioning.md)
- [Privacy and Signing Boundaries](privacy-and-signing-boundaries.md)
- [dotnet new Templates](templates.md)
