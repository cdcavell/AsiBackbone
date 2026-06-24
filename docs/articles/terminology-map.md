# Terminology Map for .NET Developers

This guide translates AsiBackbone vocabulary into familiar .NET and application-architecture language.

Use it before the deeper architecture articles if the domain terms feel heavier than the first problem you are trying to solve. For most new adopters, the first problem is simple:

> Gate a request, explain the decision, and preserve a safe audit trail.

In this project, **ASI** means **Accountable Systems Infrastructure**. AsiBackbone is a governance spine for accountable software decision flow. It is not an intelligence engine, AI runtime, legal compliance engine, or replacement for host-owned authentication, authorization, persistence, monitoring, or operational controls.

## Start here: shortest reading path

For junior and mid-level developers, start in this order:

1. [First 15 Minutes: Standard API Gating](quickstart-api-gating.md) — shortest practical path for one ASP.NET Core endpoint.
2. [Getting Started](getting-started.md) — project orientation and package direction.
3. This terminology map — plain-language translation of the core vocabulary.
4. [1.0.0 Quickstart](quickstart-100.md) — minimum public API and package-consumer setup.
5. [Policy Evaluator Pipeline](policy-evaluator-pipeline.md) — how constraints compose into decisions.
6. [ASP.NET Core Endpoint Governance](aspnetcore-endpoint-governance.md) — endpoint metadata and middleware orchestration.
7. [Durable Audit and Outbox Persistence](durable-audit-outbox-persistence.md) — production-style persistence boundaries.
8. [OpenTelemetry Governance Emission Provider](opentelemetry-governance-emission-provider.md) — optional telemetry projection after local audit/outbox handling.

You do not need every article or every package to use Core or ASP.NET Core integration. Start with the decision path, then add persistence, outbox, emission, signing, and capability validation only when the host needs those behaviors.

## Required for first use versus advanced concepts

| Concept group | Required for first use? | When to learn it |
| --- | --- | --- |
| Constraint | Yes | When writing the first rule that allows or denies a request. |
| Evaluation context | Yes | When deciding which safe request facts a rule needs. |
| Governance decision | Yes | When the host needs to continue, deny, defer, warn, or require acknowledgment. |
| Audit residue | Usually | When the host wants a decision record for troubleshooting, review, or audit. |
| ASP.NET Core endpoint metadata | Optional | When the host wants route or controller metadata to declare governance intent. |
| Dynamic Liability Handshake | Advanced | When a risky action requires an explicit actor acknowledgment before execution. |
| Capability grant | Advanced | When execution needs a bounded, scoped permission after a decision. |
| Durable audit/outbox persistence | Advanced but production-important | When decision records or emissions must survive process restart. |
| Governance emission | Advanced | When decisions should be projected to observability, SIEM, event streaming, or enrichment providers. |
| Signing and verification | Advanced | When a host needs signing-ready or signed governance artifacts under host-owned key policy. |

## Compact terminology map

| AsiBackbone term | Plain-language meaning | Familiar .NET / architecture analogy |
| --- | --- | --- |
| Governance spine | The decision-flow layer around consequential operations. | Cross-cutting policy middleware / application governance layer. |
| Consequential action | An operation important enough to evaluate before running. | Administrative write, external API call, workflow transition, deployment step, sensitive data access. |
| Constraint | A rule that participates in a decision. | Authorization requirement, business rule, guard clause, validation rule. |
| Evaluation context | The safe facts passed into the rules. | Authorization handler context, validation context, policy input DTO. |
| Governance decision | The allow, deny, defer, warn, acknowledgment-required, or escalation-recommended result. | Authorization result, policy evaluation result, decision object. |
| Decision policy | Optional host logic that can adjust the composed decision. | Result transformer, policy post-processor, escalation rule. |
| Reason code | Machine-readable explanation for the decision. | Error code, policy code, validation code. |
| Reason message | Human-readable explanation for logs, review, or safe responses. | Validation message, audit detail, operator note. |
| Correlation ID | Identifier that connects the decision to the request or workflow. | Request ID, trace correlation ID, activity ID. |
| Policy version | Human-readable version of the active policy. | Ruleset version, configuration version, policy release label. |
| Policy hash | Stable identifier for the active policy material. | Configuration hash, ruleset checksum, policy fingerprint. |
| Actor context | Who or what attempted the operation. | Current user, service principal, workload identity, system actor. |
| Audit residue | Safe record of why a decision happened. | Audit log detail, decision receipt, policy evaluation record. |
| Audit sink | Component that receives audit residue. | Logger, audit writer, repository abstraction. |
| Audit ledger | Stored audit records. | Audit table, append-only log, review ledger. |
| Dynamic Liability Handshake | Explicit acknowledgment step before risky execution. | Consent checkpoint, responsibility acknowledgment, manual approval prompt. |
| Acknowledgment challenge | Host-facing prompt built from an acknowledgment-required decision. | UI challenge model, precondition response, approval form model. |
| Capability grant | Bounded permission to perform an operation. | Scoped access grant, short-lived capability, constrained execution token. |
| Gateway boundary | The point where a governed decision may lead to external execution. | Command gateway, tool boundary, outbound integration boundary. |
| Governance outbox | Durable queue of governance events waiting to be emitted. | Transactional outbox table, reliable delivery queue. |
| Outbox drain | Background process that delivers queued governance events. | Hosted service, worker, outbox publisher. |
| Governance emission | Event emitted after a governance decision or audit lifecycle change. | Domain event, telemetry event, audit event, SIEM feed event. |
| Governance emitter | Provider that sends governance envelopes to a downstream system. | Event publisher, telemetry adapter, exporter boundary. |
| DLP/classification policy | Rules for handling sensitive data posture and classification failures. | Data-loss-prevention decision policy, sensitivity-label handling, fail-open/fail-closed policy. |
| Signing-ready artifact | Record shaped so a host or provider can sign it later. | Canonical payload, hashable DTO, signature-prepared record. |
| Verification policy | Host rule for what to do with valid, invalid, missing, or unsupported signatures. | Trust policy, signature validation result handling. |

