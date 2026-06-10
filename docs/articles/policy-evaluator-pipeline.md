# Core Policy Evaluator Pipeline

This article documents the first host-neutral policy evaluation loop for `CDCavell.AsiBackbone.Core`.

The evaluator proves the initial governance spine without requiring ASP.NET Core, Entity Framework Core, a database, a web host, robotics integration, or an AI model runtime.

```text
intent or request
  -> policy evaluation context
  -> constraint evaluation
  -> governance decision
  -> audit residue
  -> optional in-memory audit ledger
```

## Ownership model

The initial alpha ownership model is:

| Area | Responsibility |
| --- | --- |
| `CDCavell.AsiBackbone.Core` | Policy evaluator contracts, the default evaluator, decision composition, constraint contracts, decisions, audit residue, and audit sink contracts. |
| `CDCavell.AsiBackbone.Storage.InMemory` | In-process audit ledger support for tests, samples, and local validation hosts. |
| Future `CDCavell.AsiBackbone.AspNetCore` | HTTP adaptation, current actor resolution, request-to-context mapping, and service registration. |
| Future `CDCavell.AsiBackbone.Storage.EntityFrameworkCore` | Durable persistence integration while preserving host-owned `DbContext` and database lifecycle. |

A future package split may move shared contracts into a dedicated abstractions package. For this alpha slice, the contracts remain in Core so the evaluator can be proven without widening the release branch into a larger package restructuring.

## Default evaluator

`DefaultAsiBackbonePolicyEvaluator<TContext>` accepts a framework-neutral context and a collection of `IAsiBackboneConstraint<TContext>` instances.

The evaluator runs each constraint and composes the resulting `ConstraintEvaluationResult` values into a single `GovernanceDecision`.

Composition rules are intentionally conservative:

1. Deny wins when any constraint denies the request.
2. Warning is returned when no constraint denies but at least one constraint warns.
3. Allow is returned when no constraint denies or warns.
4. Not-applicable constraint results do not block the request.
5. An optional `IAsiBackboneDecisionPolicy<TContext>` can raise the composed decision to deferred, acknowledgment-required, or escalation-recommended.

The evaluator propagates correlation, policy version, and policy hash metadata from the evaluation context into the composed governance decision.

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

The initial implementation deliberately avoids:

* ASP.NET Core middleware
* Entity Framework Core persistence
* robotics-specific gateway behavior
* legal-liability claims
* database requirements
* AI model hosting, training, inference, or orchestration
