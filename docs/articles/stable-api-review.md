# Historical Stable API Review

This review records the public API review for the initial stable AsiBackbone package family before the `1.0.0` surface was established. It is retained as a historical API-shape baseline for the compatible `1.x` line.

The current released stable package family is documented in [1.1.0 Release Notes](release-notes-110.md). The expanded `1.1.0` package family added stable analyzer, OpenTelemetry, and signing-provider packages while preserving the original `1.0.0` package compatibility promise. Future stable packages or public API expansions should continue to use release validation, package-boundary review, and follow-up API review notes when needed.

In this software project, **ASI** means **Accountable Systems Infrastructure**. This review is limited to implemented .NET package APIs and does not treat AsiBackbone as an artificial superintelligence implementation, AI model host, legal/compliance guarantee, or robot controller.

## Review status

| Item | Status |
| --- | --- |
| Public API review documented | Complete for the initial `1.0.0` stable surface. |
| Required breaking changes identified | None identified in the initial `1.0.0` review. |
| Package boundaries reviewed | Complete for the initial package family; `1.1.0` stable package-family addendum recorded below. |
| Dependency direction reviewed | Complete for the initial package family and still valid for the compatible `1.x` line. |
| Stable release dependency | Historical: the `1.0.0` tag was not to be cut until release validation passed and later review findings were resolved or intentionally deferred. For current releases, use [Stable Release Validation](release-validation.md). |

## Reviewed package family for `1.0.0`

The initial stable package family reviewed here is:

| Package | Reviewed role | Boundary decision |
| --- | --- | --- |
| `CDCavell.AsiBackbone.Core` | Framework-neutral governance primitives, actor context, constraints, decision results, audit contracts, acknowledgment primitives, capability-token abstractions, and operation results. | Stable candidate. Should remain independent of ASP.NET Core, EF Core, cloud, robotics, AI model, and host-template dependencies. |
| `CDCavell.AsiBackbone.Storage.InMemory` | Non-durable in-memory storage helpers for tests, samples, and local validation. | Stable candidate with a clear non-durable boundary. Should not be described as production storage. |
| `CDCavell.AsiBackbone.EntityFrameworkCore` | EF Core model configuration and host-owned persistence integration. | Stable candidate as host-owned EF Core integration. Should not own migrations, database lifecycle, provider selection, or retention policy. |
| `CDCavell.AsiBackbone.AspNetCore` | Thin ASP.NET Core host adapters for actor context, request correlation, result mapping, and acknowledgment challenge support. | Stable candidate as host integration. Should not become the owner of host authentication, authorization, routing, policy evaluation, persistence, UI, or execution behavior. |

## `1.1.0` stable API review addendum

The `1.1.0` stable package family is an additive compatible expansion over the `1.0.0` baseline.

| Package | `1.1.0` stable role | Boundary decision |
| --- | --- | --- |
| `CDCavell.AsiBackbone.Core` | Adds provider-neutral governance emission contracts, durable outbox contracts, DLP/classification policy primitives, signing-ready metadata abstractions, canonical hashing/signing seams, verification-policy primitives, lifecycle events, and expanded audit/outbox vocabulary. | Stable additive Core expansion. Core must remain framework-neutral and provider-neutral. |
| `CDCavell.AsiBackbone.Storage.InMemory` | Adds non-durable lifecycle and outbox proof paths for tests, samples, local validation, and no-op proof flows. | Stable non-durable helper boundary. Not production storage. |
| `CDCavell.AsiBackbone.EntityFrameworkCore` | Adds host-owned persistence integration for audit residue lifecycle and governance outbox records. | Stable EF Core integration boundary. Host still owns `DbContext`, provider, migrations, connection string, deployment, and retention. |
| `CDCavell.AsiBackbone.AspNetCore` | Adds endpoint governance and hosted outbox drain integration. | Stable ASP.NET Core host adapter boundary. Does not replace authentication, authorization, routing, persistence, UI, or execution controls. |
| `CDCavell.AsiBackbone.Analyzers` | Provides Roslyn analyzer safety rails for governance persistence and continuation flows. | Stable build-time guidance. Analyzer diagnostics are not runtime enforcement. |
| `CDCavell.AsiBackbone.OpenTelemetry` | Provides the concrete OpenTelemetry governance emission provider package. | Stable provider package. Projects provider-neutral governance envelopes into .NET diagnostics while exporter configuration remains host-owned. |
| `CDCavell.AsiBackbone.Signing.LocalDevelopment` | Provides local-development signing and verification for tests, samples, and wiring proof paths. | Stable local-only provider. Not production key custody, managed-key signing, immutability, non-repudiation, or tamper-evidence. |
| `CDCavell.AsiBackbone.Signing.ManagedKey` | Provides a managed-key signing adapter boundary. | Stable adapter boundary. The host supplies the managed-key client, credentials, key operations, monitoring, verification plan, and operational policy. |