## The first-use path in ordinary language

A minimal host usually does this:

```text
Incoming request
  -> create safe evaluation context
  -> run one or more constraints
  -> receive governance decision
  -> write audit residue
  -> continue only when decision.CanProceed is true
```

The same path in AsiBackbone language is:

```text
Intent enters a governance boundary
  -> host builds an AsiBackboneConstraintEvaluationContext
  -> IAsiBackboneConstraint<TContext> instances evaluate the context
  -> IAsiBackbonePolicyEvaluator<TContext> composes a GovernanceDecision
  -> host creates and writes AuditResidue
  -> host-owned application logic decides whether to execute
```

The host remains in charge. AsiBackbone provides the decision vocabulary and primitives around the operation; it does not execute the operation for the host.

## Plain-language examples

### Constraint

A constraint answers a focused question:

- Is this region allowed?
- Is this operation within the actor's allowed risk level?
- Is the target resource classified in a way that blocks the request?
- Is an acknowledgment required before continuing?

Keep constraints small. A constraint should be easy to name, test, and explain through a reason code.

### Governance decision

A governance decision is the result the host can act on:

| Outcome | Ordinary host interpretation |
| --- | --- |
| `Allowed` | The governance layer found no reason to block. The host may continue. |
| `Warning` | The host may continue, but should preserve the warning. |
| `Denied` | The host should not perform the protected operation. |
| `Deferred` | The host should pause because the decision needs later processing. |
| `AcknowledgmentRequired` | The host should present an acknowledgment step before execution. |
| `EscalationRecommended` | The host should route the request to a higher-review path. |

### Audit residue

Audit residue is not just a log message. It is the decision record that helps answer:

- Who or what attempted the operation?
- What operation was attempted?
- What did the governance layer decide?
- Which reason codes shaped the decision?
- Which correlation ID, policy version, and policy hash were active?

Local samples can use in-memory storage. Production hosts should use durable, host-owned persistence when records must survive restart, support review, or feed an outbox.

### Governance emission

Governance emission is optional downstream projection. It should usually come after local audit/outbox persistence, not replace it.

Examples include:

- OpenTelemetry activity and metric projection.
- SIEM or audit-stream events.
- Event Hubs or similar event-streaming providers.
- Governance or lineage enrichment systems.

The host owns exporter configuration, routing, retention, access control, and backend interpretation.

### Dynamic Liability Handshake

The Dynamic Liability Handshake is an acknowledgment workflow. It does not create legal protection by itself and should not be described as legal non-repudiation or compliance certification.

Use it when the host wants to pause before a risky operation and record whether the actor accepted a required acknowledgment statement.

### Capability grant

A capability grant is a scoped permission that travels with or follows a governance decision. It should be bounded by operation, actor, scope, expiration, and host-owned validation rules.

Use this when an operation needs more than a simple allow/deny result before an execution gateway accepts it.

## Package adoption map

| Need | Start with |
| --- | --- |
| Framework-neutral policy decisions and audit primitives | `AsiBackbone.Core` |
| ASP.NET Core request correlation, result mapping, endpoint metadata, or middleware seams | `AsiBackbone.AspNetCore` |
| Local sample or test audit storage | `AsiBackbone.Storage.InMemory` |
| Host-owned database persistence with EF Core model helpers | `AsiBackbone.EntityFrameworkCore` |
| Provider-neutral governance emission projected through .NET diagnostics | `AsiBackbone.OpenTelemetry` |
| Local development signing proof paths | `AsiBackbone.Signing.LocalDevelopment` |
| Managed-key signing adapter boundary | `AsiBackbone.Signing.ManagedKey` |
| Static-analysis guidance during development | `AsiBackbone.Analyzers` |

Basic Core or ASP.NET Core adoption does not require EF Core, OpenTelemetry, signing, managed keys, outbox draining, or capability grants. Those are later layers for hosts that need stronger persistence, observability, signing, or execution-boundary behavior.

## Safe wording checklist

When introducing AsiBackbone to a team, use precise wording:

| Say | Avoid saying |
| --- | --- |
| Governance spine | Artificial superintelligence implementation |
| Decision-flow layer | Autonomous decision-maker |
| Audit residue / decision receipt | Legal non-repudiation by default |
| Signing-ready or signed by configured provider | Tamper-proof without storage/key controls |
| Host-owned persistence | Built-in production audit database |
| Optional governance emission | Mandatory cloud telemetry pipeline |
| Capability-scoped execution boundary | General-purpose command authority |

## Next steps

- Use [First 15 Minutes: Standard API Gating](quickstart-api-gating.md) for the shortest practical ASP.NET Core endpoint path.
- Use [1.0.0 Quickstart](quickstart-100.md) for the smallest package-consumer setup.
- Use [Policy Evaluator Pipeline](policy-evaluator-pipeline.md) when you are ready to understand decision composition.
- Use [ASP.NET Core Endpoint Governance](aspnetcore-endpoint-governance.md) for route metadata, controller attributes, middleware behavior, and fail-closed endpoint configuration.
- Use [Core Domain Language](core-domain-language.md) when you need the precise project vocabulary.
- Use [Glossary](glossary.md) for broader reference definitions.
