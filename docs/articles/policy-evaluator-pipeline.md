# Core Policy Evaluator Pipeline

This article documents the host-neutral policy evaluation loop for `AsiBackbone.Core`.

The Core evaluator proves the governance spine without requiring ASP.NET Core, Entity Framework Core, a database, a web host, robotics integration, or an AI model runtime.

```text
intent or request
  -> policy evaluation context
  -> constraint evaluation
  -> governance decision
  -> audit residue
  -> optional in-memory audit ledger
```

## Ownership model

The current stable package-family ownership model is:

| Area | Responsibility |
| --- | --- |
| `AsiBackbone.Core` | Policy evaluator contracts, the default evaluator, decision composition, constraint contracts, decisions, audit residue, and audit sink contracts. |
| `AsiBackbone.Storage.InMemory` | In-process audit ledger support for tests, samples, and local validation hosts. |
| `AsiBackbone.AspNetCore` | Thin HTTP host adapters for service registration, current actor resolution, request correlation, audit enrichment, HTTP result mapping, and acknowledgment challenge helpers. |
| `AsiBackbone.EntityFrameworkCore` | EF Core model configuration and durable accountability persistence while preserving host-owned `DbContext`, provider, migrations, and database lifecycle. |

A future package split may move shared contracts into a dedicated abstractions package. For the current stable package family, the contracts remain in Core so the evaluator can be used without requiring a larger package restructuring.

## Default evaluator

`DefaultAsiBackbonePolicyEvaluator<TContext>` accepts a framework-neutral context and a collection of `IAsiBackboneConstraint<TContext>` instances.

The evaluator runs each constraint and composes the resulting `ConstraintEvaluationResult` values into a single `GovernanceDecision`.

Composition rules are intentionally conservative:

1. Deny wins when any constraint blocks the request.
2. Warning is returned when no constraint blocks but at least one constraint warns.
3. Allow is returned when no constraint blocks or warns. This preserves the existing zero-constraint behavior unless strict empty-policy denial is enabled.
4. Not-applicable constraint results do not block the request.
5. An optional `IAsiBackboneDecisionPolicy<TContext>` can raise the composed decision to deferred, acknowledgment-required, or escalation-recommended.
6. When `AsiBackbonePolicyEvaluatorOptions.DenyWhenNoConstraints` is enabled and the supplied constraint collection is empty, the evaluator returns a denied decision with reason code `asibackbone.policy.no_constraints`.
7. When `AsiBackbonePolicyEvaluatorOptions.ShortCircuitOnFirstDenial` is enabled, the evaluator stops after the first blocked constraint result and preserves reasons produced up to that point.
8. When full evaluation finds a denial, warning-only reasons are not copied into the final denied decision; the denied decision remains focused on blocking rationale.
9. When `AsiBackbonePolicyEvaluatorOptions.TreatConstraintExceptionAsDenial` is enabled, a non-cancellation exception thrown by a constraint becomes a denied decision with reason code `asibackbone.policy.constraint_exception`.

The evaluator propagates correlation, policy version, and policy hash metadata from the evaluation context into the composed governance decision.

When an optional `ILogger<DefaultAsiBackbonePolicyEvaluator<TContext>>` is supplied and evaluation runs with zero constraints while `DenyWhenNoConstraints` is `false`, the evaluator emits a warning. This preserves backward compatibility while making the permissive empty-policy path visible in operational logs.

## Constructor overload selection

`DefaultAsiBackbonePolicyEvaluator<TContext>` keeps several constructor overloads so simple hosts, compatibility callers, and fully wired dependency-injection hosts can all create the evaluator. Pick the smallest overload that honestly represents the host posture, but do not drop configured options or diagnostics just to make registration shorter.

