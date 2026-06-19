# CDCavell.AsiBackbone.Analyzers

Roslyn analyzer safety rails for Accountable Systems Infrastructure governance flows.

> **New to AsiBackbone?** Start with the concept, not this package: [Intent to Execution: An Accountability Pattern](https://cdcavell.github.io/AsiBackbone/articles/intent-to-execution-pattern.html) and the [documentation site](https://cdcavell.github.io/AsiBackbone/). This README covers one package in the family.

> **Important:**
> This package does not add runtime enforcement, persistence, transactions, audit storage, outbox delivery, legal protection, or compliance certification.

## ASIB001 - Persist or continue AsiBackbone governance artifact

`ASIB001` warns when an AsiBackbone governance artifact is created or returned and then discarded.

Examples include discarded governance decisions, audit residue, outbox entries, capability grants, handshake acknowledgments, and governance emission results.

```csharp
GovernanceDecision.Allow(); // ASIB001
_ = GovernanceDecision.Allow(); // ASIB001
```

Persist the artifact, pass it to a durable audit/outbox path, return it to the caller, or route it into a host-owned continuation before execution.

```csharp
GovernanceDecision decision = GovernanceDecision.Allow();
return decision;
```

## Suppression

Prefer fixing the flow. When a host application owns a custom persistence or outbox abstraction the analyzer cannot see, use one of the normal Roslyn suppression mechanisms:

- `#pragma warning disable ASIB001`
- `.editorconfig` severity configuration
- `SuppressMessageAttribute`

The analyzer also honors a host-defined marker attribute named `AsiBackbonePersistenceHandledAttribute` on the containing method or type. The package does not ship that marker as a runtime contract; hosts that want this pattern may define it in their own application or shared infrastructure project.

```csharp
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
internal sealed class AsiBackbonePersistenceHandledAttribute : Attribute;
```

## Non-goals

- Compile-time proof of transactional correctness.
- Replacement for durable audit storage, outbox persistence, transaction tests, or integration tests.
- Runtime enforcement or execution gating.
- EF Core, ASP.NET Core, cloud, robotics, or provider-specific dependencies.
