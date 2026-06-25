# API Baseline and Architecture Boundary Checks

This page records the post-`1.0` guardrail plan for the stable AsiBackbone package line.

In this software project, **ASI** means **Accountable Systems Infrastructure**. These checks protect the implemented package contracts; they do not turn AsiBackbone into an artificial superintelligence implementation, AI model host, robot controller, compliance product, signing system, or production audit guarantee.

## Purpose

The initial `1.0.0` release uses a documented stable API review and package-boundary review as the release-blocking API control. Post-`1.0`, additional automation should make the same contract easier to protect during normal `1.x` development.

The guardrails have two different jobs:

| Guardrail | Purpose | Current decision |
| --- | --- | --- |
| Public API drift detection | Detect unreviewed changes to stable public types, members, namespaces, and extension points. | Explicitly deferred to a later `1.x` milestone until the preferred analyzer/package-validation approach is selected. |
| Core architecture boundary checks | Fail if the Core package starts depending on integration or provider concerns such as ASP.NET Core, EF Core, cloud providers, robotics packages, or AI model packages. | Implemented as test coverage for the stable package project files. |

## Implemented boundary check

The Core test suite includes package-boundary checks that inspect stable package project files.

The checks verify that:

- `AsiBackbone.Core` has no `ProjectReference` entries;
- `AsiBackbone.Core` does not reference integration/provider package families such as ASP.NET Core, EF Core, cloud-provider packages, robotics packages, or AI model packages;
- `AsiBackbone.Storage.InMemory`, `AsiBackbone.EntityFrameworkCore`, and `AsiBackbone.AspNetCore` reference Core through the expected dependency direction instead of referencing each other through integration layers.

This keeps the stable package dependency direction aligned with the documented shape:

```text
AsiBackbone.Core
  <- AsiBackbone.Storage.InMemory
  <- AsiBackbone.EntityFrameworkCore
  <- AsiBackbone.AspNetCore
```

## Explicitly deferred API drift detection

Generated public API baselines remain deferred for the first post-`1.0` follow-up pass.

Accepted deferral:

- no `PublicAPI.Shipped.txt` / `PublicAPI.Unshipped.txt` files are added in this issue;
- no analyzer-based public API compatibility gate is added in this issue;
- no package-level binary compatibility tool is selected in this issue.

The deferral is intentional because the project should choose one stable process and avoid adding a noisy or poorly tuned baseline gate immediately after the first stable package publication.

## Preferred `1.x` implementation path

When public API drift detection is added, prefer an approach that:

1. runs in CI for pull requests touching stable package projects;
2. records approved public API additions through the normal SemVer process;
3. distinguishes additive minor-version APIs from breaking changes;
4. documents how maintainers update the baseline after review;
5. avoids treating samples, tests, docs, scripts, and non-public implementation details as stable API;
6. keeps future preview/provider packages out of the stable Core compatibility gate until they are explicitly promoted.

Candidate approaches include:

- analyzer-backed public API files such as `PublicAPI.Shipped.txt` / `PublicAPI.Unshipped.txt`;
- package-level API compatibility validation during release workflows;
- generated public surface reports reviewed as part of PR validation.

The project should select the smallest tool that reliably protects the stable package contract without making normal documentation or internal implementation work unnecessarily noisy.

## SemVer alignment

Public API drift checks should enforce the compatibility guidance in [API Compatibility and SemVer](api-compatibility-and-semver.md):

- patch releases should not intentionally break stable public APIs;
- minor releases may add compatible APIs, options, adapters, and behavior;
- major releases are reserved for breaking changes;
- preview/provider packages may follow their own stability path before being promoted.

Architecture boundary checks should enforce the same package-boundary rule documented by the stable API review: Core remains framework-neutral, while ASP.NET Core, EF Core, storage, gateway, signing, cloud, robotics, and provider concerns stay in their own packages or host applications.

## Release decision

Issue #177 is a post-`1.0` guardrail record.

For this issue:

- Core dependency-boundary checks are implemented in the test suite;
- public API drift detection is explicitly deferred to a later `1.x` milestone;
- the deferral is compatible with the existing stable API review, release readiness checklist, and API compatibility documentation.

The remaining API-baseline work should be prioritized before broad `1.x` API expansion, especially before adding new stable provider packages or new stable integration surfaces.
