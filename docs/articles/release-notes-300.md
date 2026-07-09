# AsiBackbone 3.0.0 Release Notes

`3.0.0` establishes the current `3.x` stable AsiBackbone package family.

This release keeps the existing `AsiBackbone.*` package IDs and public namespaces while moving the binary assembly identity to the `3.0.0.0` major line, aligning release metadata and documentation for the 3.0.0 posture, and carrying forward the current governance-spine package family.

## Release summary

`3.0.0` is a major release because it starts a new stable binary identity line and makes the Core evaluator's constraint-exception behavior fail closed by default. The public package IDs and namespaces do **not** change from the `2.x` line.

Consumers should update package references to `3.0.0`, rebuild, and run their normal host validation. No broad package rename or `using` namespace migration is intended for this release.

## Added

* Added 3.0.0 release notes and a 3.0.0 release readiness record.
* Promoted current documentation posture around threat-model contributors as governance hardening hooks for suspicious, malformed, replayed, or unsafe command-like inputs.
* Promoted strict-governance profile guidance for hosts that want explicit fail-closed configuration through `AddAsiBackboneStrictGovernance()` or `UseStrictGovernanceProfile()`.
* Included current EF Core JSON metadata storage guidance, production placeholder guardrails, policy-input hardening guidance, and release-validation guidance in the 3.0.0 documentation map.

## Changed

* Promotes central package version metadata from `2.3.0` to `3.0.0`.
* Updates `AssemblyVersion` from `2.0.0.0` to `3.0.0.0` for the new major line.
* Updates `FileVersion` to `3.0.0.0`.
* Updates `CITATION.cff` and `.zenodo.json` for the `3.0.0` release.
* Changes `AsiBackbonePolicyEvaluatorOptions.TreatConstraintExceptionAsDenial` to default to `true` for the `3.x` line so eligible ordinary constraint exceptions become denied governance decisions with stable reason codes and auditable policy metadata.
* Updates README, documentation home, article index, DocFX article navigation, release validation guidance, API compatibility / SemVer guidance, release cadence guidance, security posture, governance wording, template documentation, generated template fallback package versions, and Source Link validation defaults for the `3.0.0` package family.
* Updates stale documentation that still described `2.3.0` or the `2.x` line as the current stable release line.
* Refreshes the historical stable API review so it no longer describes the historical `1.2.1` package family as current.

## Fixed

* Fixed release-facing documentation drift between implemented code surfaces and current package-family wording.
* Fixed template fallback package references so generated fallback `.csproj` files use `3.0.0` when repository project references are unavailable.
* Fixed Source Link post-publish validation defaults so the validation script targets `3.0.0` unless another version is supplied.
* Fixed strict-governance documentation so it no longer frames `3.0.0` as a future possible migration target.
* Fixed ambiguous 3.x constraint-exception posture by documenting fail-closed default behavior and the explicit opt-out path for hosts that intentionally need fail-fast propagation.

## Compatibility

Package IDs and public namespaces remain unchanged:

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

`AssemblyVersion` changes to `3.0.0.0`. Consumers should rebuild and validate host deployments, especially applications that depend on assembly binding, plugin loading, or strict package-version pinning.

The release does not intentionally remove or rename public APIs, package IDs, or namespaces. The strict governance profile remains explicit host opt-in; hosts remain responsible for authentication, authorization, execution enforcement, persistence, signing/key custody, monitoring, compliance review, and operational controls.

The Core evaluator now defaults eligible ordinary constraint exceptions to denied governance decisions. Hosts that intentionally require fail-fast exception propagation can set `TreatConstraintExceptionAsDenial = false` and should ensure their host boundary still records the failed governed attempt.

## Migration notes

Update package references from `2.3.0` to `3.0.0`:

```xml
<PackageReference Include="AsiBackbone.Core" Version="3.0.0" />
<PackageReference Include="AsiBackbone.AspNetCore" Version="3.0.0" />
```

No namespace update is expected for consumers already using the `AsiBackbone.*` package family.

Review evaluator exception handling during the upgrade. The `3.x` default is:

```csharp
new AsiBackbonePolicyEvaluatorOptions
{
    DenyWhenNoConstraints = true,
    TreatConstraintExceptionAsDenial = true,
    TreatThreatContributorExceptionAsDenial = true
};
```

Hosts that rely on propagated constraint exceptions should opt out intentionally:

```csharp
new AsiBackbonePolicyEvaluatorOptions
{
    TreatConstraintExceptionAsDenial = false
};
```

For production-oriented hosts, review whether the explicit strict profile should be enabled for endpoint-governance fail-closed settings as well:

```csharp
builder.Services.AddAsiBackboneStrictGovernance();
```

or through the builder facade:

```csharp
builder.Services.AddAsiBackbone(backbone =>
{
    backbone.UseStrictGovernanceProfile();
    backbone.UseAspNetCoreEndpointGovernance();
});
```

## Release boundary

AsiBackbone remains Accountable Systems Infrastructure for governed .NET decision flow: a governance spine, not an intelligence engine. It does not host, train, or run AI models; it does not control physical systems by itself; and it does not provide end-to-end legal, compliance, production tamper-evidence, or package-signing guarantees by default.

Event Hubs, Purview, Azure-specific SDK adapters, Aspire runtime packages, robotics, immutable-storage, and additional provider packages remain outside the stable package contract unless separately reviewed and released.

## Validation

Before tagging `v3.0.0`, the release candidate should pass the repository release gates, including:

* CI restore, build, formatting, tests, and coverage gates.
* Stable Release Validation.
* DocFX documentation build.
* Package creation.
* Generated NuGet metadata validation.
* Package SBOM generation.
* Package signing readiness documentation review.
* Template package smoke validation.
* External consumer smoke tests.
* Version consistency validation for `3.0.0`.
* Package/SBOM provenance handling where supported by the workflow event.

After packages are published and visible on NuGet, validate Source Link repository commit metadata with:

```powershell
./scripts/Validate-Source-Link-commit-metadata.ps1 -Version 3.0.0
```
