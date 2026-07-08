# Strict Governance Profile

`AddAsiBackboneStrictGovernance()` is the explicit fail-closed governance profile for hosts that want production-oriented defaults without hiding those choices behind implicit package behavior.

The helper does not register authentication, authorization, host policy rules, audit persistence, endpoint middleware, or business execution. It configures the high-consequence AsiBackbone options that decide whether missing policy structure, policy exceptions, threat-contributor failures, and ungoverned endpoints fail open or fail closed.

## Why this exists

The current `3.x` line keeps strict posture explicit. A host can still construct an intentionally permissive evaluator for local validation, tests, samples, or explicitly unconstrained flows, but production hosts often want the opposite posture: missing governance structure should stop execution visibly instead of allowing the operation quietly.

The strict profile gives that posture one discoverable registration call while keeping final execution authority with the host application.

## Registration

For ASP.NET Core hosts, register the strict profile near the rest of the AsiBackbone service setup:

```csharp
using AsiBackbone.AspNetCore.DependencyInjection;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddAsiBackboneStrictGovernance();
builder.Services.AddAsiBackboneAspNetCore();
```

The same profile is also available through the explicit builder facade:

```csharp
builder.Services.AddAsiBackbone(backbone =>
{
    backbone.UseStrictGovernanceProfile();
    backbone.UseAspNetCoreEndpointGovernance();
});
```

Configuration order still matters. Later `Configure<TOptions>(...)` calls can intentionally override the strict profile. Calling the strict profile later re-applies the fail-closed settings.

## Options applied

| Option | Strict value | Reason |
| --- | --- | --- |
| `AsiBackbonePolicyEvaluatorOptions.DenyWhenNoConstraints` | `true` | Empty policy structure becomes a denied governance decision instead of an allowed decision. |
| `AsiBackbonePolicyEvaluatorOptions.TreatConstraintExceptionAsDenial` | `true` | Eligible policy constraint exceptions become safe denied decisions with stable reason codes. Cancellation and critical host/runtime failures still propagate. |
| `AsiBackbonePolicyEvaluatorOptions.TreatThreatContributorExceptionAsDenial` | `true` | Threat-model contributor failures fail closed when contributors are registered. |
| `AsiBackbonePolicyEvaluatorOptions.PreventThreatAssessmentAllowDowngrade` | `true` | Actionable threat assessment outcomes remain protected from being downgraded to pure allow decisions. |
| `AsiBackboneEndpointGovernanceOptions.FailClosedWhenPolicyEvaluatorMissing` | `true` | Endpoints that require policy evaluation fail closed when no evaluator is configured. |
| `AsiBackboneEndpointGovernanceOptions.FailClosedWhenCapabilityValidatorMissing` | `true` | Capability-gated endpoints fail closed when no capability validator is configured. |
| `AsiBackboneEndpointGovernanceOptions.FailClosedWhenAuditSinkMissing` | `true` | Audit-emitting endpoints fail closed when no host-owned audit sink is configured. |
| `AsiBackboneEndpointGovernanceOptions.RequireGovernanceMetadata` | `true` | Selected endpoints without governance metadata are blocked unless explicitly marked as allowed to omit governance metadata. |

## Empty-policy behavior

Without the strict profile, an evaluator with no constraints can preserve permissive evaluation behavior and emit an empty-policy warning when a logger is supplied.

With the strict profile applied, hosts should pass the configured `AsiBackbonePolicyEvaluatorOptions` into evaluator registrations. Empty-policy evaluation then returns a denied decision with the default `asibackbone.policy.no_constraints` reason code.

```csharp
builder.Services.AddSingleton<IAsiBackbonePolicyEvaluator<AsiBackboneConstraintEvaluationContext>>(serviceProvider =>
{
    var options = serviceProvider
        .GetRequiredService<IOptions<AsiBackbonePolicyEvaluatorOptions>>()
        .Value;

    return new DefaultAsiBackbonePolicyEvaluator<AsiBackboneConstraintEvaluationContext>(
        serviceProvider.GetServices<IAsiBackboneConstraint<AsiBackboneConstraintEvaluationContext>>(),
        decisionPolicy: null,
        options: options);
});
```

If a host constructs `DefaultAsiBackbonePolicyEvaluator<TContext>` manually with `new AsiBackbonePolicyEvaluatorOptions`, that manually supplied object owns the behavior. The strict registration helper configures DI options; it does not rewrite explicitly constructed option instances.

## Constraint-exception behavior

With the strict profile applied, eligible ordinary constraint failures are converted into denied governance decisions using the default `asibackbone.policy.constraint_exception` reason code and safe public reason message. Exception details remain in host logging/exception handling paths and are not copied into public decision reasons.

`OperationCanceledException` and critical host/runtime failures continue to propagate. The strict profile is a governance failure policy, not a corrupted-process recovery mechanism.

## Threat-contributor behavior

Threat-model contributor failures are also configured to fail closed. When a registered contributor fails with an eligible ordinary exception, the evaluator produces a denied decision with a stable reason code rather than silently continuing to execution.

This is especially important when a contributor screens replay indicators, capability-token mismatch, region-policy mismatch, prompt-injection-like tool requests, or unsafe external commands.

## Endpoint metadata behavior

When `RequireGovernanceMetadata` is enabled, endpoint governance treats unmarked endpoints as configuration failures. Public or intentionally ungoverned endpoints should opt out explicitly with the endpoint metadata helper provided by the ASP.NET Core package.

Use this to distinguish intentional public surface area from accidentally ungoverned execution paths.

## 3.x adoption path

Hosts upgrading to or starting with `3.0.0` can use this path:

1. Add `AddAsiBackboneStrictGovernance()` in non-production or staging first.
2. Confirm every governed endpoint has policy, capability, and audit metadata where expected.
3. Mark intentionally public endpoints with the explicit missing-governance-metadata opt-out.
4. Make policy evaluator registrations consume `IOptions<AsiBackbonePolicyEvaluatorOptions>` instead of creating unrelated option instances.
5. Monitor denied decisions for `asibackbone.policy.no_constraints`, `asibackbone.policy.constraint_exception`, and `asibackbone.threat.contributor_exception` reason codes.
6. Remove accidental empty-policy flows before enabling the profile in production.
7. Keep the helper in production when the host wants the fail-closed posture to be visible in code review and startup configuration.

This path keeps strict governance intentional while preserving the package boundary: AsiBackbone provides the evaluation posture, and the host owns execution enforcement, persistence, authentication, authorization, monitoring, and operational response.
