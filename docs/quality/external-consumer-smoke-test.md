# External Consumer Smoke Test

The external consumer smoke test validates AsiBackbone from the perspective of a fresh host application that consumes package-shaped artifacts instead of repository-internal project references.

This check exists to answer a pre-release confidence question:

> Can a clean consumer-style host wire the packages with minimal steps while preserving host-owned boundaries?

## What the smoke test validates

The smoke test script packs the projects under `src/` into local `.nupkg` files, creates a temporary xUnit project outside the solution structure, installs the local packages through a temporary `NuGet.config`, and runs HTTP-based assertions through `Microsoft.AspNetCore.TestHost`.

The generated consumer project validates:

- Core + ASP.NET Core adapter registration through `AddAsiBackboneAspNetCore()`;
- no package-owned MVC, auth, or EF provider assumptions;
- a host-owned `DbContext` that applies AsiBackbone EF Core model configuration;
- the EF Core audit ledger path through `EfCoreAuditLedgerStore`;
- the in-memory audit path through `InMemoryAuditLedger` for a minimal non-durable host;
- allow, deny, and require-acknowledgment decision flows over HTTP.

## Run locally

From the repository root:

```bash
bash ./eng/smoke-tests/external-consumer-smoke.sh
```

The script writes package artifacts to:

```text
artifacts/packages
```

It creates the temporary external consumer project under:

```text
artifacts/external-consumer-smoke
```

Both locations are safe to delete after the run.

## Workflow

The GitHub Actions workflow is defined at:

```text
.github/workflows/external-consumer-smoke.yml
```

The workflow runs the same script on push, pull request, and manual dispatch.

## Boundary expectations

This smoke test intentionally avoids adding project references to the generated consumer test project. The generated project should behave like an outside adopter using packages.

The EF Core path remains host-owned:

- the host declares its own `SmokeHostDbContext`;
- the host chooses SQLite for the smoke test;
- the host calls `ApplyAsiBackboneConfigurations()` from its own `OnModelCreating` method;
- the host registers its `DbContext` as the `DbContext` consumed by `EfCoreAuditLedgerStore`.

The non-durable path remains explicit:

- the host registers `InMemoryAuditLedger`;
- the host maps it to `IAsiBackboneAuditSink`;
- the smoke test verifies that in-memory records are written alongside EF ledger records.

## Decision flows

The HTTP smoke path exercises three outcomes:

| Route | Expected outcome | Purpose |
| --- | --- | --- |
| `/decisions/allow` | `Allowed` | Confirms a routine request can proceed and is written to both audit paths. |
| `/decisions/deny` | `Denied` | Confirms host-defined constraint failure blocks execution and preserves reason codes. |
| `/decisions/ack` | `AcknowledgmentRequired` | Confirms host-defined decision policy can require acknowledgment before execution. |

## Pre-release use

Run this smoke test before packaging or release-candidate validation. A failure usually means one of the following changed:

- package dependency metadata;
- DI registration ergonomics;
- host-owned EF Core integration boundaries;
- in-memory storage registration expectations;
- minimal HTTP host behavior.

Treat failures as package-consumer issues first, not sample-host issues, because the generated smoke project is deliberately outside the solution's normal project-reference graph.
