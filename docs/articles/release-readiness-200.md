# 2.0.0 Release Readiness Record

This record tracks release readiness for AsiBackbone `2.0.0`.

## Release intent

`2.0.0` is a major release that establishes the simplified `AsiBackbone.*` package and namespace identity.

The release remains bounded to practical Accountable Systems Infrastructure software: policy evaluation, governance decisions, acknowledgment workflows, audit residue, durable outbox contracts, provider emission boundaries, package templates, analyzers, and signing-provider seams.

## Release classification

| Field | Value |
| --- | --- |
| Release | `2.0.0` |
| Tag | `v2.0.0` |
| Release type | Major |
| Primary reason | Package ID and public namespace rename |
| Stable line | `2.x` |
| AssemblyVersion | `2.0.0.0` |
| FileVersion | `2.0.0.0` |
| NuGet Version | `2.0.0` |

## Stable package set

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

## Breaking-change review

`2.0.0` intentionally includes a breaking public identity change.

Required migration:

* update package references to the matching `AsiBackbone.*` package IDs;
* update `using` statements to the matching `AsiBackbone.*` namespaces;
* regenerate or review template output where projects rely on template identity metadata;
* retest package consumers after migration.

## Version metadata checklist

- [ ] `Directory.Build.props` uses `VersionPrefix` `2.0.0`.
- [ ] `Directory.Build.props` uses `AssemblyVersion` `2.0.0.0`.
- [ ] `Directory.Build.props` uses `FileVersion` `2.0.0.0`.
- [ ] `CITATION.cff` uses version `2.0.0`.
- [ ] `.zenodo.json` uses version `2.0.0`.
- [ ] Generated package filenames use `2.0.0`.
- [ ] Tag validation passes for `v2.0.0`.

## Documentation checklist

- [ ] `CHANGELOG.md` contains a `2.0.0` entry.
- [ ] `README.md` identifies stable `2.0.x` as the current release line.
- [ ] Documentation home identifies `2.0.0` as the current stable release.
- [ ] Article index links to `2.0.0` release notes and readiness record.
- [ ] API compatibility / SemVer guidance documents the `2.x` binary identity and migration boundary.
- [ ] Release validation guidance references the `2.0.0` Source Link validation command.
- [ ] NuGet follow-up guidance calls out deprecation of the previous package line.

## CI and validation checklist

- [ ] CI passes on the release-candidate commit.
- [ ] Dependency review passes where applicable.
- [ ] Restore succeeds.
- [ ] Release build succeeds.
- [ ] Formatting check succeeds.
- [ ] Full test suite succeeds.
- [ ] Coverage gates succeed.
- [ ] Package creation succeeds.
- [ ] Generated package version validation succeeds.
- [ ] Generated NuGet metadata validation succeeds.
- [ ] Template package smoke validation succeeds.
- [ ] DocFX build succeeds.
- [ ] External consumer smoke tests succeed.
- [ ] Stable package integration smoke tests succeed.
- [ ] CodeQL analysis passes where applicable.

## Post-publish checklist

- [ ] Verify all `AsiBackbone.*` packages are visible on NuGet.
- [ ] Run Source Link repository metadata validation:

```powershell
./scripts/Validate-Source-Link-commit-metadata.ps1 -Version 2.0.0
```

- [ ] Deprecate previous package IDs on NuGet and point to the matching `AsiBackbone.*` replacements.
- [ ] Request or follow up on NuGet prefix reservation for `AsiBackbone.*` if not already approved.
- [ ] Confirm the GitHub release notes clearly label the release as breaking.
- [ ] Confirm Zenodo receives or records the `2.0.0` software version metadata.

## Boundary notes

This release does not change the project into an intelligence engine, compliance product, robot controller, signing appliance, or operational enforcement system. Hosts still own authentication, authorization, execution, persistence, key management, privacy review, operational monitoring, and legal/compliance interpretation.