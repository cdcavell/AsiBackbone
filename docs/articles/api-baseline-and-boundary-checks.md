# API Baseline and Architecture Boundary Checks

This page documents the current guardrails used to protect the stable AsiBackbone package line.

In this software project, **ASI** means **Accountable Systems Infrastructure**. These checks protect implemented package contracts; they do not turn AsiBackbone into an artificial superintelligence implementation, AI model host, robot controller, compliance product, signing system, or production audit guarantee.

## Purpose

The stable `3.x` package line uses documented API compatibility rules, package-boundary tests, release validation, and review of public surface changes. Additional automated API-drift tooling may be added later when it can be introduced without creating noisy or poorly tuned release gates.

The guardrails have two different jobs:

| Guardrail | Purpose | Current posture |
| --- | --- | --- |
| Public API drift detection | Detect unreviewed changes to stable public types, members, namespaces, and extension points. | Governed through SemVer review, release validation, and public-surface review; generated public API baseline files are not currently used. |
| Core architecture boundary checks | Fail if the Core package starts depending on integration or provider concerns such as ASP.NET Core, EF Core, cloud providers, robotics packages, or AI model packages. | Implemented through test coverage for stable package project files. |

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

## Public API drift posture

Generated public API baselines are not currently part of the repository guardrail set.

The current posture is:

- no `PublicAPI.Shipped.txt` / `PublicAPI.Unshipped.txt` files are maintained;
- no analyzer-based public API compatibility gate is enabled;
- no package-level binary compatibility tool is enforced as a release-blocking check.

This is an intentional tooling decision, not permission to change public APIs casually. Public surface changes still require SemVer review, package-boundary review, documentation updates, and release validation.

## Future automation criteria

Any future public API drift tool should:

1. run in CI for pull requests touching stable package projects;
2. record approved public API additions through the normal SemVer process;
3. distinguish additive minor-version APIs from breaking changes;
4. document how maintainers update the baseline after review;
5. avoid treating samples, tests, docs, scripts, and non-public implementation details as stable API;
6. keep preview, provider-specific, or experimental packages outside the stable Core compatibility gate until explicitly promoted.

Candidate approaches include:

- analyzer-backed public API files such as `PublicAPI.Shipped.txt` / `PublicAPI.Unshipped.txt`;
- package-level API compatibility validation during release workflows;
- generated public surface reports reviewed as part of PR validation.

The project should select the smallest tool that reliably protects the stable package contract without making normal documentation or internal implementation work unnecessarily noisy.

## SemVer alignment

Public API review follows the compatibility guidance in [API Compatibility and SemVer](api-compatibility-and-semver.md):

- patch releases should not intentionally break stable public APIs;
- minor releases may add compatible APIs, options, adapters, and behavior;
- major releases are reserved for breaking changes;
- preview/provider packages may follow their own stability path before being promoted.

Architecture boundary checks enforce the same package rule: Core remains framework-neutral, while ASP.NET Core, EF Core, storage, gateway, signing, cloud, robotics, and provider concerns stay in their own packages or host applications.

## Current decision

- Core dependency-boundary checks are implemented in the test suite.
- Public API drift remains governed through SemVer, review, and release validation rather than generated baseline files.
- Future automation should be added only when its maintenance and failure behavior are clear.

Broad stable API expansion should continue to receive explicit compatibility review, especially when adding new provider packages or integration surfaces.