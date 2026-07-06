# Constraint Exception Policy

This design note defines the intended behavior when an `IAsiBackboneConstraint<TContext>` throws during policy evaluation.

AsiBackbone is audit-first governance infrastructure. A thrown constraint exception may fail closed at the host boundary, especially in HTTP usage, but exception propagation alone can leave no `GovernanceDecision`, no stable reason code, and no downstream audit residue unless the host catches and records the failure.

## Default behavior: fail fast to the host

The stable default remains fail-fast exception propagation.

When a constraint throws and `AsiBackbonePolicyEvaluatorOptions.TreatConstraintExceptionAsDenial` is `false`, the evaluator does not catch and convert the exception. The exception propagates to the host application.

This preserves compatibility and lets hosts use their existing exception handling, retry, circuit-breaker, telemetry, and transaction policies.

## Opt-in behavior: fail closed as a denied decision

Hosts that prefer an accountability artifact can opt in:

```csharp
var evaluator = new DefaultAsiBackbonePolicyEvaluator<MyPolicyContext>(
    constraints: constraintsFromConfiguration,
    decisionPolicy: decisionPolicy,
    options: new AsiBackbonePolicyEvaluatorOptions
    {
        TreatConstraintExceptionAsDenial = true
    });
```

When enabled, a non-cancellation exception thrown by a constraint becomes a denied `GovernanceDecision` with reason code:

```text
asibackbone.policy.constraint_exception
```

The generated denial preserves:

- `CorrelationId`
- `PolicyVersion`
- `PolicyHash`

If a decision policy is configured, the generated denied decision is passed through that policy with a synthetic denied `ConstraintEvaluationResult` so downstream policy code can still observe a denial path.

## Sensitive exception details

Public decision reasons intentionally do not include exception messages, stack traces, connection strings, raw payloads, secrets, tokens, or user input.

The default public reason message is:

```text
A policy constraint failed during evaluation. The operation was denied by the evaluator failure policy.
```

Hosts may override the reason code and message, but should keep them curated, bounded, and free of sensitive data.

When a logger is supplied to `DefaultAsiBackbonePolicyEvaluator<TContext>`, the converted exception is logged at error level with the exception object attached to the log entry. This is host-owned operational telemetry, not public decision output. Hosts should apply their normal log redaction, retention, and access-control policy.

## Cancellation remains cancellation

`OperationCanceledException` is not converted into a denied decision. It continues to propagate so host cancellation semantics remain intact.

## Recommended host posture

Use the default fail-fast behavior when:

- the host already has reliable exception-to-audit handling;
- exceptions should abort a transaction and be handled by a central failure path;
- the host wants to distinguish infrastructure failure from policy denial outside the evaluator.

Use `TreatConstraintExceptionAsDenial = true` when:

- every governed attempt should produce a decision artifact;
- ASP.NET Core endpoint governance or another host layer should be able to audit the resulting denial normally;
- the host wants a consistent fail-closed policy surface for constraint failures;
- downstream systems rely on reason codes rather than exception propagation.

## Boundary

This option does not make exception data safe, sanitized, or compliance-ready by itself. It only converts the evaluator outcome into a denied decision with stable reason codes. Host applications still own logging policy, audit persistence, redaction, telemetry export, retention, and incident response.

## Related documentation

- [Core Policy Evaluator Pipeline](policy-evaluator-pipeline.md)
- [Safe Audit and Telemetry Data Guidance](safe-audit-telemetry-data.md)
- [Host-Owned Execution Enforcement](host-owned-execution-enforcement.md)
- [ASP.NET Core Endpoint Governance](aspnetcore-endpoint-governance.md)
