# AsiBackbone 2.3.0 Release Notes

`2.3.0` is a compatible minor release for the stable `2.x` AsiBackbone package family.

This release preserves the simplified `AsiBackbone.*` package IDs and public namespaces established in `2.0.0` while adding host-facing governance guardrails, safer signing defaults, diagnostic policy signals, outbox/query hardening, and documentation alignment for the current release line.

## Release summary

`2.3.0` is a minor release because it includes backward-compatible public/helper APIs, option surfaces, and host-facing validation guidance while keeping the `2.x` package and namespace boundary intact.

Existing `2.0.x`, `2.1.x`, `2.2.0`, and `2.2.1` consumers should continue to compile against existing APIs. Hosts using the managed-key signing provider should review the behavioral hardening note below because the production-oriented default now fails closed when signing cannot complete.

## Added

* Added `GovernanceMetadataBudget` and `GovernanceMetadataBudgetValidator` helper APIs so hosts can validate metadata count, key length, value length, serialized size, and reserved-key shape before audit, telemetry, or signing boundaries.
* Added opt-in `AsiBackbonePolicyEvaluatorOptions.TreatConstraintExceptionAsDenial` behavior so hosts can convert constraint exceptions into denied governance decisions with configured safe reason codes.
* Added managed-key local-validation helpers through `CreateLocalValidation(...)` and `AddAsiBackboneManagedKeySigningForLocalValidation(...)` for samples, tests, diagnostics, and explicit host policies that intentionally need unsigned failure metadata.
* Added warning-level logging when the policy evaluator runs with zero constraints while permissive empty-policy behavior remains enabled.
* Added design guidance for future opt-in governance outbox claim/lease behavior without mutating the existing find/read APIs.
* Added tests covering metadata budgets, constraint-exception behavior, managed-key fail-closed behavior, empty-policy warning signals, EF Core outbox ordering, template package fallback versions, and collection-backed reason normalization.

## Changed

* Promotes central package version metadata from `2.2.1` to `2.3.0` while preserving `AssemblyVersion` as `2.0.0.0` for the compatible stable `2.x` line.
* Updates `FileVersion` to `2.3.0.0`.
* Updates `CITATION.cff` and `.zenodo.json` for the `2.3.0` release.
* Updates README, documentation home, article index, DocFX article navigation, release validation guidance, API compatibility / SemVer guidance, release notes, release readiness, templates guidance, generated template fallback package versions, and Source Link validation defaults for the `2.3.0` package family.
* Changes production-oriented managed-key signing to fail closed by default by making `ManagedKeySigningOptions.ReturnUnsignedOnFailure` default to `false`.
* Removes repeated endpoint-governance option validation from the request path while preserving fail-closed startup/configured-options validation.
* Pushes EF Core governance outbox pending and retry-ready filtering, ordering, and `Take(maxCount)` into database queries before materialization.
* Adds deterministic `OutboxEntryId` tie-breakers and corresponding index coverage guidance for provider-neutral outbox drain paths.
* Updates template fallback package references from `2.2.1` to `2.3.0`.

## Fixed

* Fixes template fallback package version drift so generated fallback `.csproj` files align with the repository package version.
* Preserves empty and all-null governance decision reason fallback behavior while optimizing collection-backed reason normalization to use a single enumeration pass for non-empty `ICollection<OperationReason>` inputs.
* Preserves cancellation propagation for `OperationCanceledException` while documenting and testing the new opt-in constraint-exception denial path.
* Preserves default permissive empty-policy behavior for compatibility while documenting it as a named sharp edge and recommending fail-closed configuration/startup validation.
* Preserves endpoint-governance configuration validation through registration/configured-options validation and `ValidateOnStart` rather than per-request validation.

## Behavioral hardening note

The managed-key signing provider now fails closed by default in production-oriented registration paths. `ManagedKeySigningOptions.ReturnUnsignedOnFailure` defaults to `false`, and validation or provider failures throw instead of returning unsigned failure metadata.

Hosts that intentionally relied on unsigned failure metadata for samples, diagnostics, local validation, or policy-routed fallback should choose one of the explicit opt-in paths:

```csharp
ManagedKeySigningOptions.CreateLocalValidation(...)
```

or:

```csharp
services.AddAsiBackboneManagedKeySigningForLocalValidation(...)
```

Unsigned failure metadata remains diagnostic or policy-routable evidence. It is not a successful signature and must not be treated as a signed governance artifact.

## Security, governance, and metadata guardrails

* Metadata budget validation helps hosts bound audit/telemetry/signing metadata before values cross durable or external-provider boundaries.
* Reserved-key guidance documents which metadata keys should remain package-owned, canonical-signing-owned, or host-owned.
* Constraint-exception denial behavior is opt-in so compatibility is preserved for hosts that prefer fail-fast exception propagation.
* Empty-policy warning diagnostics make permissive no-constraint evaluation visible without changing the existing default allow behavior.
* Managed-key signing defaults now better match production fail-closed expectations.

## Performance and reliability refinements

* EF Core outbox drain queries now filter, order, and limit pending/retry-ready entries in the database query before materialization.
* Endpoint-governance request-path overhead is reduced by relying on startup/configured-options validation instead of repeated per-request option validation.
* Governance decision reason normalization avoids a second enumeration for collection-backed non-empty reason inputs.
* Dependency/tooling updates include BenchmarkDotNet, dotnet-stryker, Roslyn analyzer package updates, and CodeQL action updates.

## Compatibility

No package ID or namespace changes are included.

No public API removals or renamed public APIs are intended.

`AssemblyVersion` remains `2.0.0.0` for the compatible stable `2.x` line.

This release adds public/helper APIs and safer defaults while preserving the existing stable package family. Consumers should review the managed-key signing behavioral hardening note if they use default managed-key failure behavior.

## Stable package family

The stable package set remains:

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

## Release boundary

`2.3.0` does not change the project boundary. AsiBackbone remains Accountable Systems Infrastructure for governed .NET decision flow: a governance spine, not an intelligence engine. See [Project Boundaries and Non-Claims](project-boundaries.md) for the full scope statement.

Event Hubs, Purview, Azure-specific SDK adapters, Aspire runtime packages, robotics, immutable-storage, and additional provider packages remain outside the stable package contract unless separately reviewed and released.

## Validation

Before tagging `v2.3.0`, the release candidate should pass the repository release gates, including:

* CI restore, build, formatting, tests, and coverage gates.
* Stable Release Validation.
* DocFX documentation build.
* Package creation.
* Generated NuGet metadata validation.
* Package SBOM generation.
* Template package smoke validation.
* External consumer smoke tests.
* Version consistency validation for `2.3.0`.
* Package/SBOM provenance handling where supported by the workflow event.

After packages are published and visible on NuGet, validate Source Link repository commit metadata with:

```powershell
./scripts/Validate-Source-Link-commit-metadata.ps1 -Version 2.3.0
```
