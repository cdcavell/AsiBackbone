# Dynamic Liability Handshake

The **Dynamic Liability Handshake** is the broader conceptual name for AsiBackbone's acknowledgment and responsibility-handshake workflow.

In the implementation, the language is intentionally grounded. Core types use terms such as `LiabilityHandshakeRequest`, `LiabilityHandshakeAcknowledgment`, acknowledgment, responsibility handshake, risk level, reason code, policy version, policy hash, and correlation metadata.

> [!IMPORTANT]
> The handshake records acknowledgment context. It does not create legal protection, legal non-repudiation, compliance certification, production tamper-evidence, or a substitute for organizational/legal review by itself.

## Purpose

Some software actions should not move directly from request to execution.

A consequential action may need a deliberate pause where the requesting actor is shown the reason for the pause, the required acknowledgment text, and the risk or responsibility context before the host proceeds.

The handshake is that pause point.

## Typical flow

```text
Governance decision requires acknowledgment
  -> Host creates handshake request
  -> Host presents required acknowledgment text/code
  -> Actor accepts or rejects
  -> Host records acknowledgment response
  -> Host links acknowledgment to audit residue and lifecycle events
  -> Host decides whether execution may continue
```

The host owns presentation, authentication, authorization, storage, execution behavior, and policy for what an accepted or rejected acknowledgment means.

## Request contents

A handshake request can carry:

- stable handshake ID;
- actor ID, actor type, and display name;
- operation name;
- reason code;
- human-readable message;
- required acknowledgment code;
- required acknowledgment text;
- risk level;
- risk category;
- correlation ID;
- trace ID;
- policy version;
- policy hash;
- schema version;
- host-provided metadata.

This makes the request explainable and auditable without requiring a specific web framework, storage provider, or UI.

## Acknowledgment contents

An acknowledgment response can carry:

- stable acknowledgment ID;
- handshake ID;
- actor ID, actor type, and display name;
- acknowledgment code;
- accepted/rejected result;
- UTC timestamp;
- correlation ID;
- trace ID;
- schema version;
- host-provided metadata.

The acknowledgment is not the execution itself. It is the recorded actor response to the responsibility checkpoint.

## Relationship to governance decisions

A governance decision can produce `AcknowledgmentRequired` when constraints and decision policy determine that immediate execution should not proceed without actor confirmation.

The handshake then lets the host produce a separate record showing that the actor was presented the required acknowledgment and either accepted or rejected it.

## Relationship to audit residue

The handshake should be linked to audit residue and lifecycle events whenever the host preserves durable accountability records.

Recommended lifecycle mapping:

```text
DecisionEvaluated
  -> AcknowledgmentRequested
  -> AcknowledgmentCompleted
  -> CapabilityTokenIssued, if applicable
  -> GatewayExecutionStarted, if applicable
  -> GatewayExecutionCompleted or GatewayExecutionDenied
  -> ExternalEmissionQueued, if applicable
```

This lets reviewers follow the decision path without rewriting the original audit record.

## Safe wording

Use wording such as:

- "The actor acknowledged the required responsibility statement."
- "The host recorded the acknowledgment response."
- "The acknowledgment is linked to audit residue and lifecycle metadata."
- "The host remains responsible for deciding whether acknowledgment permits execution."

Avoid wording such as:

- "The user accepted all legal liability."
- "The handshake guarantees compliance."
- "The record is legally non-repudiable."
- "The handshake makes the audit trail tamper-proof."

## Related articles

- [Core Domain Language](core-domain-language.md)
- [Policy Evaluator Pipeline](policy-evaluator-pipeline.md)
- [ASP.NET Core Integration Boundary](aspnetcore-integration-boundary.md)
- [ASP.NET Core Endpoint Governance](aspnetcore-endpoint-governance.md)
- [Signed Audit and Outbox Records](signed-audit-and-outbox-records.md)
