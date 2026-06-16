# ASP.NET Core Endpoint Governance

`CDCavell.AsiBackbone.AspNetCore` includes an optional ergonomic endpoint-governance layer for the common ASP.NET Core case where a host wants to protect a Minimal API endpoint or controller action with AsiBackbone metadata.

This layer is intentionally a host adapter. It does not replace Core primitives, make persistence durable, create transactions, or certify audit immutability. It reduces endpoint boilerplate while keeping storage, transaction boundaries, capability-grant validation, and outbox behavior under host ownership.

## Register services and middleware

```csharp
builder.Services.AddAsiBackboneAspNetCore();

WebApplication app = builder.Build();

app.UseAsiBackboneEndpointGovernance();
```

Place `UseAsiBackboneEndpointGovernance()` after routing has selected an endpoint and before the protected endpoint executes. In a typical minimal host this means before `MapControllers()` and before mapped endpoints are executed by endpoint routing.

Hosts that use policy metadata should register an `IAsiBackbonePolicyEvaluator<AsiBackboneConstraintEvaluationContext>`. Hosts that use capability metadata should register an `IAsiBackboneEndpointCapabilityGrantValidator`. Hosts that request audit emission should register a host-owned `IAsiBackboneAuditSink`.

## Minimal API / fluent endpoint path

```csharp
app.MapPost("/high-risk-action", handler)
    .RequireGovernancePolicy<MyStrictPolicy>()
    .RequireLiabilityHandshake()
    .RequireCapabilityGrant("robotics.execute")
    .EmitGovernanceAudit();
```

The fluent methods add endpoint metadata. The middleware resolves that metadata into an `AsiBackboneEndpointGovernanceDescriptor`, builds a framework-neutral evaluation context, and delegates policy evaluation, capability validation, handshake creation, and audit emission to registered host-owned services.

## Controller/action attribute path

```csharp
[RequireGovernancePolicy(typeof(MyStrictPolicy))]
[RequireLiabilityHandshake]
[RequireCapabilityGrant("robotics.execute")]
[EmitGovernanceAudit]
public IActionResult ExecuteHighRiskAction()
{
    return Ok();
}
```

The attribute model is designed to feel familiar to ASP.NET Core developers who already use attributes such as `[Authorize]`. The attributes only add metadata. They do not by themselves validate capability grants, write durable audit records, or provide transaction guarantees.

## What the ergonomic layer does

When endpoint governance metadata is present, the middleware can:

1. Resolve the selected endpoint metadata.
2. Build a safe `AsiBackboneConstraintEvaluationContext` using HTTP request correlation data.
3. Invoke the host-registered policy evaluator when policy metadata exists.
4. Invoke the host-registered capability validator when capability scopes exist.
5. Emit `AuditResidue` through the host-owned audit sink when audit emission is requested.
6. Return an acknowledgment challenge when the governance decision requires acknowledgment and the endpoint requested liability-handshake support.
7. Block execution with a safe HTTP result when policy, capability, or configuration checks fail closed.

## Host-owned boundaries

The ergonomic endpoint layer deliberately does not own persistence. Durable audit storage, outbox persistence, transactional consistency, signing, key management, and replay protection remain host responsibilities.

| Concern | Owner |
| --- | --- |
| Endpoint metadata and middleware orchestration | `CDCavell.AsiBackbone.AspNetCore` |
| Policy constraints and decision policy | Host/Core evaluator registration |
| Capability-grant source, proof validation, and replay handling | Host-owned `IAsiBackboneEndpointCapabilityGrantValidator` |
| Audit sink, ledger store, outbox store, and transactions | Host-owned storage/integration layer |
| Legal/compliance interpretation | Host governance process |

## Failure behavior

`AsiBackboneEndpointGovernanceOptions` controls fail-closed behavior for missing host services:

```csharp
builder.Services.Configure<AsiBackboneEndpointGovernanceOptions>(options =>
{
    options.FailClosedWhenPolicyEvaluatorMissing = true;
    options.FailClosedWhenCapabilityValidatorMissing = true;
    options.FailClosedWhenAuditSinkMissing = true;
});
```

Failing closed is the default because an endpoint that declares governance intent should not silently bypass missing policy, capability, or audit services. Advanced hosts can relax this behavior during migration, but should document why.

## Relation to full manual wire-up

Manual wire-up remains the most explicit path for complex flows. Use manual integration when the endpoint requires a custom transaction boundary, multiple persistence stores, custom signing, outbox enqueue-before-execution semantics, or workflow-specific acknowledgment handling.

Use the ergonomic layer when the endpoint follows the common pattern: read metadata, evaluate policy, validate capability, optionally emit audit residue, and either continue or return a safe governance response.
