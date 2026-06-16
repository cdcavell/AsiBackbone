# Capability Grant Hardening

Issue: #225.

This article documents provider-neutral capability grant validation, proof handling, and bounded-use checks for AsiBackbone.

In this software project, **ASI** means **Accountable Systems Infrastructure**. AsiBackbone can model short-lived, scoped grants for governed execution, but it does not replace host authentication, host authorization, resource authorization, or external execution controls.

> [!IMPORTANT]
> A capability grant is not broad authority. It should be short-lived, scoped, bound to policy and acknowledgment context when needed, and checked at the execution boundary before any consequential action proceeds.

## Grant metadata

`CapabilityTokenGrant` models the metadata a host can protect, persist, and validate:

| Field | Purpose |
| --- | --- |
| Token ID | Stable grant identifier for validation and bounded-use checks. |
| Issuer | Host or service that created the grant. |
| Audience | Intended execution gateway, service, or host boundary. |
| Scopes | Least-privilege actions allowed by the grant. |
| Issued UTC | Timestamp when the grant was created. |
| Not-before UTC | Optional timestamp before which the grant is not valid. |
| Expires UTC | Timestamp after which the grant is no longer valid. |
| Policy version/hash | Binds the grant to the policy context that produced it. |
| Acknowledgment/handshake reference | Binds follow-on execution to the approval or acknowledgment flow that authorized it. |
| Gateway/resource binding | Limits the grant to a specific gateway or target resource when supplied. |

The grant model is not a wire format. Hosts decide whether they serialize it as JSON, wrap it in a signed envelope, store it server-side, or project it into another provider-owned format.

## Validation at the execution boundary

Use `CapabilityGrantValidator.ValidateAsync(...)` before follow-on execution. Validation can check:

- proof when configured through the existing signing verification seam;
- issuer and audience;
- expiration and not-before time;
- required scopes;
- policy version and policy hash;
- acknowledgment and handshake references;
- gateway and resource bindings;
- bounded-use state through a host-owned use store.

Example:

```csharp
CapabilityGrantValidationResult result = await CapabilityGrantValidator.ValidateAsync(
    signedGrant,
    CapabilityGrantValidationOptions.Create(
        issuer: "policy-engine",
        audience: "robotics-gateway",
        scopes: ["robotics.execute"],
        policyVersion: "policy-v1",
        policyHash: "policy-hash",
        acknowledgmentId: "ack-123",
        requireProof: true,
        requireAcknowledgmentReference: true,
        requireUseCheck: true,
        maxUseCount: 1),
    verificationService,
    useStore,
    cancellationToken);
```

Proceed only when `result.ShouldAllow` is true.

## Failure behavior

Capability grant validation maps failures to host-facing actions from the verification policy model.

| Failure | Category | Default action |
| --- | --- | --- |
| Missing proof when required | `MissingProof` | `Deny` |
| Invalid proof | `InvalidProof` | `Deny` |
| Wrong issuer or audience | `WrongIssuer`, `WrongAudience` | `Deny` |
| Expired grant | `Expired` | `Deny` |
| Not yet valid | `NotYetValid` | `Defer` |
| Required scope missing | `WrongScope` | `Deny` |
| Policy mismatch | `PolicyMismatch` | `Deny` |
| Missing acknowledgment reference | `MissingAcknowledgmentReference` | `RequireAcknowledgment` |
| Acknowledgment or handshake mismatch | `AcknowledgmentMismatch`, `HandshakeMismatch` | `Deny` |
| Gateway or resource mismatch | `GatewayMismatch`, `ResourceMismatch` | `Deny` |
| Use limit exceeded | `ReuseLimitExceeded` | `Deny` |
| Grant stopped or cancelled | `Revoked`, `Cancelled` | `Deny` |
| Use store unavailable | `ReplayStoreUnavailable` | `Defer` |

High-risk workflows should not fall back to broad authority when validation fails.

## Bounded-use expectations

`ICapabilityGrantUseStore` is the provider-neutral seam for single-use or bounded-use workflows. Hosts own the implementation because durable state, concurrency control, distributed locks, cache consistency, retention, and storage schema are deployment-specific.

Recommended use-store behavior:

```text
Validate metadata and proof
  -> check use state using grant ID
  -> atomically consume one use when accepted
  -> return use-limit, stopped, cancelled, or unavailable state when not accepted
```

For high-risk workflows, use checks should be atomic at the host storage boundary. An in-memory store is acceptable for tests and samples only; production use needs durable, concurrency-safe state.

## Core boundary

Core provides metadata, validation result categories, bounded-use interfaces, and signing verification integration. Core does not provide:

- a bearer-token format;
- host authentication or authorization;
- automatic proof issuance;
- durable replay storage;
- distributed locking;
- external system execution;
- legal or compliance guarantees.

Use safe wording such as "the grant was validated for this execution context." Avoid wording such as "the grant replaces authorization" or "single-use is guaranteed" unless the host store provides that guarantee under documented assumptions.
