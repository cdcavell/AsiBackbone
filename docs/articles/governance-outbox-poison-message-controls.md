# Governance Outbox Poison-Message Controls

This article documents the provider-neutral maximum retry and dead-letter controls used by the AsiBackbone governance outbox drain.

In this software project, **ASI** means **Accountable Systems Infrastructure**. These controls provide deterministic quarantine behavior for repeatedly failing governance emissions. They do not replace host-owned monitoring, incident response, legal review, provider configuration, or replay authorization.

## Configuration

`AsiBackboneGovernanceOutboxOptions` exposes the following controls:

| Option | Default | Purpose |
| --- | --- | --- |
| `MaxRetryAttempts` | `5` | Maximum failed emission attempts permitted before the drain applies its poison-message policy. The failure currently being processed counts toward the threshold. |
| `DeadLetterOnMaxRetryAttempts` | `true` | Dead-letters the entry when the configured threshold is reached. When disabled, the entry-level `MaxRetryCount` policy remains responsible for terminal handling. |
| `DeadLetterReasonCode` | `outbox.max_retry_attempts_exceeded` | Stable provider-neutral error code recorded on threshold dead-lettering. |
| `DeadLetterReasonMessage` | `Governance outbox entry exceeded the configured maximum retry attempts.` | Stable provider-neutral diagnostic and dead-letter reason. |

Example:

```csharp
services.Configure<AsiBackboneGovernanceOutboxOptions>(options =>
{
    options.MaxRetryAttempts = 8;
    options.DeadLetterOnMaxRetryAttempts = true;
    options.DeadLetterReasonCode = "outbox.max_retry_attempts_exceeded";
    options.DeadLetterReasonMessage =
        "Governance outbox entry exceeded the configured maximum retry attempts.";
});
```

`MaxRetryAttempts` must be greater than zero. Reason codes and messages must be non-empty. Values are validated when the drain resolves its options.

## Drain behavior

The same threshold policy applies to both drain modes:

- normal provider-neutral draining through `IAsiBackboneGovernanceOutboxStore`; and
- claim/lease draining through `IAsiBackboneGovernanceOutboxClaimStore`.

A successful emission is marked delivered as usual. Provider-returned terminal dead-letter results remain terminal. Pending and deferred results remain deferred and do not consume the failed-attempt threshold because they do not increment `RetryCount`.

For failed results and unexpected emitter exceptions, the drain evaluates the next failed attempt before persisting the transition:

```text
next failed attempt < MaxRetryAttempts
    -> failed or retryable-failure
    -> schedule the next retry when applicable

next failed attempt >= MaxRetryAttempts
and DeadLetterOnMaxRetryAttempts = true
    -> dead-lettered
    -> stable provider-neutral reason code/message
    -> no next retry timestamp
```

The threshold is deliberately enforced in the drain rather than delegated to a provider SDK. This keeps poison-message behavior consistent across emitters and across claim and non-claim stores.

## Entry-level and drain-level limits

`GovernanceOutboxEntry` also carries `MaxRetryCount`. The two controls have different ownership:

- `MaxRetryAttempts` is the host-configured drain policy applied consistently to every attempted emission handled by that drain.
- `MaxRetryCount` is persisted with an individual outbox entry and remains a storage/domain safety boundary.

When drain-level dead-lettering is enabled, the earlier applicable threshold wins. Hosts should configure these values intentionally and document which limit is authoritative for their deployment.

Disabling `DeadLetterOnMaxRetryAttempts` does not create unlimited retries by itself. The persisted entry-level limit may still dead-letter the entry. A custom store or replay workflow must not silently reset retry history without an explicit, audited remediation decision.

## Alerting responsibilities

AsiBackbone records a stable terminal state; the host must make that state operationally visible. Hosts should:

- emit a counter or event whenever an entry transitions to dead-lettered;
- alert on increases in dead-letter count, especially repeated reason codes or provider paths;
- alert before the threshold when retry counts approach `MaxRetryAttempts`;
- correlate the incident with outbox entry ID, correlation ID, audit residue ID, provider, region, tenant, and workload using minimized metadata;
- avoid placing raw prompts, protected content, credentials, access tokens, or secrets in alerts.

A dead-letter transition should normally open an incident or review item for regulated or consequential workloads. It should never be treated as successful delivery.

## Incident review and manual remediation

A host runbook should require operators to:

1. Confirm that the local durable store is healthy and the terminal transition was persisted.
2. Identify whether the repeated failure came from provider unavailability, malformed data, schema drift, authorization, classification policy, credentials, throttling, or application defects.
3. Inspect only minimized provider-neutral error details and protected diagnostics available under the host's access-control policy.
4. Correct the underlying cause before replay.
5. Record the reviewer, reason, approval, remediation action, affected time window, and related incident or change ticket.
6. Verify downstream delivery after replay and retain the original dead-letter history where required.

Dead-lettered governance emissions are reviewable incidents, not discarded telemetry.

## Replay guidance

AsiBackbone does not automatically replay dead-lettered entries. Replay is host-owned because a terminal record may represent an unsafe payload, invalid authorization, prohibited classification, schema incompatibility, or permanent provider rejection.

A replay implementation should:

- require explicit authorization;
- create an auditable remediation record;
- preserve or reference the original outbox entry and terminal reason;
- avoid resetting retry counters without recording why;
- issue a new claim or execution token where claim leases are used;
- replay in controlled batches to avoid renewed provider throttling;
- verify provider-side receipt before closing the incident.

## Claim-lease considerations

When claim leases are enabled, threshold dead-lettering uses the claim-aware store transition. The terminal transition must clear claim ownership so a dead-lettered row cannot remain stranded under an expired or misleading worker lease.

Claim-capable stores remain responsible for atomic claim validation and persistence. A stale worker must not overwrite a terminal transition made by the current claim owner.

## Compliance boundary

These controls improve resilience and auditability by converting repeated failures into deterministic, queryable terminal records. They do not certify compliance, guarantee external delivery, prove immutable storage, or determine whether replay is legally or operationally appropriate.

The host remains responsible for:

- durable storage and retention;
- monitoring and alert rules;
- access control and data minimization;
- incident ownership and escalation;
- manual review and replay authorization;
- provider-side validation and reconciliation;
- documenting selected thresholds and exceptions.

## Related documentation

- [Outbox Drain Reliability and Alerting](outbox-drain-reliability-and-alerting.md)
- [Hosted Governance Outbox Drain](hosted-governance-outbox-drain.md)
- [Outbox Claim/Lease Design](outbox-claim-lease-design.md)
- [Governance Outbox Delivery Semantics](governance-outbox-delivery-semantics.md)
- [Durable Audit and Outbox Persistence](durable-audit-outbox-persistence.md)