The `1.1.0` addendum does not bring future Event Hubs, Purview, Azure-specific SDK adapters, robotics, immutable storage, external anchoring, Azure Key Vault-specific implementation packages, HSM-specific implementation packages, or other future providers into the stable contract. Those must complete their own review before being documented as stable packages.

## Public naming review

The reviewed public naming pattern was acceptable for `1.0.0` and remains acceptable for the compatible `1.x` line:

- package names consistently use `CDCavell.AsiBackbone.*`;
- namespaces mirror package boundaries;
- public host integration types use the `AsiBackbone` prefix where they are package-specific;
- Core domain types such as `GovernanceDecision`, `OperationResult`, `AuditResidue`, and `AuditLedgerRecord` are acceptable without repeating the prefix because they are already under the `CDCavell.AsiBackbone.Core` namespace;
- ASP.NET Core, EF Core, OpenTelemetry, analyzer, and signing-provider types carry package-specific names where ambiguity is likely;
- extension method names are readable and host-oriented, such as `AddAsiBackboneAspNetCore`, `AddAsiBackboneOpenTelemetryGovernanceEmission`, and `ApplyAsiBackboneConfigurations`.

No required renames were identified for the stable `1.x` documentation review.

## Namespace review

The namespace layout was acceptable for `1.0.0` and is corrected here for the current public API:

| Namespace area | Review result |
| --- | --- |
| `CDCavell.AsiBackbone.Core.Actors` | Clear home for framework-neutral actor context abstractions and defaults. |
| `CDCavell.AsiBackbone.Core.Constraints` | Clear home for policy constraint abstractions and evaluation results. |
| `CDCavell.AsiBackbone.Core.Decisions` | Clear home for composed governance decisions and outcomes. |
| `CDCavell.AsiBackbone.Core.Evaluation` | Clear home for evaluator and decision policy contracts. |
| `CDCavell.AsiBackbone.Core.Audit` | Clear home for audit residue, ledger records, lifecycle events, and ledger store contracts. |
| `CDCavell.AsiBackbone.Core.Handshakes` | Clear home for acknowledgment and responsibility-handshake primitives. |
| `CDCavell.AsiBackbone.Core.CapabilityTokens` | Clear home for capability token abstractions. |
| `CDCavell.AsiBackbone.Core.Emissions` | Clear home for provider-neutral governance emission contracts and envelopes. |
| `CDCavell.AsiBackbone.Core.Outbox` | Clear home for provider-neutral durable outbox contracts and records. |
| `CDCavell.AsiBackbone.Core.Classification` | Clear home for DLP/classification policy primitives. |
| `CDCavell.AsiBackbone.Core.Signing` | Clear home for signing-ready metadata, canonical hashing/signing seams, and verification policy primitives. |
| `CDCavell.AsiBackbone.Storage.InMemory` | Clear storage-provider package boundary for non-durable local validation. |
| `CDCavell.AsiBackbone.EntityFrameworkCore` | Clear EF Core integration boundary. |
| `CDCavell.AsiBackbone.AspNetCore` | Clear ASP.NET Core host adapter boundary. |
| `CDCavell.AsiBackbone.Analyzers` | Clear analyzer package boundary for build-time diagnostics. |
| `CDCavell.AsiBackbone.OpenTelemetry` | Clear OpenTelemetry provider package boundary. |
| `CDCavell.AsiBackbone.Signing.LocalDevelopment` | Clear local-development signing provider boundary. |
| `CDCavell.AsiBackbone.Signing.ManagedKey` | Clear managed-key signing adapter boundary. |

The stale `CDCavell.AsiBackbone.Core.Tokens` reference from the historical review is corrected to `CDCavell.AsiBackbone.Core.CapabilityTokens`.

## Dependency direction review

The dependency direction was acceptable for the initial stable package family and remains the rule for `1.1.0` provider additions:

```text
CDCavell.AsiBackbone.Core
  <- CDCavell.AsiBackbone.Storage.InMemory
  <- CDCavell.AsiBackbone.EntityFrameworkCore
  <- CDCavell.AsiBackbone.AspNetCore
  <- CDCavell.AsiBackbone.Analyzers
  <- CDCavell.AsiBackbone.OpenTelemetry
  <- CDCavell.AsiBackbone.Signing.LocalDevelopment
  <- CDCavell.AsiBackbone.Signing.ManagedKey
```

Review notes:

- Core remains the root package and should not reference integration packages.
- In-memory storage depends on Core only.
- EF Core integration depends on Core and EF Core packages.
- ASP.NET Core integration depends on Core and ASP.NET Core framework services.
- Analyzer, OpenTelemetry, and signing-provider packages depend on Core and their own package-specific dependencies only.
- No reviewed package requires a dependency on a future Event Hubs provider, Purview provider, Azure Monitor-specific provider, robotics package, AI-model package, immutable-storage package, or external-anchoring package.

No dependency-direction breaking changes were identified.

## Extension point review

The reviewed extension points were acceptable for `1.0.0`:

| Extension point | Review result |
| --- | --- |
| `IAsiBackboneActorContext` | Acceptable framework-neutral actor abstraction. Keeps host identity/authentication ownership explicit. |
| `IAsiBackboneConstraint<TContext>` | Acceptable host-extensible constraint abstraction with async evaluation and cancellation support. |
| `IAsiBackbonePolicyEvaluator<TContext>` | Acceptable composition point for converting constraint output into governance decisions. |
| `IAsiBackboneDecisionPolicy` | Acceptable customization point for post-composition decision policy. |
| `IAsiBackboneAuditSink` | Acceptable minimal audit emission contract. |
| `IAsiBackboneAuditLedgerStore` | Acceptable framework-neutral persisted ledger store contract. |
| ASP.NET Core actor/correlation/challenge services | Acceptable host adapter contracts. They should remain adapters, not host policy owners. |
| `AddAsiBackboneAspNetCore` | Acceptable service-registration entry point. |
| `ApplyAsiBackboneConfigurations` | Acceptable EF Core model configuration entry point for host-owned DbContexts. |

The `1.1.0` addendum recognizes additional stable extension seams:

| Extension point | Review result |
| --- | --- |
| `IAsiBackboneGovernanceEmitter` | Acceptable provider-neutral governance emission seam. Concrete providers adapt this contract downstream. |
| `IAsiBackboneGovernanceOutboxStore` | Acceptable provider-neutral durable outbox seam. Host/storage packages own persistence behavior. |
| `AsiBackboneGovernanceOutboxDrain` | Acceptable provider-neutral drain helper. Concrete emission provider remains replaceable. |
| OpenTelemetry governance emitter registration | Acceptable provider package registration boundary. Exporter configuration remains host-owned. |
| Signing provider abstractions and verification policy seams | Acceptable provider-neutral trust-boundary seams. They do not imply production tamper-evidence by themselves. |
| Managed-key signing adapter boundary | Acceptable adapter boundary when the host supplies the actual managed-key client and operational policy. |

No required breaking changes were identified.

## Artifact and schema review

Stable persisted or exported artifacts should remain covered by schema-version guidance.

Reviewed artifact families now include:

- audit residue;
- audit ledger records;
- audit residue lifecycle events;
- acknowledgment and responsibility-handshake records;
- capability token records;
- governance emission envelopes and payload descriptors;
- governance outbox records;
- signing-ready metadata and canonical payload/hash artifacts;
- policy version, policy hash, correlation ID, trace ID, actor ID, record ID, and schema-version fields.

Review decision:

- the stable direction remains acceptable because durable artifacts are documented separately from package versioning;
- future additive fields should remain compatible where possible;
- future incompatible durable shape changes should use schema-version guidance and migration documentation;
- signing and verification seams are stable, but production key management, immutable storage, external anchoring, and compliance certification remain host-owned or future-provider concerns unless separately implemented and released.

## Findings

### Required breaking changes

None identified in this review.

### Non-blocking follow-up considerations

The following were not required before `1.0.0`, but should remain visible for later milestones:

- Core architecture boundary checks are tracked by [API Baseline and Architecture Boundary Checks](api-baseline-and-boundary-checks.md) and should continue to protect Core from integration/provider dependencies;
- generated public API baseline files remain explicitly deferred to a later `1.x` milestone so the project can choose one stable public API drift process;
- consider adding package-specific API review notes for future provider packages before those packages become stable;
- consider adding upgrade notes if any later provider package introduces stable serialized artifacts;
- keep release notes and package READMEs aligned when new stable packages are added.

For the post-`1.1.0` line, future Event Hubs, Purview, Azure-specific SDK adapters, robotics, immutable-storage, timestamping, or external-anchoring packages should remain design-only, strategy-only, preview, or sample-only until they complete their own stable package review.

## Release decision

The initial stable package family was acceptable to proceed toward `1.0.0` from an API-shape perspective, provided release validation passed and any new API concerns discovered before tagging were resolved or intentionally deferred with follow-up issues.

The `1.1.0` stable package-family expansion is additive and compatible with the original `1.0.0` package-family boundary. Its added stable package surfaces should be treated as part of the compatible `1.x` contract once released.

This review does not replace CI, tests, package smoke validation, DocFX validation, or release version checks. It documents the historical stable public API and package-boundary review for issue #13 plus the `1.1.0` stable package-family addendum.

## Related documentation

- [1.1.0 Release Notes](release-notes-110.md)
- [API Compatibility and SemVer](api-compatibility-and-semver.md)
- [Upgrade Guide: 1.0.0 to 1.1.0](upgrade-100-to-110.md)
- [Release Validation](release-validation.md)
- [API Baseline and Architecture Boundary Checks](api-baseline-and-boundary-checks.md)
- [Schema Versioning](schema-versioning.md)
- [Privacy and Signing Boundaries](privacy-and-signing-boundaries.md)
- [Developer Checklist](developer-checklist.md)