| Overload group | Use when | Guidance |
| --- | --- | --- |
| Constraints only | The host wants the default 2.x composition behavior and has no custom decision policy, explicit evaluator options, threat contributors, or logger. | This is the simplest compatibility path. It preserves permissive empty-policy behavior unless the host supplies options through another overload. |
| Constraints plus decision policy | The host wants normal constraint composition followed by a custom `IAsiBackboneDecisionPolicy<TContext>` that can raise or reshape the composed decision. | Use this when the policy needs outcomes such as defer, acknowledgment-required, or escalation-recommended after base composition. |
| Constraints plus evaluator options | The host needs explicit settings for empty-policy denial, constraint-exception denial, fast-abort behavior, or threat-assessment downgrade protection. | Strict and fail-closed hosts should pass the configured `AsiBackbonePolicyEvaluatorOptions` instance that represents the host posture. |
| Constraints plus threat model contributors | The host wants pre-constraint threat contributors to inspect the context and emit actionable warnings or blocking decisions. | Use one of the overloads that accepts `IEnumerable<IThreatModelContributor<TContext>>`; include evaluator options when contributor exception behavior matters. |
| Logger overloads | The host wants operational diagnostics for permissive empty policies, converted constraint exceptions, or converted threat-contributor exceptions. | Prefer the logger overload in production-style DI wiring so warnings and fail-closed conversions become visible in normal logging. |
| Full overload | The host has constraints, threat contributors, an optional decision policy, configured options, and a logger. | This is the most explicit DI path and is usually the clearest choice for strict production registrations. |

Strict or fail-closed hosts should pass configured options into the evaluator rather than constructing an unrelated default `new AsiBackbonePolicyEvaluatorOptions()` at the call site. This is especially important with `AddAsiBackboneStrictGovernance()` or `UseStrictGovernanceProfile()`: those helpers configure `IOptions<AsiBackbonePolicyEvaluatorOptions>`, but they cannot rewrite a manually constructed options object that the host passes directly to the evaluator.

A DI registration that preserves the strict profile and operational diagnostics should resolve the configured options and logger from the service provider:

```csharp
builder.Services.AddSingleton<IAsiBackbonePolicyEvaluator<AsiBackboneConstraintEvaluationContext>>(serviceProvider =>
{
    var options = serviceProvider
        .GetRequiredService<IOptions<AsiBackbonePolicyEvaluatorOptions>>()
        .Value;

    var logger = serviceProvider
        .GetService<ILogger<DefaultAsiBackbonePolicyEvaluator<AsiBackboneConstraintEvaluationContext>>>();

    return new DefaultAsiBackbonePolicyEvaluator<AsiBackboneConstraintEvaluationContext>(
        serviceProvider.GetServices<IAsiBackboneConstraint<AsiBackboneConstraintEvaluationContext>>(),
        serviceProvider.GetServices<IThreatModelContributor<AsiBackboneConstraintEvaluationContext>>(),
        decisionPolicy: serviceProvider.GetService<IAsiBackboneDecisionPolicy<AsiBackboneConstraintEvaluationContext>>(),
        options: options,
        logger: logger);
});
```

For intentionally permissive local samples, tests, or migration flows, using the shorter overloads is acceptable as long as the host clearly intends default 2.x behavior. For governed production surfaces, prefer the explicit overload that carries the host's configured options, threat contributors, decision policy, and logger.

## Minimal usage example

```csharp
var evaluator = new DefaultAsiBackbonePolicyEvaluator<MyPolicyContext>(
    constraints:
    [
        new AuthenticatedActorConstraint(),
        new OwnershipConstraint(),
        new RiskConstraint()
    ],
    decisionPolicy: new HighRiskDecisionPolicy());

GovernanceDecision decision = await evaluator.EvaluateAsync(
    context,
    cancellationToken);
```

For host-owned orchestration examples, see [Custom Decision Policy Examples](custom-decision-policy-examples.md). That article covers warning preservation, acknowledgment-required outcomes, regional overlays, gateway readiness checks, and the difference between policy evaluation and host-owned execution.

## Named sharp edge: permissive empty-policy evaluation

The current stable `2.x` default keeps `AsiBackbonePolicyEvaluatorOptions.DenyWhenNoConstraints` set to `false`. That means an evaluator created with an empty constraint collection can produce an allowed decision.

This default exists for backward compatibility and for intentionally unconstrained local flows. It is still a sharp edge for governance-sensitive hosts because an empty collection may also mean:

- dependency injection did not register expected constraints;
- dynamic policy discovery failed;
- a feature flag or configuration source returned no policy entries;
- a database-backed constraint loader returned an empty set after an outage or migration;
- a test or sample registration path accidentally reached production.

If a logger is supplied, the evaluator now emits a warning when this permissive empty-policy path is used. Treat that warning as an operational signal, not as a substitute for fail-closed configuration.

