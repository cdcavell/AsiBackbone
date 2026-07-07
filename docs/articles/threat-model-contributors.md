# Threat Model Contributors

Threat model contributors let a host attach threat-aware checks to the AsiBackbone policy evaluation pipeline without turning AsiBackbone into a standalone security product.

The package provides the extension point and structured decision plumbing. The host remains responsible for defining the actual checks, thresholds, categories, and operational response.

## Why this exists

Policy evaluation already narrows a request into a governance decision such as allow, deny, defer, acknowledgment required, or escalation recommended. Threat model contributors add an earlier inspection point for signals such as malformed input, replay indicators, capability-token mismatch, regional-policy mismatch, or unsafe external command requests.

The intent is practical hardening:

- suspicious inputs should become traceable governance decisions;
- severe or high-confidence findings should not silently collapse to `Allow`;
- contributor failures should fail closed by default when contributors are explicitly registered;
- assessment details should appear in decision reasons and audit-facing metadata where the selected decision shape supports it.

## Extension point

Implement `IThreatModelContributor<TContext>` for the same framework-neutral context type used by the policy evaluator.

```csharp
using AsiBackbone.Core.Constraints;
using AsiBackbone.Core.Decisions;
using AsiBackbone.Core.ThreatModeling;

public sealed class ReplayThreatContributor : IThreatModelContributor<MyPolicyContext>
{
    public string Name => "replay-threat-contributor";

    public ValueTask<ThreatAssessment> AssessAsync(
        MyPolicyContext context,
        CancellationToken cancellationToken = default)
    {
        if (!context.Metadata.TryGetValue("request.nonce", out string? nonce) || string.IsNullOrWhiteSpace(nonce))
        {
            return ValueTask.FromResult(ThreatAssessment.Create(
                ThreatSeverity.High,
                ThreatCategories.ReplayAttempt,
                "threat.replay_nonce_missing",
                "The request did not include a replay-protection nonce.",
                GovernanceDecisionOutcome.EscalationRecommended));
        }

        return ValueTask.FromResult(ThreatAssessment.NoThreat());
    }
}
```

## Register with the default evaluator

Threat contributors are supplied explicitly to the default evaluator. Multiple contributors run in deterministic order before normal constraint composition.

```csharp
var evaluator = new DefaultAsiBackbonePolicyEvaluator<MyPolicyContext>(
    constraints,
    threatModelContributors,
    decisionPolicy,
    new AsiBackbonePolicyEvaluatorOptions
    {
        TreatThreatContributorExceptionAsDenial = true,
        PreventThreatAssessmentAllowDowngrade = true
    });
```

## Assessment fields

A `ThreatAssessment` includes:

- `Severity` — `None`, `Low`, `Medium`, `High`, or `Critical`.
- `Category` — a host-defined category; `ThreatCategories` provides common names.
- `ReasonCode` — a stable machine-readable code.
- `Description` — a human-readable explanation.
- `RecommendedOutcome` — the governance outcome the contributor recommends.
- `Confidence` — a value from `0.0` to `1.0`.
- `Metadata` — optional host metadata retained on generated operation reasons.

The built-in category constants are intentionally conventional, not exhaustive:

- `InputMalformed`
- `InputOversized`
- `PolicyBypassAttempt`
- `PromptInjectionLikeInput`
- `ReplayAttempt`
- `CapabilityTokenMismatch`
- `RegionPolicyMismatch`
- `UnsafeExternalCommand`
- `AuditIntegrityRisk`

Hosts may use their own category strings when these names do not fit.

## Fail-closed behavior

`TreatThreatContributorExceptionAsDenial` defaults to `true`. If a registered contributor throws, the evaluator returns a denied governance decision with the stable reason code `asibackbone.threat.contributor_exception`.

Set this option to `false` only when the host intentionally wants contributor exceptions to propagate to its own error boundary.

## Allow-downgrade protection

`PreventThreatAssessmentAllowDowngrade` defaults to `true`. When enabled, actionable threat assessments are protected from being silently replaced by a pure `Allow` decision from a custom decision policy.

This keeps suspicious, malformed, ambiguous, replayed, or unsafe inputs traceable in the governance trail.

## Boundary statement

Threat model contributors do not claim to detect all security threats, validate legal compliance, or provide legal protection. They are policy-pipeline hardening hooks: a host-owned way to transform threat-relevant signals into explicit, auditable governance decisions.
