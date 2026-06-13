# API Compatibility and Semantic Versioning

This article defines the public API compatibility promise for the first stable `1.0.0` AsiBackbone package family and documents how semantic versioning is expected to apply after stabilization.

It complements the stable API review tracked in [issue #13](https://github.com/cdcavell/AsiBackbone/issues/13). That review verifies the public type names, namespaces, package boundaries, dependency direction, and extension points before the package family commits to a stable surface.

## Compatibility promise for `1.0.0`

Starting with `1.0.0`, packages identified as stable are expected to preserve their documented public API surface for consumers within the same major version.

The compatibility promise applies to:

- public types and members exposed by stable packages;
- public namespaces intended for consumer use;
- documented extension points and service registration methods;
- documented option shapes and default behavior;
- durable artifact shapes that are explicitly described as stable, including their schema version where applicable.

The promise does not mean implementation internals will never change. Internal code, private members, tests, documentation wording, samples, and non-public implementation details may change in minor or patch releases when the public contract remains compatible.

## Stable package scope

At `1.0.0`, the stable contract should be limited to packages explicitly released as stable.

The expected initial stable package line may include the package surfaces that have completed stable API review, such as:

- `CDCavell.AsiBackbone.Core`
- `CDCavell.AsiBackbone.AspNetCore`
- `CDCavell.AsiBackbone.Storage.InMemory`
- `CDCavell.AsiBackbone.EntityFrameworkCore`

Future or provider-specific packages are not automatically part of the stable Core contract. Packages such as signing providers, external gateway providers, robotics examples, cloud-specific emitters, telemetry providers, or other later integrations may remain preview even when Core is stable. Those packages should communicate their own stability level through package versioning, README guidance, and documentation.

## What counts as a breaking change

After `1.0.0`, a breaking change should require a new major version when it affects a stable package contract. Examples include:

- removing or renaming a public type, member, enum value, interface, namespace, or package;
- changing public method signatures, constructor signatures, generic constraints, or return types;
- changing documented default behavior in a way that can alter consumer outcomes;
- making previously optional configuration required;
- changing stable serialized or persisted artifact shapes without a compatible reader, migration path, or schema-versioned transition;
- changing service registration behavior in a way that breaks existing host startup code;
- changing package dependency direction or adding a framework dependency that violates a documented package boundary;
- changing documented exception behavior where callers are expected to handle that behavior.

A change is not usually breaking when it only adds new optional APIs, adds new optional configuration, improves implementation behavior without changing the public contract, fixes a bug to match documented behavior, or updates documentation and samples.

## Semantic versioning expectations

AsiBackbone follows Semantic Versioning expectations after stabilization:

| Version segment | Expected behavior |
| --- | --- |
| Major | May include intentional breaking changes to stable public APIs or stable artifact contracts. Breaking changes should be documented with migration notes. |
| Minor | Adds backward-compatible public APIs, options, adapters, or features. Existing stable APIs should continue to compile and behave compatibly. |
| Patch | Fixes bugs, documentation issues, packaging issues, or implementation defects without adding breaking public API changes. |
| Preview suffix | Indicates packages or features that are still under review and may change before stable release. |

Before `1.0.0`, alpha or preview packages may still make breaking changes as the API is shaped. Those changes should remain visible in the changelog and release notes.

## Durable artifact and schema-version policy

Stable persisted or exported governance artifacts should carry an explicit schema/version field when they may be stored or consumed outside the running process. See [Schema Versioning](schema-versioning.md) for artifact-specific guidance.

Schema versioning does not replace package versioning. Package versions describe the released library. Schema versions describe durable payload shapes that may outlive a single package release.

## Provider and future package guidance

Provider packages planned for later milestones should not be described as part of the stable Core contract until they complete their own API review and stable release checklist.

Examples of packages that may remain preview while the main package line stabilizes include:

- signing and key-management providers;
- cloud or observability emission packages;
- external system gateway integrations;
- robotics or physical execution examples;
- specialized persistence providers beyond the documented host-owned EF Core path.

Documentation should state whether each package is stable, preview, experimental, sample-only, or host-owned integration guidance.

## Release readiness checklist reference

Before cutting a stable `1.0.0` release, the release readiness checklist should confirm that:

- the stable API review in [issue #13](https://github.com/cdcavell/AsiBackbone/issues/13) is complete or intentionally deferred with follow-up issues;
- the stable package list is identified;
- public APIs for stable packages are reviewed for naming, namespace, dependency direction, and extension-point clarity;
- breaking changes found during review are resolved before release or captured for a future major version;
- stable serialized artifacts have documented schema-version behavior;
- preview/provider packages are not implied to be part of the stable Core contract;
- the changelog and release notes include the compatibility promise and migration guidance where needed.