Hosts that expect at least one constraint should validate configuration at startup and should prefer `DenyWhenNoConstraints = true` for governed production surfaces.

```csharp
if (!constraintsFromConfiguration.Any())
{
    throw new InvalidOperationException(
        "Governance policy configuration produced no constraints for this host.");
}
```

## Strict default-deny for empty policies

By default, an empty constraint collection still composes to `Allowed` for backward compatibility and for hosts that intentionally run an unconstrained local validation flow. In zero-trust or dynamically configured deployments, however, an empty collection can also mean that a database, configuration, feature-flag, or dependency-injection policy load failed.

Hosts that require a fail-closed posture can opt in:

```csharp
var evaluator = new DefaultAsiBackbonePolicyEvaluator<MyPolicyContext>(
    constraints: constraintsFromConfiguration,
    decisionPolicy: new HighRiskDecisionPolicy(),
    options: new AsiBackbonePolicyEvaluatorOptions
    {
        DenyWhenNoConstraints = true
    });
```

When enabled and no constraints are supplied, the evaluator returns a denied `GovernanceDecision` with reason code `asibackbone.policy.no_constraints`, preserving correlation, policy version, and policy hash metadata from the evaluation context.

Permissive zero-constraint behavior is appropriate only when the host explicitly intends an unconstrained policy surface, such as a local sample, test harness, migration step, or separately protected flow. Strict default-deny should be used when constraints are loaded dynamically and an empty collection may indicate a policy-load failure.

This option is not a substitute for authentication, authorization, or host-level configuration validation.

## Constraint exception behavior

By default, if a constraint throws, the exception propagates to the host. This preserves compatibility and lets the host's existing exception handling, transaction, retry, telemetry, and incident-response policy decide what happens next.

Hosts that need a governance decision artifact even when a constraint fails can opt in:

```csharp
var evaluator = new DefaultAsiBackbonePolicyEvaluator<MyPolicyContext>(
    constraints: constraintsFromConfiguration,
    decisionPolicy: new HighRiskDecisionPolicy(),
    options: new AsiBackbonePolicyEvaluatorOptions
    {
        TreatConstraintExceptionAsDenial = true
    });
```

When enabled, a non-cancellation exception thrown by a constraint becomes a denied `GovernanceDecision` with reason code `asibackbone.policy.constraint_exception`. The generated denial preserves correlation ID, policy version, and policy hash. If a decision policy is configured, the denied decision is passed through that policy with a synthetic denied constraint result so downstream audit and policy code can observe the denial path.

Public reason messages intentionally do not include exception messages, stack traces, connection strings, raw payloads, secrets, tokens, or user input. When a logger is supplied, the exception object is attached to an error-level log entry. Hosts remain responsible for log redaction, retention, and access control.

`OperationCanceledException` is not converted into a denial; cancellation continues to propagate.

See [Constraint Exception Policy](constraint-exception-policy.md) for the design note and recommended host posture.

## Warning-only reason handling when denial occurs

`DefaultAsiBackbonePolicyEvaluator<TContext>` treats warning-only reasons as advisory audit context and denial reasons as the blocking rationale. When `ShortCircuitOnFirstDenial` is `false`, the evaluator keeps running after a denied constraint so it can aggregate every denial reason produced by the full active constraint structure. As soon as a denial appears in this full-evaluation mode, accumulated warning-only reasons are cleared from the composed decision and later warnings are ignored. This is intentional: the final denied `GovernanceDecision` remains focused on the reasons that blocked the operation instead of mixing advisory warnings with blocking rationale.

This differs from `ShortCircuitOnFirstDenial = true`. In fast-abort mode, the evaluator stops as soon as the first denial is seen. Warnings produced before that abort point remain in the denied decision because they are part of the evaluated path, while later constraints are intentionally skipped and cannot add denial or warning reasons.

Threat model warnings follow the same distinction. An actionable threat warning is protected from being downgraded to `Allowed` when the composed decision can proceed, including the empty-policy or all-allowed constraint path. When a later constraint denial is composed during full evaluation, the result is already a blocking decision, so threat warning reasons are not duplicated into the final denial reason set. Blocking threat outcomes such as denied, deferred, acknowledgment-required, or escalation-recommended still short-circuit before constraint evaluation and remain protected as their own governance decisions.

