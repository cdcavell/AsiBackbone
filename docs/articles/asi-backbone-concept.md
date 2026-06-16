# ASI Backbone Concept Synopsis

In this software project, **ASI** means **Accountable Systems Infrastructure**.

AsiBackbone is a .NET governance spine for consequential software actions. It is inspired by broader Eden/Backbone governance ideas, but the repository implements practical software infrastructure: policy evaluation, decision results, acknowledgment workflows, audit residue, durable outbox persistence, optional provider emission, capability-scoped execution, signing-ready metadata, and host integration.

> [!IMPORTANT]
> AsiBackbone does not implement artificial superintelligence, host AI models, train AI models, control robots, certify compliance, or prove the Eden/Backbone framework.

## The practical concept

AsiBackbone sits at the boundary between proposed intent and host-owned execution.

A host application may receive a request, AI-agent proposal, administrative operation, workflow action, API call, infrastructure change, or external gateway command. Before the host executes that action, AsiBackbone helps the host ask:

- Which actor or system requested this?
- Which regional, local, organizational, risk, or operational constraints apply?
- Which governance outcome is appropriate?
- Does the actor need to acknowledge responsibility or risk first?
- Which policy version and policy hash shaped the decision?
- Should follow-on execution be scoped through a short-lived capability grant?
- What audit residue and lifecycle events should be preserved?
- Should a minimized governance envelope be emitted to an observability or governance provider?

The result is not an autonomous intelligence. It is accountable decision infrastructure.

## Core flow

```text
Intent or request
  -> Actor and policy context
  -> Constraint evaluation
  -> Governance decision
  -> Required acknowledgment when applicable
  -> Audit residue and lifecycle event
  -> Optional capability grant
  -> Durable local audit/outbox persistence
  -> Optional provider emission
  -> Host-owned execution or rejection
```

The host remains responsible for authentication, authorization, persistence registration, endpoint exposure, UI rendering, exporter configuration, legal/compliance review, and operational execution.

## Supported decision outcomes

AsiBackbone narrows a proposed action into a framework-neutral governance decision:

- `Allowed`
- `Warning`
- `Denied`
- `Deferred`
- `AcknowledgmentRequired`
- `EscalationRecommended`

These outcomes are the software expression of controlled collapse: open intent becomes an explainable decision under active policy structure.

## Relationship to Eden/Backbone language

The broader Eden/Backbone framework uses collapse language to describe how open possibility becomes realized form under constraints.

AsiBackbone maps that idea into software without making physical, metaphysical, or artificial-superintelligence claims:

| Broader concept | Software implementation meaning |
| --- | --- |
| Open possibility | Proposed intent or request |
| Active structure | Policies, constraints, actor context, risk context, and gateway limits |
| Collapse boundary | Governance decision before execution |
| Residue | Audit record, lifecycle event, outbox entry, or emission envelope |
| Return/review path | Defer, escalate, require acknowledgment, retry, dead-letter, or review |

This keeps the conceptual inspiration useful while preserving the software boundary.

## Current package areas

The first stable package family centers on Core, in-memory storage, EF Core persistence, and ASP.NET Core integration. Current source also includes analyzer, OpenTelemetry, and signing-provider package projects for the `1.x` line.

Package-specific documentation and release notes define what is stable, optional, local-only, or future-facing.

## Safe wording

Use wording such as:

- "Accountable Systems Infrastructure"
- "governance spine"
- "policy-controlled decision flow"
- "host-owned execution"
- "audit residue and lifecycle events"
- "durable local/outbox persistence before optional provider emission"
- "signing-ready or provider-signed artifacts, depending on the deployed package and host configuration"

Avoid wording such as:

- "AsiBackbone implements artificial superintelligence"
- "AsiBackbone controls robots"
- "AsiBackbone guarantees compliance"
- "AsiBackbone is tamper-evident by default"
- "AsiBackbone proves the Eden Hypothesis"

## Related articles

- [Core Domain Language](core-domain-language.md)
- [Policy Evaluator Pipeline](policy-evaluator-pipeline.md)
- [Equations and Toy Models](equations-and-toy-models.md)
- [Dynamic Liability Handshake](dynamic-liability-handshake.md)
- [Gateway and Regional Policy Flow](gateway-and-regional-policy-flow.md)
