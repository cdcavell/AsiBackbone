# AsiBackbone 3.0.1 Release Notes

Release date: 2026-07-13

## Summary

`3.0.1` is a compatible patch release for the stable `3.0.x` AsiBackbone package family. It corrects three threat-model decision-integrity boundaries identified after `3.0.0` without changing package IDs, public namespaces, the `net10.0` target, or the stable `AssemblyVersion` of `3.0.0.0`.

The release keeps AsiBackbone within its established role as Accountable Systems Infrastructure for governed .NET decision flow: a governance spine, not an intelligence engine, security product, compliance certification, or host execution system.

## Fixed

### Undefined threat enum values now fail closed

`ThreatAssessment` now rejects undefined numeric values for `ThreatSeverity` and `GovernanceDecisionOutcome` during construction.

This prevents malformed or unvalidated enum values from reaching evaluator fallback behavior and disappearing from decision composition. When malformed construction occurs inside a registered contributor, the evaluator's default contributor-exception policy converts the failure into a controlled denied decision. Cancellation and critical runtime failures continue to propagate.

### Framework threat provenance cannot be overwritten

Contributor metadata can no longer use keys beginning with `threat.`. Prefix matching is case-insensitive and occurs after key trimming.

The entire namespace is reserved for framework-generated provenance, including contributor identity, category, severity, confidence, recommended outcome, and evaluator-selected effective outcome. This also protects future framework provenance keys from silent collision.

Contributor-owned metadata should use a host- or extension-owned namespace such as `contoso.threat_detector.*` or `replay_check.*`.

### Restrictive outcomes retain their matching reason

When multiple contributors return different actionable outcomes, the evaluator now retains the reason associated with the selected most-restrictive non-denial outcome.

For `Deferred`, `AcknowledgmentRequired`, and `EscalationRecommended`, the first contributor reason whose effective outcome matches the selected final outcome is retained. Contributor registration order remains the deterministic tie-breaker when multiple contributors return the same selected outcome.

Existing behavior remains unchanged for:

- `Denied`, which retains all accumulated actionable threat reasons in contributor order;
- warning-only evaluation, which retains all warning reasons in contributor order; and
- `ThreatAssessment.NoThreat()`, which contributes no reason.

## Compatibility notes

- Package IDs and public namespaces are unchanged.
- The target framework remains `net10.0`.
- `AssemblyVersion` remains `3.0.0.0`; `FileVersion` advances to `3.0.1.0`.
- Callers constructing assessments from unvalidated numeric or deserialized enum values may now receive `ArgumentOutOfRangeException`.
- Contributors that place custom metadata under `threat.*` must move those values to a contributor- or host-owned namespace.
- No new production key-management, replay-protection, execution, robotics, legal, or compliance capability is introduced.

## Package signing posture

NuGet package signing remains intentionally deferred while AsiBackbone is independently maintained. Consumers should continue to rely on the official NuGet source, public repository, release tags, Source Link repository metadata, SBOMs, provenance artifacts where available, and reproducible release practices.

## Validation

The release candidate should pass:

- Debug and Release solution builds;
- formatting and analyzer validation;
- all unit and integration tests;
- repository-wide and package-specific coverage gates;
- Core branch coverage;
- XML-documentation inventory validation;
- package creation and NuGet metadata validation;
- template, external-consumer, and stable-package smoke tests;
- version-consistency validation;
- DocFX generation and link validation;
- documentation release-claim validation;
- CodeQL and dependency review; and
- SBOM and provenance handling where supported.

After publication, validate Source Link repository metadata with:

```powershell
./scripts/Validate-Source-Link-commit-metadata.ps1 -Version 3.0.1
```