## Future-major compatibility note

Changing the default value of `DenyWhenNoConstraints` from `false` to `true` would alter documented decision behavior and should be treated as a future-major-version candidate, not as a patch or minor release change. Any future flip should include migration notes for hosts that intentionally rely on permissive empty-policy evaluation.

Changing the default value of `TreatConstraintExceptionAsDenial` from `false` to `true` would also alter documented exception behavior and should be handled at a future major-version boundary if the project later decides that fail-closed decision artifacts should become the default.

## Optional fast-abort on first blocked result

By default, the evaluator runs every registered constraint so the resulting decision, constraint result set, and downstream decision policy have the fullest available denial-reason visibility. This comprehensive path is the safest default for audit receipts, diagnostics, and policy review, while denied governance decisions remain focused on blocking reasons rather than warning-only audit messages.

Latency-sensitive hosts can opt into first-denial fast-abort behavior:

```csharp
var evaluator = new DefaultAsiBackbonePolicyEvaluator<MyPolicyContext>(
    constraints: constraintsFromConfiguration,
    decisionPolicy: new HighRiskDecisionPolicy(),
    options: new AsiBackbonePolicyEvaluatorOptions
    {
        ShortCircuitOnFirstDenial = true
    });
```

When enabled, the evaluator stops iterating constraints immediately after a blocked constraint result. The composed decision still includes the block reasons produced at the abort point and any warning reasons produced before that point, including actionable threat-model warning reasons. Constraints later in the list are not evaluated, so their warnings, block reasons, telemetry side effects, or expensive checks are intentionally skipped.

Use this mode only when the host explicitly prefers latency or throughput over complete constraint visibility. Keep the default full-evaluation mode for audit-heavy, diagnostic, or reviewer-facing paths where denial-reason aggregation is more important than preserving warning-only messages inside a denied decision.

### ASP.NET Core endpoint metadata exploration

ASP.NET Core integration includes endpoint metadata that can express a per-controller, per-action, or Minimal API preference through `ShortCircuitOnFirstDenialAttribute` or the `.ShortCircuitOnFirstDenial()` route-builder extension.

The endpoint descriptor exposes this as `ShortCircuitOnFirstDenial` and includes `endpoint.short_circuit_on_first_denial` in descriptor metadata. Host-owned ASP.NET Core policy wiring can use that metadata to decide whether to construct or resolve an evaluator with `AsiBackbonePolicyEvaluatorOptions.ShortCircuitOnFirstDenial = true` for that endpoint. The built-in default remains comprehensive unless the host explicitly maps endpoint metadata into evaluator configuration.

After the decision is produced, a host or gateway can create audit residue and write it through an audit sink:

```csharp
AuditResidue residue = AuditResidue.Create(
    actor: context.Actor,
    operationName: context.OperationName,
    outcome: decision.Outcome.ToString(),
    reasonCodes: decision.ReasonCodes,
    correlationId: decision.CorrelationId,
    policyVersion: decision.PolicyVersion,
    policyHash: decision.PolicyHash);

await auditSink.WriteAsync(residue, cancellationToken);
```

For local tests or samples, `InMemoryAuditLedger` can be used as the audit sink:

```csharp
var auditSink = new InMemoryAuditLedger();
await auditSink.WriteAsync(residue, cancellationToken);

IReadOnlyList<IAsiBackboneAuditResidue> matchingRecords =
    auditSink.GetByCorrelationId(decision.CorrelationId!);
```

## Boundaries

This evaluator does not execute the governed action. It decides whether the action should be allowed, denied, deferred, acknowledged, escalated, or allowed with warnings.

Execution still belongs to the host or gateway layer. That boundary keeps Core as governance infrastructure rather than an execution engine.

See [Host-Owned Execution Enforcement](host-owned-execution-enforcement.md) for recommended enforcement patterns across HTTP endpoints, service-layer calls, background workers, message consumers, CLI tools, and fail-closed review paths.

The Core evaluator itself deliberately avoids:

* ASP.NET Core middleware
* Entity Framework Core persistence
* robotics-specific gateway behavior
* legal-liability claims
* database requirements
* AI model hosting, training, inference, or orchestration
