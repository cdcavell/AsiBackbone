# Strict Governance Profile

`AddAsiBackboneStrictGovernance()` is the 2.x migration bridge for hosts that want a fail-closed governance posture without waiting for a possible 3.0.0 default change.

The helper does not register authentication, authorization, host policy rules, audit persistence, endpoint middleware, or business execution. It configures the high-consequence AsiBackbone options that decide whether missing policy structure, policy exceptions, and ungoverned endpoints fail open or fail closed.

## Why this exists

Stable 2.x preserves backward-compatible defaults. That means a host that intentionally constructs a permissive evaluator can still allow empty-policy flows, keep ordinary constraint exceptions in the host exception pipeline, and let endpoint governance ignore endpoints that do not carry governance metadata.

That compatibility is useful during adoption, but production hosts often want the opposite posture: missing governance structure should stop execution visibly instead of allowing the operation quietly.

The strict profile gives that posture one discoverable registration call.

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

With default 2.x behavior, an evaluator with no constraints preserves backward compatibility by allowing the decision and emitting an empty-policy warning when a logger is supplied.

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

## Endpoint metadata behavior

When `RequireGovernanceMetadata` is enabled, endpoint governance treats unmarked endpoints as configuration failures. Public or intentionally ungoverned endpoints should opt out explicitly with the endpoint metadata helper provided by the ASP.NET Core package.

Use this to distinguish intentional public surface area from accidentally ungoverned execution paths.

## Possible 3.0.0 migration path

A future 3.0.0 release could make fail-closed behavior the default. Hosts can prepare during 2.x by following this path:

1. Add `AddAsiBackboneStrictGovernance()` in non-production or staging first.
2. Confirm every governed endpoint has policy, capability, and audit metadata where expected.
3. Mark intentionally public endpoints with the explicit missing-governance-metadata opt-out.
4. Make policy evaluator registrations consume `IOptions<AsiBackbonePolicyEvaluatorOptions>` instead of creating unrelated option instances.
5. Monitor denied decisions for `asibackbone.policy.no_constraints` and `asibackbone.policy.constraint_exception` reason codes.
6. Remove accidental empty-policy flows before enabling the profile in production.
7. If 3.0.0 later flips defaults, keep the helper as documentation of intent or remove it once the host's default posture is verified.

This path keeps 2.x compatibility intact while giving production hosts an early, explicit fail-closed posture.
