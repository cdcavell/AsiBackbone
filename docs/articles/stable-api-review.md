# Stable API Review

This review records the public API review for the initial stable AsiBackbone package family before committing to a `1.0.0` surface.

In this software project, **ASI** means **Accountable Systems Infrastructure**. This review is limited to implemented .NET package APIs and does not treat AsiBackbone as an artificial superintelligence implementation, AI model host, legal/compliance guarantee, or robot controller.

## Review status

| Item | Status |
| --- | --- |
| Public API review documented | Complete |
| Required breaking changes identified | None identified in this review |
| Package boundaries reviewed | Complete |
| Dependency direction reviewed | Complete |
| Stable release dependency | `1.0.0` should not be tagged until release validation passes and any later review findings are resolved or intentionally deferred. |

## Reviewed package family

The initial stable package family reviewed here is:

| Package | Reviewed role | Boundary decision |
| --- | --- | --- |
| `CDCavell.AsiBackbone.Core` | Framework-neutral governance primitives, actor context, constraints, decision results, audit contracts, acknowledgment primitives, capability token abstractions, and operation results. | Stable candidate. Should remain independent of ASP.NET Core, EF Core, cloud, robotics, AI model, and host-template dependencies. |
| `CDCavell.AsiBackbone.Storage.InMemory` | Non-durable in-memory storage helpers for tests, samples, and local validation. | Stable candidate with a clear non-durable boundary. Should not be described as production storage. |
| `CDCavell.AsiBackbone.EntityFrameworkCore` | EF Core model configuration and host-owned persistence integration. | Stable candidate as host-owned EF Core integration. Should not own migrations, database lifecycle, provider selection, or retention policy. |
| `CDCavell.AsiBackbone.AspNetCore` | Thin ASP.NET Core host adapters for actor context, request correlation, result mapping, and acknowledgment challenge support. | Stable candidate as host integration. Should not become the owner of host authentication, authorization, routing, policy evaluation, persistence, UI, or execution behavior. |

## Public naming review

The reviewed public naming pattern is acceptable for `1.0.0`:

- package names consistently use `CDCavell.AsiBackbone.*`;
- namespaces mirror package boundaries;
- public host integration types use the `AsiBackbone` prefix where they are package-specific;
- Core domain types such as `GovernanceDecision`, `OperationResult`, `AuditResidue`, and `AuditLedgerRecord` are acceptable without repeating the prefix because they are already under the `CDCavell.AsiBackbone.Core` namespace;
- ASP.NET Core and EF Core types carry package-specific names where ambiguity is likely;
- extension method names are readable and host-oriented, such as `AddAsiBackboneAspNetCore` and `ApplyAsiBackboneConfigurations`.

No required renames were identified.

## Namespace review

The namespace layout is acceptable for `1.0.0`:

| Namespace area | Review result |
| --- | --- |
| `CDCavell.AsiBackbone.Core.Actors` | Clear home for framework-neutral actor context abstractions and defaults. |
| `CDCavell.AsiBackbone.Core.Constraints` | Clear home for policy constraint abstractions and evaluation results. |
| `CDCavell.AsiBackbone.Core.Decisions` | Clear home for composed governance decisions and outcomes. |
| `CDCavell.AsiBackbone.Core.Evaluation` | Clear home for evaluator and decision policy contracts. |
| `CDCavell.AsiBackbone.Core.Audit` | Clear home for audit residue, ledger records, and ledger store contracts. |
| `CDCavell.AsiBackbone.Core.Handshakes` | Clear home for acknowledgment and responsibility-handshake primitives. |
| `CDCavell.AsiBackbone.Core.Tokens` | Clear home for capability token abstractions. |
| `CDCavell.AsiBackbone.Storage.InMemory` | Clear storage-provider package boundary for non-durable local validation. |
| `CDCavell.AsiBackbone.EntityFrameworkCore` | Clear EF Core integration boundary. |
| `CDCavell.AsiBackbone.AspNetCore` | Clear ASP.NET Core host adapter boundary. |

No required namespace changes were identified.

## Dependency direction review

The dependency direction is acceptable for the initial stable package family:

```text
CDCavell.AsiBackbone.Core
  <- CDCavell.AsiBackbone.Storage.InMemory
  <- CDCavell.AsiBackbone.EntityFrameworkCore
  <- CDCavell.AsiBackbone.AspNetCore
```

Review notes:

- Core remains the root package and should not reference integration packages.
- In-memory storage depends on Core only.
- EF Core integration depends on Core and EF Core packages.
- ASP.NET Core integration depends on Core and ASP.NET Core framework services.
- No reviewed package requires a dependency on a future provider package, signing provider, cloud provider, robotics package, or AI-model package.

No dependency-direction breaking changes were identified.

## Extension point review

The reviewed extension points are acceptable for `1.0.0`:

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

No required breaking changes were identified.

## Artifact and schema review

Stable persisted or exported artifacts should remain covered by schema-version guidance.

Reviewed artifact families:

- audit residue;
- audit ledger records;
- acknowledgment and responsibility-handshake records;
- capability token records;
- policy version, policy hash, correlation ID, trace ID, actor ID, record ID, and schema-version fields.

Review decision:

- the current stable direction is acceptable because durable artifacts are documented separately from package versioning;
- future additive fields should remain compatible where possible;
- future incompatible durable shape changes should use schema-version guidance and migration documentation;
- built-in signing, key management, tamper-evident storage, privacy classification, and compliance certification remain outside the `1.0.0` stable package boundary.

## Findings

### Required breaking changes

None identified in this review.

### Non-blocking follow-up considerations

The following are not required before `1.0.0`, but should remain visible for later milestones:

- Core architecture boundary checks are tracked by [API Baseline and Architecture Boundary Checks](api-baseline-and-boundary-checks.md) and should continue to protect Core from integration/provider dependencies;
- generated public API baseline files remain explicitly deferred to a later `1.x` milestone so the project can choose one stable public API drift process;
- consider adding package-specific API review notes for future provider packages before those packages become stable;
- consider adding upgrade notes if any later provider package introduces stable serialized artifacts;
- keep release notes and package READMEs aligned when new stable packages are added.

## Release decision

The initial stable package family is acceptable to proceed toward `1.0.0` from an API-shape perspective, provided release validation passes and any new API concerns discovered before tagging are resolved or intentionally deferred with follow-up issues.

This review does not replace CI, tests, package smoke validation, DocFX validation, or release version checks. It only documents the stable public API and package-boundary review for issue #13.

## Related documentation

- [API Compatibility and SemVer](api-compatibility-and-semver.md)
- [API Baseline and Architecture Boundary Checks](api-baseline-and-boundary-checks.md)
- [Schema Versioning](schema-versioning.md)
- [Privacy and Signing Boundaries](privacy-and-signing-boundaries.md)
- [Release Validation](release-validation.md)
- [Developer Checklist](developer-checklist.md)
