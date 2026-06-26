# API Compatibility and Semantic Versioning

This article defines the public API compatibility promise for the stable AsiBackbone package family and documents how semantic versioning applies after stabilization.

It complements the historical stable API review tracked in [issue #13](https://github.com/cdcavell/AsiBackbone/issues/13). That review established the original `1.0.0` public type names, namespaces, package boundaries, dependency direction, and extension points. The released `1.1.0` package family expanded the stable contract with additive analyzer, OpenTelemetry, and signing-provider package surfaces. `1.2.0` formalized additive adoption, diagnostics, testing, templates, samples, and documentation-alignment surfaces on the stable `1.x` contract. `1.2.1` preserved the `1.2.0` package/API boundary while hardening release metadata, Source Link repository-commit metadata, validation guidance, workflow hygiene, and documentation wording. `2.0.0` started the current `2.x` line because the public package IDs and namespaces moved from `CDCavell.AsiBackbone.*` to `AsiBackbone.*`. `2.0.1` preserved the `2.0.0` public package and namespace boundary while hardening release metadata, documentation currency, package SBOM/provenance artifacts, and repository/package branding. `2.0.2` preserves that same public package and namespace boundary while correcting package icon presentation metadata/assets.

> [!NOTE]
> `1.1.1` included small additive, opt-in endpoint-governance public surface and an additive template package while preserving source and binary compatibility for existing `1.1.0` consumers. That release is documented as a compatibility exception to the expected SemVer policy below. Future additive public API or package surface should use a minor version bump even when the change is backward-compatible.

## Compatibility promise for the stable `2.x` line

Starting with `2.0.0`, packages identified as stable are expected to preserve their documented public API surface for consumers within the same major version.

The compatibility promise applies to:

- public types and members exposed by stable packages;
- public namespaces intended for consumer use;
- documented extension points and service registration methods;
- documented option shapes and default behavior;
- durable artifact shapes that are explicitly described as stable, including their schema version where applicable;
- package boundaries and dependency direction described as stable for the released package family.

The promise does not mean implementation internals will never change. Internal code, private members, tests, documentation wording, samples, and non-public implementation details may change in minor or patch releases when the public contract remains compatible.

## Stable package scope by release

Stable compatibility is package-specific. A package becomes part of the stable contract when it is released as a stable package and documented as part of the stable package family.

### Original `1.0.0` stable family

The initial stable `1.0.0` package family established the first compatible `1.x` baseline:

| Package | Stable role |
| --- | --- |
| `AsiBackbone.Core` | Framework-neutral governance primitives, decisions, constraints, actor context, audit residue, acknowledgment, capability-token abstractions, and operation results. |
| `AsiBackbone.AspNetCore` | ASP.NET Core host adapters for actor context, request correlation, HTTP result mapping, and acknowledgment challenge support. |
| `AsiBackbone.Storage.InMemory` | Non-durable in-memory storage helpers for tests, samples, and local validation. |
| `AsiBackbone.EntityFrameworkCore` | EF Core model configuration and host-owned persistence integration. |

### Expanded `1.1.x` stable family

The released `1.1.0` package family kept the `1.0.0` packages compatible and added stable additive package surfaces. `1.1.1` is a patch release on the same API surface, with the endpoint-governance and template-package additive compatibility exception noted above.

| Package | `1.1.x` stable role |
| --- | --- |
| `AsiBackbone.Core` | Adds provider-neutral governance emission contracts, durable outbox contracts, DLP/classification policy primitives, signing-ready metadata abstractions, canonical hashing/signing seams, verification-policy primitives, and lifecycle/audit additions while preserving the compatible `1.x` Core line. |
| `AsiBackbone.DependencyInjection` | Adds the explicit `AddAsiBackbone(...)` builder facade for coordinating host-selected provider registrations without making Core own infrastructure. |
| `AsiBackbone.Storage.InMemory` | Adds non-durable lifecycle and outbox proof paths for tests, samples, and local validation. |
| `AsiBackbone.EntityFrameworkCore` | Adds host-owned durable persistence surfaces for audit residue lifecycle and governance outbox records. |
| `AsiBackbone.AspNetCore` | Adds endpoint governance and hosted outbox drain integration while keeping host ownership explicit. |
| `AsiBackbone.Testing` | Adds test-only endpoint-governance harness helpers for deterministic policy decisions and in-memory inspection. |
| `AsiBackbone.Templates` | Adds `dotnet new` templates for generating governed ASP.NET Core host scaffolds. The package is a developer-experience scaffold, not a runtime dependency. |
| `AsiBackbone.Analyzers` | Stable build-time analyzer safety rails for governance persistence and continuation-flow patterns. |
| `AsiBackbone.OpenTelemetry` | Stable concrete governance emission provider that projects provider-neutral envelopes into .NET diagnostics primitives such as `ActivitySource` and `Meter`. |
| `AsiBackbone.Signing.LocalDevelopment` | Stable local-development signing and verification provider for tests, samples, and wiring proof paths only. Not production key custody. |
| `AsiBackbone.Signing.ManagedKey` | Stable managed-key signing adapter boundary. The host supplies the actual managed-key client and operational controls. |

### Final `1.2.x` stable family before the rename

`1.2.1` was the final stable patch release for the compatible `1.x` line. It preserved the `1.0.0`, `1.1.x`, and `1.2.0` contracts while hardening release metadata, Source Link repository-commit metadata, validation guidance, workflow hygiene, and documentation wording.

| Package | `1.2.x` stable role |
| --- | --- |
| `AsiBackbone.Core` | Continues the framework-neutral governance primitive surface and durable artifact contracts from the compatible `1.x` line. |
| `AsiBackbone.DependencyInjection` | Provides the explicit builder facade and host-selected provider registration composition path. |
| `AsiBackbone.Storage.InMemory` | Provides non-durable storage helpers for tests, samples, local validation, lifecycle events, and outbox proof paths. |
| `AsiBackbone.EntityFrameworkCore` | Provides EF Core host-owned persistence helpers for audit, acknowledgment, lifecycle, and outbox records. |
| `AsiBackbone.AspNetCore` | Provides ASP.NET Core host adapters, endpoint governance, development diagnostics, and hosted outbox drain integration. |
| `AsiBackbone.Testing` | Provides test-only harness helpers for deterministic governance and package-wiring tests. |
| `AsiBackbone.Templates` | Provides developer-experience `dotnet new` templates for governed ASP.NET Core host scaffolding. |
| `AsiBackbone.Analyzers` | Provides build-time analyzer safety rails, including production-signing configuration guidance. |
| `AsiBackbone.OpenTelemetry` | Provides the released OpenTelemetry governance emission provider. |
| `AsiBackbone.Signing.LocalDevelopment` | Provides local-development signing and verification for tests, samples, and wiring proof paths only. |
| `AsiBackbone.Signing.ManagedKey` | Provides the managed-key signing adapter boundary where the host supplies the concrete managed-key client and operational controls. |

### Current `2.0.x` stable family

`2.0.2` is the current stable patch release. It preserves the `2.0.0` public package and namespace boundary after the public rename from `CDCavell.AsiBackbone.*` to `AsiBackbone.*` while correcting package icon presentation metadata/assets. The underlying governance-spine package roles carry forward from `1.2.1`, and consumers already on `2.0.0` or `2.0.1` should not need source-code changes to move to `2.0.2`.

| Package | `2.0.x` stable role |
| --- | --- |
| `AsiBackbone.Core` | Framework-neutral governance primitives and durable artifact contracts for the current `2.x` line. |
| `AsiBackbone.DependencyInjection` | Explicit builder facade and host-selected provider registration composition path. |
| `AsiBackbone.Storage.InMemory` | Non-durable storage helpers for tests, samples, local validation, lifecycle events, and outbox proof paths. |
| `AsiBackbone.EntityFrameworkCore` | EF Core host-owned persistence helpers for audit, acknowledgment, lifecycle, and outbox records. |
| `AsiBackbone.AspNetCore` | ASP.NET Core host adapters, endpoint governance, development diagnostics, and hosted outbox drain integration. |
| `AsiBackbone.Testing` | Test-only harness helpers for deterministic governance and package-wiring tests. |
| `AsiBackbone.Templates` | Developer-experience `dotnet new` templates for governed ASP.NET Core host scaffolding. |
| `AsiBackbone.Analyzers` | Build-time analyzer safety rails, including production-signing configuration guidance. |
| `AsiBackbone.OpenTelemetry` | Released OpenTelemetry governance emission provider. |
| `AsiBackbone.Signing.LocalDevelopment` | Local-development signing and verification for tests, samples, and wiring proof paths only. |
| `AsiBackbone.Signing.ManagedKey` | Managed-key signing adapter boundary where the host supplies the actual managed-key client and operational controls. |

Stable package status does not imply that every future provider idea is stable. Event Hubs, Purview, Azure-specific SDK adapters, Aspire runtime packages, robotics, immutable storage, and additional provider packages remain outside the stable contract unless separately reviewed and released as stable packages.

## What counts as a breaking change

A breaking change should require a new major version when it affects a stable package contract. Examples include:

- removing or renaming a public type, member, enum value, interface, namespace, or package;
- changing public method signatures, constructor signatures, generic constraints, or return types;
- changing documented default behavior in a way that can alter consumer outcomes;
- making previously optional configuration required;
- changing stable serialized or persisted artifact shapes without a compatible reader, migration path, or schema-versioned transition;
- changing service registration behavior in a way that breaks existing host startup code;
- changing package dependency direction or adding a framework/provider dependency that violates a documented package boundary;
- changing documented exception behavior where callers are expected to handle that behavior;
- converting a host-owned integration responsibility into a package-owned requirement without a compatible opt-in path.

A change is not usually breaking when it only adds new optional APIs, adds new optional configuration, improves implementation behavior without changing the public contract, fixes a bug to match documented behavior, or updates documentation and samples.

## Semantic versioning expectations

AsiBackbone follows Semantic Versioning expectations after stabilization:

| Version segment | Expected behavior |
| --- | --- |
| Major | May include intentional breaking changes to stable public APIs, stable package boundaries, or stable artifact contracts. Breaking changes should be documented with migration notes. |
| Minor | Adds backward-compatible public APIs, options, adapters, packages, or features. Existing stable APIs should continue to compile and behave compatibly. |
| Patch | Fixes bugs, documentation issues, packaging issues, or implementation defects without adding breaking public API changes. Patch releases can also clarify documentation and strengthen tests. |
| Preview suffix | Indicates packages or features that are still under review and may change before stable release. |

For future releases, additive public API or package changes should be grouped into a minor release even when they are opt-in and backward-compatible. Patch releases should be reserved for fixes, documentation, packaging, tests, and implementation hardening that does not expand the stable public surface.

Before `1.0.0`, alpha or preview packages may still make breaking changes as the API is shaped. Those changes should remain visible in the changelog and release notes.

## Assembly version policy

For the stable `2.x` package line, AsiBackbone keeps `AssemblyVersion` fixed at `2.0.0.0` for compatible minor and patch releases. NuGet package `Version`, `FileVersion`, and `InformationalVersion` continue to move with each package release.

This separates the package release identity from the binary assembly identity. Package versions communicate the SemVer release to NuGet consumers. `FileVersion` and `InformationalVersion` communicate the concrete build/product version. `AssemblyVersion` is reserved for the compatible major line and should change only when the project intentionally creates a new binary identity.

Expected stable-line behavior:

| Release | Package `Version` | `AssemblyVersion` | `FileVersion` | `InformationalVersion` |
| --- | --- | --- | --- | --- |
| `1.0.0` | `1.0.0` | `1.0.0.0` | `1.0.0.0` | `1.0.0+...` |
| `1.0.1` | `1.0.1` | `1.0.0.0` | `1.0.1.0` | `1.0.1+...` |
| `1.1.0` | `1.1.0` | `1.0.0.0` | `1.1.0.0` | `1.1.0+...` |
| `1.1.1` | `1.1.1` | `1.0.0.0` | `1.1.1.0` | `1.1.1+...` |
| `1.2.0` | `1.2.0` | `1.0.0.0` | `1.2.0.0` | `1.2.0+...` |
| `1.2.1` | `1.2.1` | `1.0.0.0` | `1.2.1.0` | `1.2.1+...` |
| `2.0.0` | `2.0.0` | `2.0.0.0` | `2.0.0.0` | `2.0.0+...` |
| `2.0.1` | `2.0.1` | `2.0.0.0` | `2.0.1.0` | `2.0.1+...` |
| `2.0.2` | `2.0.2` | `2.0.0.0` | `2.0.2.0` | `2.0.2+...` |

Before cutting stable releases, release validation should verify that `AssemblyVersion`, `FileVersion`, `InformationalVersion`, package metadata, release notes, and repository tags match this policy.

## Durable artifact and schema-version policy

Stable persisted or exported governance artifacts should carry an explicit schema/version field when they may be stored or consumed outside the running process. See [Schema Versioning](schema-versioning.md) for artifact-specific guidance.

Schema versioning does not replace package versioning. Package versions describe the released library. Schema versions describe durable payload shapes that may outlive a single package release.

Additive artifact fields are normally acceptable in a compatible minor release when existing readers can continue operating or when schema-versioned handling is documented. Incompatible durable shape changes should use schema-version guidance, migration documentation, and a major-version boundary when needed.

## Provider and future package guidance

Released provider packages have their own stable contract within the compatible `2.x` line once they are published as stable packages. In the current `2.0.x` line, that includes:

- `AsiBackbone.OpenTelemetry`;
- `AsiBackbone.Signing.LocalDevelopment`;
- `AsiBackbone.Signing.ManagedKey`.

The analyzer package is also part of the released stable package family, but analyzer diagnostics are build-time guidance rather than runtime enforcement. The testing package is a test-harness package rather than runtime enforcement. The templates package is a developer-experience package rather than a runtime provider.

Provider packages planned for later milestones should not be described as part of the stable contract until they complete their own API review and stable release checklist.

Examples of packages or integrations outside the current stable contract include:

- Event Hubs governance emission provider packages;
- Purview governance and lineage enrichment provider packages;
- Azure Monitor-specific SDK adapters, beyond host-configured OpenTelemetry exporter guidance;
- Azure Key Vault, Managed HSM, cloud KMS, HSM, or certificate-store implementations beyond the managed-key adapter boundary;
- robotics or physical execution packages;
- immutable storage, ledger, or external anchoring packages;
- any package that would make AsiBackbone a compliance product, model host, execution engine, or production signing appliance by default.

Documentation should state whether each package is stable, preview, experimental, design-only, strategy-only, sample-only, or host-owned integration guidance.

## Release readiness checklist reference

Before cutting a stable release or stable package-family expansion, the release readiness checklist should confirm that:

- the stable package list is identified;
- public APIs for stable packages are reviewed for naming, namespace, dependency direction, and extension-point clarity;
- breaking changes found during review are resolved before release or captured for a future major version;
- stable serialized artifacts have documented schema-version behavior;
- the `AssemblyVersion` strategy is resolved and reflected in build metadata;
- stable provider packages are clearly distinguished from future/design-only provider pages;
- preview, strategy-only, design-only, or sample-only packages are not implied to be part of the stable contract;
- the changelog and release notes include the compatibility promise and migration guidance where needed.

## Related documentation

- [2.0.2 Release Notes](release-notes-202.md)
- [2.0.1 Release Notes](release-notes-201.md)
- [2.0.0 Release Notes](release-notes-200.md)
- [1.2.1 Release Notes](release-notes-121.md)
- [1.2.0 Release Notes](release-notes-120.md)
- [1.1.x Release Notes](release-notes-110.md)
- [Upgrade Guide: 1.0.0 to 1.1.0](upgrade-100-to-110.md)
- [Schema Versioning](schema-versioning.md)
- [API Baseline and Boundary Checks](api-baseline-and-boundary-checks.md)
- [Project Boundaries and Non-Claims](project-boundaries.md)
- [dotnet new Templates](templates.md)
- [Release Validation](release-validation.md)
