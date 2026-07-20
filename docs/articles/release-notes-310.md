# AsiBackbone 3.1.0 Release Notes

Release date: 2026-07-20

## Summary

`3.1.0` is a backward-compatible minor release for the stable `3.x` AsiBackbone package family. It expands the stable public surface around host execution accountability, capability-proof trust policy, and ASP.NET Core actor classification while preserving package IDs, public namespaces, the `net10.0` target, and the stable `AssemblyVersion` of `3.0.0.0`.

The release keeps AsiBackbone within its established role as Accountable Systems Infrastructure for governed .NET decision flow: a governance spine, not an intelligence engine, host execution system, robot controller, security product, or compliance certification.

## Added

### Governed execution-to-mutation accountability

Core now provides stable provider-neutral contracts for binding a governed decision lifecycle to the outcome of a host-owned execution attempt:

- `GovernedOperationExecutionReceipt`;
- `GovernedOperationPersistenceOutcome`;
- canonical receipt-payload construction;
- host-accountability metadata keys; and
- lifecycle helpers that preserve decision, execution, retry-attempt, and optional mutation-batch correlation.

The receipt carries only minimized identifiers, counts, hashes, timestamps, provider information, and bounded metadata. Original and current application values remain authoritative in the host audit store.

### Capability-proof trust pinning

`CapabilityGrantValidationOptions` now supports optional proof trust pins for:

- signing key ID and key version;
- proof policy version and policy hash;
- verification provider; and
- proof hash algorithm.

These settings allow a host to narrow which otherwise valid signing authority is acceptable for a particular capability boundary. Omitting the pins preserves the earlier verification behavior.

### Actor-type claim trust controls

ASP.NET Core actor-context mapping now provides:

- a conservative allow-list for actor types accepted from claims;
- `Human` as the default claim-mapped actor type;
- explicit host opt-in before `System`, `Service`, `Agent`, or `Unknown` claim values are honored; and
- safe fallback to `DefaultAuthenticatedActorType` for unrecognized, undefined, or disallowed values.

The configured actor-type claim must be issued or protected by a trusted host identity boundary and must not be sourced from user-controlled request or profile data.

### Optional NCAT audit-completion adapter

A source-neutral NCAT audit-completion reference adapter and tests are included as an isolated, non-packable sample outside the required AsiBackbone package graph. It demonstrates how host mutation completion evidence can be mapped into the new execution receipt and lifecycle event contracts without making NCAT a package dependency.

## Fixed and hardened

- Clarified audit-integrity sequence ownership and preserved duplicate-before-continuity precedence so repeated sequence values are classified consistently as forked chains.
- Removed temporary accepted-sequence add/remove behavior from link verification.
- Canceled and disposed the losing hosted-outbox polling delay when an options-change signal completes first.
- Removed a redundant capability-validation action pass-through helper while preserving validation behavior.
- Expanded property-based validation coverage for governance metadata normalization.
- Added or hardened OpenSSF Scorecard, actionlint/Zizmor, OWASP Dependency-Check, dependency review, locked restore, workflow-permission, and dependency-pinning controls.
- Added reviewed, narrowly scoped OWASP suppressions for cross-ecosystem false-positive CVE associations with expiration dates.

## Compatibility notes

- Package IDs and public namespaces are unchanged.
- The target framework remains `net10.0`.
- `AssemblyVersion` remains `3.0.0.0`; `FileVersion` advances to `3.1.0.0`.
- The new Core and ASP.NET Core APIs are additive.
- Hosts that currently map privileged actor categories from a trusted `actor_type` claim must explicitly add those values to `AllowedActorTypesFromClaims`. Without that opt-in, mapping falls back to `DefaultAuthenticatedActorType`, which defaults to `Human`.
- Capability-proof trust pins are optional; existing callers that do not configure them retain the earlier proof-verification behavior.
- The NCAT adapter is sample-only and non-packable; it does not add NCAT to the stable package dependency graph.
- No production key-management, host execution, robotics, legal, or compliance capability is introduced by this release.

## Package signing posture

NuGet package signing remains intentionally deferred while AsiBackbone is independently maintained. Consumers should continue to rely on the official NuGet source, public repository, release tags, Source Link repository metadata, SBOMs, provenance artifacts where available, and reproducible release practices.

## Validation

The release candidate should pass:

- locked restore and Debug/Release solution builds;
- formatting, analyzer, unit, integration, and property-based tests;
- repository-wide and package-specific coverage gates;
- Core branch coverage and XML-documentation inventory validation;
- API baseline and compatibility checks;
- package creation and generated NuGet metadata validation;
- template, external-consumer, and stable-package smoke tests;
- version-consistency validation for `3.1.0` and `v3.1.0`;
- DocFX generation and documentation release-claim validation;
- CodeQL, dependency review, OpenSSF, actionlint/Zizmor, and OWASP checks; and
- SBOM and provenance handling where supported.

After publication, validate Source Link repository metadata with:

```powershell
./scripts/Validate-Source-Link-commit-metadata.ps1 -Version 3.1.0
```
