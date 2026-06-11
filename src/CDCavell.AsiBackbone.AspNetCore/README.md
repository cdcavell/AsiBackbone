# CDCavell.AsiBackbone.AspNetCore

ASP.NET Core host integration scaffold for ASI Backbone governance primitives.

This package is intended to act as a thin web-host adapter around `CDCavell.AsiBackbone.Core`.

> [!IMPORTANT]
> This package provides thin host adapters only. It does not currently provide concrete middleware, endpoint mapping, authentication integration, policy enforcement, persistence, or execution-gateway behavior. HTTP result mapping and acknowledgment challenge helpers are explicit adapters and do not enforce decisions automatically.

## Service registration

Register the ASP.NET Core integration package from a plain ASP.NET Core host through `IServiceCollection`.

```csharp
using CDCavell.AsiBackbone.AspNetCore.DependencyInjection;

builder.Services.AddAsiBackboneAspNetCore();
```

Host applications may configure the first integration options explicitly.

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

The registration is intentionally narrow. It does not register persistence, EF Core, authentication handlers, MVC, Razor Pages, Minimal API endpoints, middleware, policy evaluators, or host-specific authorization behavior.

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

## Current boundary

This package may eventually provide:

- ASP.NET Core service registration extensions;
- request-aware policy context building seams;
- current-user/current-actor resolution from `HttpContext`;
- optional middleware for request context preparation;
- optional endpoint mapping helpers;
- optional HTTP and Problem Details mapping helpers.

This package should avoid:

- Entity Framework Core persistence;
- database provider assumptions;
- direct dependencies on NetCoreApplicationTemplate;
- authentication-provider assumptions;
- robotics or physical execution dependencies;
- AI model hosting, training, inference, or orchestration.

See `docs/articles/aspnetcore-integration-boundary.md` for the intended design boundary.
