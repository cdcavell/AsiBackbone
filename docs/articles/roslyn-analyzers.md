# Roslyn Analyzers

`CDCavell.AsiBackbone.Analyzers` provides compile-time safety rails for host applications that use AsiBackbone governance primitives.

The analyzer package is intentionally separate from `CDCavell.AsiBackbone.Core`. Core remains framework-neutral and does not depend on Roslyn, ASP.NET Core, EF Core, observability providers, or storage providers.

## Intent

AsiBackbone keeps persistence, transaction boundaries, outbox behavior, and execution ownership in the host application. That boundary is important, but it also means a host can accidentally create a governance artifact and then continue without preserving audit residue, durable outbox state, acknowledgment evidence, capability-grant checks, or a safe continuation path.

The analyzer package helps catch simple, recognizable mistakes at build time. It is developer ergonomics, not proof of correctness.

## Rule IDs

| Rule | Default severity | Summary |
| --- | --- | --- |
| `ASIB001` | Warning | Governance artifact created or returned and then discarded. |

Future rules may expand this set for audit residue, capability-grant validation, acknowledgment checks, and high-risk endpoint persistence boundaries.

## ASIB001 - Persist or continue AsiBackbone governance artifact

`ASIB001` warns when a recognized governance artifact is created or returned and then discarded.

Examples of recognized artifacts include:

- `GovernanceDecision`
- `AuditResidue`
- `AuditLedgerRecord`
- `GovernanceOutboxEntry`
- `GovernanceEmissionEnvelope`
- `GovernanceEmissionResult`
- `CapabilityTokenGrant`
- `CapabilityGrantValidationResult`
- `CapabilityGrantUseResult`
- `LiabilityHandshakeAcknowledgment`

### Diagnostic examples

```csharp
GovernanceDecision.Allow();
```

```csharp
_ = GovernanceDecision.Allow();
```

Both examples create a governance decision and discard it. The analyzer cannot see durable audit persistence, outbox enqueue, a return path, or another host-owned continuation.

### Safer examples

```csharp
GovernanceDecision decision = GovernanceDecision.Allow();
return decision;
```

```csharp
GovernanceDecision decision = GovernanceDecision.Allow();
await auditLedger.AppendAsync(record, cancellationToken);
return decision;
```

The analyzer intentionally starts conservatively. It does not try to prove that every stored or returned artifact is durably persisted. Runtime tests, transaction tests, and integration tests still matter.

## Suppression and custom host-owned persistence

Prefer fixing the flow. When a custom host architecture already handles persistence or safe continuation in a way the analyzer cannot recognize, use normal Roslyn suppression tools:

```csharp
#pragma warning disable ASIB001
GovernanceDecision.Allow();
#pragma warning restore ASIB001
```

Hosts may also configure severity in `.editorconfig`:

```ini
dotnet_diagnostic.ASIB001.severity = warning
```

or disable it for a specific project:

```ini
dotnet_diagnostic.ASIB001.severity = none
```

For custom persistence wrappers, the analyzer also honors a host-defined marker attribute named `AsiBackbonePersistenceHandledAttribute` on the containing method or type.

```csharp
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
internal sealed class AsiBackbonePersistenceHandledAttribute : Attribute;

[AsiBackbonePersistenceHandled]
public async Task ExecuteThroughCustomPipelineAsync()
{
    GovernanceDecision.Allow();
    await customGovernancePipeline.FlushAsync();
}
```

The marker is name-based and host-owned. It is not a runtime contract shipped by Core.

## Non-goals

The analyzer package does not:

- make Core depend on Roslyn;
- require EF Core, ASP.NET Core, or any cloud provider;
- prove transactional correctness;
- replace durable audit storage, outbox persistence, transaction tests, or integration tests;
- block builds by default unless the host chooses to treat analyzer warnings as errors;
- certify compliance or legal adequacy.

Use the analyzers as early-warning rails around common persistence and continuation mistakes, not as the only safety mechanism.
