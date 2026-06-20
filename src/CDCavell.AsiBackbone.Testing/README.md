# CDCavell.AsiBackbone.Testing

`CDCavell.AsiBackbone.Testing` provides test harness helpers for exercising AsiBackbone-governed endpoints and services without forcing host applications to wire production persistence, signing, outbox, audit, or capability-grant infrastructure in every automated test.

> [!IMPORTANT]
> This package is for tests only. It is not a production enforcement provider and should not be used to weaken host-owned governance, persistence, signing, audit, or capability validation in production applications.

## What it registers

`AddAsiBackboneTestHarness(...)` registers deterministic test-only substitutions for common host-owned seams:

- `IAsiBackbonePolicyEvaluator<AsiBackboneConstraintEvaluationContext>`
- `IAsiBackboneEndpointCapabilityGrantValidator`
- `IAsiBackboneAuditSink` backed by `AsiBackboneTestAuditSink`
- `IAsiBackboneGovernanceOutboxStore` backed by the non-durable in-memory outbox store
- `IAsiBackboneSigningService` backed by a deterministic no-signature service

The package does not change production package defaults. ASP.NET Core endpoint governance remains fail-closed when host-owned services are missing unless tests explicitly register this harness.

## Basic endpoint test setup

```csharp
using CDCavell.AsiBackbone.AspNetCore.DependencyInjection;
using CDCavell.AsiBackbone.Testing;

builder.Services
    .AddAsiBackboneAspNetCore()
    .AddAsiBackboneTestHarness(harness =>
    {
        harness.AllowAllPolicies();
        harness.AllowCapabilityGrants();
    });
```

## Deterministic policy results

```csharp
builder.Services.AddAsiBackboneTestHarness(harness =>
{
    harness.AllowAllPolicies();
    harness.SetPolicyResult<MyStrictPolicy>(
        GovernanceDecision.Deny("test.denied", "Denied by test harness."));
});
```

For stricter tests, require every policy marker to have an explicit result:

```csharp
builder.Services.AddAsiBackboneTestHarness(harness =>
{
    harness.RequirePolicyResult<MyStrictPolicy>(GovernanceDecision.Allow());
});
```

When `RequirePolicyResult<TPolicy>(...)` is used and the selected endpoint has an unconfigured policy marker, the harness returns a deterministic denied decision with reason code `test_harness.policy_result.missing`.

## Inspecting audit residue

```csharp
AsiBackboneTestAuditSink auditSink = services.GetRequiredService<AsiBackboneTestAuditSink>();

Assert.Single(auditSink.Entries);
```

The audit sink is in-memory and process-local. It is intended for assertion-friendly automated tests, not durable records or tamper-evidence.

## WebApplicationFactory-style usage

```csharp
factory.WithWebHostBuilder(builder =>
{
    builder.ConfigureServices(services =>
    {
        services.AddAsiBackboneTestHarness(harness =>
        {
            harness.AllowAllPolicies();
            harness.AllowCapabilityGrants();
        });
    });
});
```

Use this pattern when a host application already calls `AddAsiBackboneAspNetCore()` and exposes endpoints with metadata such as `.RequireGovernancePolicy<TPolicy>()`, `.RequireCapabilityGrant(...)`, or `.EmitGovernanceAudit()`.

## Production boundary

This package is intentionally scoped to automated tests, samples, and local developer validation. Production applications should register real policy evaluators, capability validators, audit sinks, durable outbox stores, signing providers, and storage providers appropriate to their risk profile.
