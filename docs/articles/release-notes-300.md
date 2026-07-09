# AsiBackbone 3.0.0 Release Notes

`3.0.0` establishes the current `3.x` stable AsiBackbone package family.

This release keeps the existing `AsiBackbone.*` package IDs and public namespaces while moving the binary assembly identity to the `3.0.0.0` major line, aligning release metadata and documentation for the 3.0.0 posture, and carrying forward the current governance-spine package family.

## Release summary

`3.0.0` is a major release because it starts a new stable binary identity line, makes the Core evaluator's constraint-exception behavior fail closed by default, and adds a non-durable reference capability-grant use store for local replay validation. The public package IDs and namespaces do **not** change from the `2.x` line.

Consumers should update package references to `3.0.0`, rebuild, and run their normal host validation. No broad package rename or `using` namespace migration is intended for this release.

## Added

* Added 3.0.0 release notes and a 3.0.0 release readiness record.
* Added the [3.0.0 Consumer Verification Guide](consumer-verification-300.md) with package-source, package ID, package version, NuGet metadata, Source Link, SBOM, provenance, deferred-signing, target-framework, and copy/paste checklist guidance.
* Added the [Target Framework Support Decision Record](target-framework-support.md), documenting that the `3.0.x` package family intentionally remains .NET 10+ by targeting `net10.0`, while .NET 8 multi-targeting is deferred unless future consumer demand justifies the full compatibility surface.
* Added the [Production Managed-Key Integration Guide](production-managed-key-integration.md), documenting the provider-neutral runtime signing path for hosts that connect Azure Key Vault, AWS KMS, GCP Cloud KMS, HSM, or enterprise key-management clients through the existing managed-key boundary.
* Added `InMemoryCapabilityGrantUseStore` to `AsiBackbone.Storage.InMemory` as a clearly non-production reference implementation of `ICapabilityGrantUseStore` for tests, samples, local validation, and stable package smoke coverage of first-use/replay-denied behavior.
* Added Debug solution build coverage validation so first-party package and test projects stay enabled for default local Debug solution builds unless a reviewed exclusion is documented.
* Promoted current documentation posture around threat-model contributors as governance hardening hooks for suspicious, malformed, replayed, or unsafe command-like inputs.
* Promoted strict-governance profile guidance for hosts that want explicit fail-closed configuration through `AddAsiBackboneStrictGovernance()` or `UseStrictGovernanceProfile()`.
* Included current EF Core JSON metadata storage guidance, production placeholder guardrails, policy-input hardening guidance, provider-neutral runtime signing guidance, and release-validation guidance in the 3.0.0 documentation map.

## Changed

* Promotes central package version metadata from `2.3.0` to `3.0.0`.
* Updates `AssemblyVersion` from `2.0.0.0` to `3.0.0.0` for the new major line.
* Updates `FileVersion` to `3.0.0.0`.
* Updates `CITATION.cff` and `.zenodo.json` for the `3.0.0` release.
* Changes `AsiBackbonePolicyEvaluatorOptions.TreatConstraintExceptionAsDenial` to default to `true` for the `3.x` line so eligible ordinary constraint exceptions become denied governance decisions with stable reason codes and auditable policy metadata.
* Documents `net10.0` as the intentional target framework and .NET 10+ consumer boundary for the `3.0.x` package line.
* Updates README, documentation home, article index, DocFX article navigation, release validation guidance, API compatibility / SemVer guidance, release cadence guidance, security posture, governance wording, template documentation, generated template fallback package versions, runtime signing-provider boundary guidance, and Source Link validation defaults for the `3.0.0` package family.
* Updates stale documentation that still described `2.3.0` or the `2.x` line as the current stable release line.
* Refreshes the historical stable API review so it no longer describes the historical `1.2.1` package family as current.

## Fixed

* Fixed release-facing documentation drift between implemented code surfaces and current package-family wording.
* Fixed template fallback package references so generated fallback `.csproj` files use `3.0.0` when repository project references are unavailable.
* Fixed Source Link post-publish validation defaults so the validation script targets `3.0.0` unless another version is supplied.
* Fixed strict-governance documentation so it no longer frames `3.0.0` as a future possible migration target.
* Fixed ambiguous 3.x constraint-exception posture by documenting fail-closed default behavior and the explicit opt-out path for hosts that intentionally need fail-fast propagation.
* Fixed the capability-grant replay hardening gap by giving consumers a packaged in-memory use store they can wire before replacing it with durable host-owned replay protection.
* Fixed the default Debug solution build posture so first-party package and test projects are no longer skipped by `Debug|*` solution exclusions.
* Fixed stable package integration smoke-test metadata so it no longer labels the current stable smoke path as `1.x`.

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

