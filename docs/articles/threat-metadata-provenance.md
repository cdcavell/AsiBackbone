# Threat provenance metadata

`ThreatAssessment.Metadata` accepts sanitized contributor-supplied metadata that is retained on generated operation reasons. The framework owns the complete `threat.` metadata namespace.

Contributor metadata keys are trimmed before validation. Any key beginning with `threat.` is rejected case-insensitively with an `ArgumentException` whose parameter name is `metadata`.

The reserved namespace includes the current canonical provenance fields:

- `threat.category`
- `threat.severity`
- `threat.recommended_outcome`
- `threat.effective_outcome`
- `threat.confidence`
- `threat.contributor`

The rule intentionally reserves the full namespace rather than only the current canonical list. This lets AsiBackbone add framework-owned threat provenance fields later without risking collisions with existing contributor extensions.

Use a host- or contributor-owned namespace for additional metadata, such as:

```csharp
ThreatAssessment.Create(
    ThreatSeverity.Medium,
    ThreatCategories.RegionPolicyMismatch,
    "threat.region_policy_mismatch",
    "The request region did not match the resolved policy region.",
    GovernanceDecisionOutcome.Deferred,
    metadata: new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["region.requested"] = "US-LA",
        ["region.resolved"] = "US-TX"
    });
```

Do not attempt to restate or override framework provenance:

```csharp
// Throws ArgumentException because threat.* is framework-owned.
ThreatAssessment.Create(
    ThreatSeverity.High,
    ThreatCategories.PolicyBypassAttempt,
    "threat.policy_bypass_attempt",
    "A policy bypass attempt was reported.",
    GovernanceDecisionOutcome.Denied,
    metadata: new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["threat.effective_outcome"] = "Allowed"
    });
```

This validation keeps the evaluator-selected outcome and the evidence retained in decision reasons, telemetry, and audit records consistent. Non-reserved contributor metadata remains supported and is normalized using the existing key/value trimming behavior.
