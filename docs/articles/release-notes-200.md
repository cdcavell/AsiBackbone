# 2.0.0 Release Notes

`2.0.0` is the first major release for the simplified `AsiBackbone.*` package and namespace identity.

This release is a breaking migration from the previous package and namespace line. The underlying project purpose remains the same: Accountable Systems Infrastructure for governed .NET decision flow.

## Release summary

`2.0.0` establishes the current `2.x` package family and aligns package IDs, namespaces, repository identity, citation metadata, Zenodo metadata, documentation, and release validation around the public `AsiBackbone.*` naming convention.

The stable package set is:

```text
AsiBackbone.Core
AsiBackbone.DependencyInjection
AsiBackbone.Storage.InMemory
AsiBackbone.EntityFrameworkCore
AsiBackbone.AspNetCore
AsiBackbone.Testing
AsiBackbone.Templates
AsiBackbone.Analyzers
AsiBackbone.OpenTelemetry
AsiBackbone.Signing.LocalDevelopment
AsiBackbone.Signing.ManagedKey
```

## Breaking changes

* Renamed NuGet package IDs and public namespaces to `AsiBackbone.*`.
* Updated package, documentation, template, citation, and release metadata to the simplified project identity.
* Moved the stable binary identity from `AssemblyVersion` `1.0.0.0` to `2.0.0.0` for the new compatible `2.x` line.

## Migration

Consumers should update package references to the matching `AsiBackbone.*` package IDs and update source imports to the matching `AsiBackbone.*` namespaces.

Example package reference:

```xml
<PackageReference Include="AsiBackbone.Core" Version="2.0.0" />
```

Example namespace import:

```csharp
using AsiBackbone.Core;
```

## Changed

* Promoted central package version metadata to `2.0.0`.
* Updated `AssemblyVersion` to `2.0.0.0` and `FileVersion` to `2.0.0.0`.
* Updated `CITATION.cff` and `.zenodo.json` to `2.0.0`.
* Updated release validation and Source Link validation guidance for `2.0.0`.
* Added the `2.0.0` release notes and release readiness record to the documentation navigation.
* Updated the template identity to the simplified `AsiBackbone.Templates.WebApi` identity.

## Compatibility notes

* This is a major release because package IDs and public namespaces changed.
* Existing `1.x` consumers must update package references and `using` statements.
* The `2.x` line should preserve `AssemblyVersion` `2.0.0.0` for future compatible minor and patch releases.
* Future Event Hubs, Purview, Azure-specific SDK adapters, Aspire runtime packages, robotics, immutable-storage, and additional provider packages remain outside the stable package contract unless separately reviewed and released.

## Validation

Before tagging `v2.0.0`, confirm the release-candidate commit passes:

* CI restore, build, formatting, tests, coverage gates, package creation, template smoke validation, and CodeQL.
* Stable Release Validation.
* Publish Documentation / DocFX build.
* External Consumer Smoke Test.
* Generated NuGet package metadata validation.
* Version consistency validation for `2.0.0`, including `Directory.Build.props`, `CITATION.cff`, `.zenodo.json`, optional tag `v2.0.0`, and generated package filenames.

After packages are published and visible on NuGet, validate Source Link repository commit metadata with:

```powershell
./scripts/Validate-Source-Link-commit-metadata.ps1 -Version 2.0.0
```

## NuGet follow-up

After `AsiBackbone.*` packages are published, the previous package line should be deprecated on NuGet with alternate package guidance pointing to the corresponding `AsiBackbone.*` packages.