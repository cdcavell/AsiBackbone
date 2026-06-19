# CDCavell.AsiBackbone.AspNetCore

ASP.NET Core host adapters for Accountable Systems Infrastructure governance primitives.

This package acts as a thin web-host adapter around `CDCavell.AsiBackbone.Core`.

> **New to AsiBackbone?** Start with the concept, not this package: [Intent to Execution: An Accountability Pattern](https://cdcavell.github.io/AsiBackbone/articles/intent-to-execution-pattern.html) and the [documentation site](https://cdcavell.github.io/AsiBackbone/). This README covers one package in the family.

> **Important:**
> This package provides host adapters only. Low-level helpers do not enforce decisions automatically; endpoint-governance middleware acts only when the host explicitly adds it to the pipeline and registers the required host-owned policy, capability, audit, persistence, and transaction services. Attributes and route-builder calls do not by themselves make audit records durable, immutable, tamper-evident, or transactionally safe.

## Service registration

Register the ASP.NET Core integration package from a plain ASP.NET Core host through `IServiceCollection`.

```csharp
using CDCavell.AsiBackbone.AspNetCore.DependencyInjection;

builder.Services.AddAsiBackboneAspNetCore();
```

Host applications may configure the integration options explicitly.

```csharp
builder.Services.AddAsiBackboneAspNetCore(options =>
{
    options.IncludeRouteValues = true;
    options.IncludeEndpointMetadata = true;
    options.IncludeRequestMethod = true;
    options.IncludeRequestPath = false;
    options.CorrelationIdHeaderNames = ["X-Correlation-ID", "X-Request-ID"];
});
```

The base registration is intentionally narrow. It does not register persistence, EF Core, authentication handlers, MVC, Razor Pages, Minimal API endpoints, policy evaluators, capability grant validators, or host-specific authorization behavior.

## Ergonomic endpoint governance

`UseAsiBackboneEndpointGovernance()` evaluates AsiBackbone endpoint metadata before endpoint execution.

```csharp
using CDCavell.AsiBackbone.AspNetCore.Endpoints;

app.UseAsiBackboneEndpointGovernance();

app.MapPost("/high-risk-action", handler)
    .RequireGovernancePolicy<MyStrictPolicy>()
    .RequireLiabilityHandshake()
    .RequireCapabilityGrant("robotics.execute")
    .EmitGovernanceAudit();
```

Controller/action attributes are also available:

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

The metadata layer is optional and ergonomic. It does not replace full manual wire-up. Hosts that attach policy metadata should register an `IAsiBackbonePolicyEvaluator<AsiBackboneConstraintEvaluationContext>`. Hosts that attach capability metadata should register an `IAsiBackboneEndpointCapabilityGrantValidator`. Hosts that request audit emission should register a host-owned `IAsiBackboneAuditSink`.

## Hosted governance outbox drain

`AddAsiBackboneGovernanceOutboxDrainWorker` registers a host-owned background worker that runs the provider-neutral Core `AsiBackboneGovernanceOutboxDrain` through dependency injection.

```csharp
using CDCavell.AsiBackbone.AspNetCore.DependencyInjection;
using CDCavell.AsiBackbone.Core.Emissions;
using CDCavell.AsiBackbone.Core.Outbox;
using CDCavell.AsiBackbone.Storage.InMemory.Outbox;

builder.Services.AddSingleton<IAsiBackboneGovernanceOutboxStore, InMemoryGovernanceOutboxStore>();
builder.Services.AddSingleton<IAsiBackboneGovernanceEmitter>(NoOpGovernanceEmitter.Instance);

builder.Services.AddAsiBackboneGovernanceOutboxDrainWorker(options =>
{
    options.BatchSize = 25;
    options.PollingInterval = TimeSpan.FromSeconds(15);
});
```

The worker resolves the drain from a scoped service provider so host-owned durable stores can depend on scoped infrastructure such as EF Core `DbContext` instances. Production hosts should avoid duplicate active drain workers against the same durable outbox unless their store implements leasing, row claiming, partitioning, or provider-side idempotency.

## Request correlation and audit enrichment

`IAsiBackboneHttpRequestCorrelationResolver` resolves request correlation data from the current `HttpContext` without making Core depend on ASP.NET Core types.

The default resolver:

- checks configured correlation headers such as `X-Correlation-ID` and `X-Request-ID`;
- falls back to `HttpContext.TraceIdentifier` when no configured header is present;
- captures a trace identifier from `Activity.Current` or the ASP.NET Core trace identifier;
- emits safe request metadata such as method, route pattern, endpoint display name, and route values;
- excludes sensitive request data such as headers, query strings, request bodies, cookies, and tokens by default.

Example usage:

```csharp
using CDCavell.AsiBackbone.AspNetCore.Correlation;
using CDCavell.AsiBackbone.Core.Audit;

AsiBackboneHttpRequestCorrelation correlation = correlationResolver.ResolveRequestCorrelation();

AuditResidue residue = correlation.CreateAuditResidue(
    actor,
    "ApproveWidget",
    decision);
```

Use `AsiBackboneHttpRequestCorrelation.ToEvaluationContext(...)` when a web host needs to carry the resolved correlation identifier and safe request metadata into a framework-neutral Core policy evaluation context.

## HTTP result mapping

`AsiBackboneHttpResultMappingExtensions` maps Core `GovernanceDecision` and `OperationResult` instances into ASP.NET Core `IResult` responses through explicit helpers.

```csharp
using CDCavell.AsiBackbone.AspNetCore.Results;
using CDCavell.AsiBackbone.Core.Decisions;

GovernanceDecision decision = GovernanceDecision.Deny(
    "policy.denied",
    "Internal policy detail for audit only.",
    correlationId: "request-123");

return decision.ToHttpResult();
```

Default governance decision mapping:

| Core outcome | Default HTTP behavior |
| --- | --- |
| `Allowed` | `200 OK` JSON response. |
| `Warning` | `200 OK` JSON response with retained reason codes. |
| `Denied` | `403 Forbidden` Problem Details response. |
| `Deferred` | `202 Accepted` Problem Details response. |
| `AcknowledgmentRequired` | `428 Precondition Required` Problem Details response. |
| `EscalationRecommended` | `409 Conflict` Problem Details response. |

Default operation-result mapping:

| Core result | Default HTTP behavior |
| --- | --- |
| Success | `200 OK` JSON response. |
| Failure | `400 Bad Request` Problem Details response. |

Reason codes and correlation identifiers are preserved by default when available. Reason messages, trace identifiers, policy versions, and policy hashes are not exposed by default because those values may reveal sensitive policy internals or diagnostic details.

Hosts can opt into broader detail only when appropriate:

```csharp
using CDCavell.AsiBackbone.AspNetCore.Results;

AsiBackboneHttpResultMappingOptions mappingOptions = new()
{
    IncludeReasonMessages = true,
    IncludeTraceId = true,
    IncludePolicyMetadata = true,
};

return decision.ToHttpResult(mappingOptions);
```

Status-code policy remains host-overridable through `AsiBackboneHttpResultMappingOptions`. Hosts that intentionally mask denial or scanner traffic with alternate status codes should configure their own mapping rather than relying on the defaults.

## Acknowledgment challenge flow

`IAsiBackboneAcknowledgmentChallengeService` provides a host-friendly bridge for Core `AcknowledgmentRequired` decisions. It builds an `AsiBackboneAcknowledgmentChallenge` that MVC, Razor Pages, Minimal APIs, a SPA, or another UI layer can render without the package taking a dependency on that stack.

```csharp
using CDCavell.AsiBackbone.AspNetCore.Handshakes;
using CDCavell.AsiBackbone.Core.Decisions;

GovernanceDecision decision = GovernanceDecision.RequireAcknowledgment(
    "risk.high",
    "Manual acknowledgment is required before execution.",
    correlationId: "request-123");

AsiBackboneAcknowledgmentChallenge challenge = acknowledgmentChallengeService.CreateChallenge(
    actor,
    "PublishEpisode",
    decision);
```

The challenge preserves safe round-trip fields such as handshake identifier, operation name, reason code, required acknowledgment code/text, risk level, risk category, and correlation identifier. Trace identifiers and policy metadata are hidden by default and can be enabled through `AsiBackboneAcknowledgmentChallengeOptions` only when the host intentionally wants to expose those diagnostics.

Hosts can round-trip a submitted acknowledgment response back into Core handshake models:

```csharp
AsiBackboneAcknowledgmentChallengeResult result = acknowledgmentChallengeService.HandleResponse(
    challenge,
    actor,
    new AsiBackboneAcknowledgmentChallengeRequest
    {
        HandshakeId = challenge.HandshakeId,
        AcknowledgmentCode = challenge.RequiredAcknowledgmentCode,
        Acknowledged = true,
    });
```

A successful challenge result contains a Core `LiabilityHandshakeAcknowledgment`. Failed responses return an `OperationResult` with a reason code, such as a handshake mismatch or acknowledgment-code mismatch. The package does not persist challenge state; hosts decide whether to store the Core handshake request, serialize it into protected state, or associate it with an existing workflow.

## Stable boundary

This package provides:

- ASP.NET Core service registration extensions;
- configurable HTTP integration options;
- request correlation resolution;
- safe request metadata capture;
- request correlation to Core evaluation context mapping;
- audit enrichment helpers;
- HTTP and Problem Details result mapping helpers;
- acknowledgment challenge creation and response handling helpers;
- optional endpoint-governance middleware and metadata helpers;
- hosted governance outbox drain registration and scheduling options.

This package avoids:

- Entity Framework Core persistence;
- database provider assumptions;
- direct dependencies on NetCoreApplicationTemplate;
- authentication-provider assumptions;
- provider exporter dependencies;
- automatic transaction safety;
- durable persistence guarantees from attributes or endpoint metadata alone;
- robotics or physical execution dependencies;
- AI model hosting, training, inference, or orchestration.

See `docs/articles/aspnetcore-integration-boundary.md`, `docs/articles/aspnetcore-endpoint-governance.md`, and `docs/articles/hosted-governance-outbox-drain.md` for the implemented design boundary.