The `3.0.0` package family intentionally targets `net10.0`. Consumers should use a .NET 10 SDK/runtime or later for this release line. The project does not multi-target .NET 8 for `3.0.x`; compatibility can be reconsidered in a later release only with corresponding CI, packaging validation, analyzer compatibility, template smoke tests, and documentation updates.

`AssemblyVersion` changes to `3.0.0.0`. Consumers should rebuild and validate host deployments, especially applications that depend on assembly binding, plugin loading, or strict package-version pinning.

The release does not intentionally remove or rename public APIs, package IDs, or namespaces. The strict governance profile remains explicit host opt-in; hosts remain responsible for authentication, authorization, execution enforcement, persistence, signing/key custody, monitoring, compliance review, and operational controls.

The Core evaluator now defaults eligible ordinary constraint exceptions to denied governance decisions. Hosts that intentionally require fail-fast exception propagation can set `TreatConstraintExceptionAsDenial = false` and should ensure their host boundary still records the failed governed attempt.

`InMemoryCapabilityGrantUseStore` is a non-durable local-validation helper. It can show first-use and replay-denied behavior in one process, but production replay protection remains host/provider-owned.

## Migration notes

Update package references from `2.3.0` to `3.0.0`:

```xml
<PackageReference Include="AsiBackbone.Core" Version="3.0.0" />
<PackageReference Include="AsiBackbone.AspNetCore" Version="3.0.0" />
```

Use a .NET 10 SDK/runtime or later. This release line targets `net10.0`; it does not provide `net8.0` assets.

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

For local capability-grant replay validation, hosts can wire the packaged in-memory reference store before replacing it with durable production storage:

```csharp
services.AddAsiBackbone(builder =>
    builder.UseInMemoryCapabilityGrantUseStore());
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

AsiBackbone remains Accountable Systems Infrastructure for governed .NET decision flow: a governance spine, not an intelligence engine. It does not host, train, or run AI models; it does not control physical systems by itself; and it does not provide end-to-end legal, compliance, production tamper-evidence, replay-protection, package-signing, or production key-management guarantees by default.

Runtime governance-residue signing remains provider-neutral through `AsiBackbone.Signing.ManagedKey`. Hosts may connect Azure Key Vault, AWS KMS, GCP Cloud KMS, HSM, certificate-store, or enterprise key-management clients through the managed-key boundary, but AsiBackbone does not ship first-party production signing providers or production-style signing sample hosts.

Event Hubs, Purview, Azure-specific non-signing SDK adapters, Aspire runtime packages, robotics, immutable-storage, and additional non-signing provider packages remain outside the stable package contract unless separately reviewed and released.

## Consumer verification

Consumers can use the [3.0.0 Consumer Verification Guide](consumer-verification-300.md) to confirm package source, package IDs, package version, target framework, NuGet repository metadata, Source Link commit metadata, SBOM/provenance artifacts, and the current deferred package-signing posture.

Package SBOMs and package/SBOM provenance artifacts are release evidence for produced package artifacts where supported by the workflow event. They do not prove that packages are maintainer-signed, repository-signed, Authenticode-signed, production tamper-evident after download, legally non-repudiable, vulnerability-free, or approved for a consumer's compliance boundary.

NuGet package signing remains deferred unless a reviewed signing process is adopted and documented before release.

## Validation

Before tagging `v3.0.0`, the release candidate should pass the repository release gates, including:

* CI restore, Debug build smoke, Release build, formatting, tests, and coverage gates.
* Stable Release Validation.
* Debug solution build coverage validation.
* DocFX documentation build.
* Package creation.
* Generated NuGet metadata validation.
* Package SBOM generation.
* Package signing readiness documentation review.
* Provider-neutral runtime signing guidance review.
* Template package smoke validation.
* External consumer smoke tests.
* Version consistency validation for `3.0.0`.
* Package/SBOM provenance handling where supported by the workflow event.
* Consumer verification guide review for package source, package IDs, Source Link, SBOM/provenance, target framework, deferred signing, and conservative wording.

After packages are published and visible on NuGet, validate Source Link repository commit metadata with:

```powershell
./scripts/Validate-Source-Link-commit-metadata.ps1 -Version 3.0.0
```
