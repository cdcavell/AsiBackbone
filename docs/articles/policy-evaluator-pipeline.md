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
8. When `AsiBackbonePolicyEvaluatorOptions.TreatConstraintExceptionAsDenial` is enabled, a non-cancellation exception thrown by a constraint becomes a denied decision with reason code `asibackbone.policy.constraint_exception`.

The evaluator propagates correlation, policy version, and policy hash metadata from the evaluation context into the composed governance decision.

When an optional `ILogger<DefaultAsiBackbonePolicyEvaluator<TContext>>` is supplied and evaluation runs with zero constraints while `DenyWhenNoConstraints` is `false`, the evaluator emits a warning. This preserves backward compatibility while making the permissive empty-policy path visible in operational logs.

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

## Future-major compatibility note

Changing the default value of `DenyWhenNoConstraints` from `false` to `true` would alter documented decision behavior and should be treated as a future-major-version candidate, not as a patch or minor release change. Any future flip should include migration notes for hosts that intentionally rely on permissive empty-policy evaluation.

Changing the default value of `TreatConstraintExceptionAsDenial` from `false` to `true` would also alter documented exception behavior and should be handled at a future major-version boundary if the project later decides that fail-closed decision artifacts should become the default.

## Optional fast-abort on first blocked result

By default, the evaluator runs every registered constraint so the resulting decision and downstream decision policy have the fullest available reason visibility. This comprehensive path is the safest default for audit receipts, diagnostics, and policy review.

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

When enabled, the evaluator stops iterating constraints immediately after a blocked constraint result. The composed decision still includes the block reasons produced at the abort point and any warning reasons produced before that point. Constraints later in the list are not evaluated, so their warnings, block reasons, telemetry side effects, or expensive checks are intentionally skipped.

Use this mode only when the host explicitly prefers latency or throughput over complete constraint visibility. Keep the default full-evaluation mode for audit-heavy, diagnostic, or reviewer-facing paths.

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
