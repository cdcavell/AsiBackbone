# ASP.NET Core Integration Boundary

This design note defines the intended boundary for the planned `CDCavell.AsiBackbone.AspNetCore` package before implementation begins.

The package should make AsiBackbone easier to wire into ASP.NET Core hosts without moving host ownership, persistence ownership, or domain policy ownership out of the consuming application.

## Purpose

`CDCavell.AsiBackbone.AspNetCore` should act as a thin host-integration layer around the framework-neutral Core primitives.

It should help an ASP.NET Core application:

- register AsiBackbone services through standard dependency injection;
- resolve the current HTTP actor into Core actor context primitives;
- build request-aware policy contexts;
- expose optional middleware and endpoint mapping helpers;
- connect Core decision, acknowledgment, audit, capability-token, and gateway workflows to web requests;
- remain usable in plain ASP.NET Core hosts and NetCoreApplicationTemplate-based hosts.

The package should not become the policy engine, persistence layer, application template, authentication provider, or authorization system for the host.

## Package Dependencies

The planned dependency direction is:

```text
CDCavell.AsiBackbone.AspNetCore
  -> CDCavell.AsiBackbone.Core
  -> Microsoft.AspNetCore.* abstractions needed for hosting, routing, middleware, Problem Details, and HTTP context integration
```

The ASP.NET Core package may depend on minimal ASP.NET Core abstractions needed for host integration, such as HTTP context access, routing, endpoint building, dependency injection, and middleware registration.

It should avoid direct dependencies on:

- Entity Framework Core;
- concrete database providers;
- NetCoreApplicationTemplate;
- signing/key-management implementations;
- robotics or physical execution packages;
- AI model hosting, training, inference, or orchestration libraries.

EF Core-backed storage should remain in `CDCavell.AsiBackbone.EntityFrameworkCore`. In-memory helpers should remain in `CDCavell.AsiBackbone.Storage.InMemory`. Signing and verification should remain in a later signing package.

## What Belongs in ASP.NET Core

`CDCavell.AsiBackbone.AspNetCore` may include:

- service registration extensions such as `AddAsiBackboneAspNetCore(...)`;
- options objects for HTTP integration behavior;
- current actor resolution contracts for HTTP requests;
- default claims-based actor resolution helpers;
- policy-context builder contracts that can read HTTP route, endpoint, user, header, correlation, and request metadata;
- middleware for attaching correlation and AsiBackbone request context;
- optional middleware for enforcing a previously evaluated decision when the host chooses that pattern;
- endpoint mapping helpers for acknowledgment or decision review workflows;
- Problem Details mapping helpers for deny, defer, require-acknowledgment, and escalation outcomes;
- endpoint filters or minimal API helpers when they remain optional and host-controlled.

The package should remain a web boundary adapter. It should translate ASP.NET Core request information into Core domain language and translate Core outcomes back into HTTP-friendly results.

## What Does Not Belong in ASP.NET Core

`CDCavell.AsiBackbone.AspNetCore` should avoid:

- defining the Core decision model;
- implementing durable audit storage;
- owning EF Core `DbContext` configuration or migrations;
- choosing the database provider;
- choosing the authentication provider;
- replacing ASP.NET Core authorization;
- requiring Identity, OpenID Connect, SAML, Microsoft Entra ID, Google, or any other specific provider;
- requiring NetCoreApplicationTemplate;
- hiding host security policy in package defaults;
- executing external or robotic commands directly;
- performing AI model inference or orchestration.

The host must remain responsible for its authentication scheme, authorization policies, routing model, database lifecycle, policy definitions, and operational execution boundaries.

## Service Registration Pattern

The primary integration surface should be explicit dependency injection extension methods.

A future registration shape may look like:

```csharp
builder.Services.AddAsiBackboneCore();
builder.Services.AddAsiBackboneAspNetCore(options =>
{
    options.ActorResolverName = "ClaimsPrincipal";
    options.IncludeHttpRouteMetadata = true;
    options.IncludeEndpointMetadata = true;
});
```

Provider-specific or host-specific services should be added separately:

```csharp
builder.Services.AddAsiBackboneInMemoryStorage();
builder.Services.AddAsiBackboneEntityFrameworkCore();
```

The ASP.NET Core package should not implicitly register EF Core, in-memory persistence, signing providers, concrete policy rules, or host authentication handlers.

## Middleware Boundary

Middleware may be appropriate when the work is request-wide and host-neutral.

Good middleware candidates include:

- ensuring a correlation ID exists;
- attaching an AsiBackbone request context to `HttpContext.Items`;
- resolving current actor context from `HttpContext.User` through a registered resolver;
- making policy-context metadata available to later endpoint code;
- mapping known AsiBackbone operation failures into consistent HTTP responses when explicitly enabled.

Middleware should avoid hidden enforcement behavior. It should not silently block requests unless the host has explicitly opted into an enforcement middleware pattern and supplied the policy evaluator, policy context builder, and outcome mapping behavior.

The safe default is context preparation, not automatic decision enforcement.

## Endpoint Mapping Boundary

Endpoint mapping may be useful for workflows that are naturally HTTP-addressable, but it should remain optional.

Possible future endpoints include:

- acknowledgment challenge creation;
- acknowledgment completion;
- decision receipt lookup;
- audit receipt lookup by correlation ID or decision ID;
- health or diagnostics endpoints for configured AsiBackbone integration.

These endpoints should be opt-in and route-prefix controlled by the host:

```csharp
app.MapAsiBackboneEndpoints("/asi-backbone");
```

The package should not require public endpoints for core operation. Hosts should be able to use only services and middleware if they do not want endpoint mapping.

## Current User and Current Actor Seams

ASP.NET Core already has a `ClaimsPrincipal` model. AsiBackbone Core should remain framework-neutral, so the ASP.NET Core package should provide the translation seam.

A likely contract is:

```csharp
public interface IAsiBackboneHttpActorResolver
{
    ValueTask<AsiBackboneActorContext> ResolveAsync(
        HttpContext httpContext,
        CancellationToken cancellationToken = default);
}
```

Default behavior may map common claim types into actor fields, but the package should allow hosts to replace this entirely.

The actor resolver should not assume:

- which claim is the true subject identifier;
- that email is present;
- that a human user is always present;
- that the actor is always authenticated;
- that one authentication scheme is preferred;
- that NetCoreApplicationTemplate claims translation is available.

A NetCoreApplicationTemplate host can plug in its own claims translation logic, while a plain ASP.NET Core host can use a simpler resolver.

## Policy Context Builder Seam

A web request usually contains route, endpoint, user, tenant, origin, resource, method, and correlation data. The ASP.NET Core package can help assemble that into a Core policy context.

A likely contract is:

```csharp
public interface IAsiBackboneHttpPolicyContextBuilder<TContext>
{
    ValueTask<TContext> BuildAsync(
        HttpContext httpContext,
        CancellationToken cancellationToken = default);
}
```

The package may provide default builders for simple scenarios, but production hosts should be expected to customize context construction.

The package should support host-specific additions such as:

- tenant or region;
- client application;
- resource identifier;
- risk classification;
- requested capability;
- policy version;
- policy hash;
- correlation ID;
- request provenance metadata.

## Decision Outcome to HTTP Mapping

Core decision outcomes should remain domain concepts. The ASP.NET Core package may provide default HTTP mappings, but the host should be able to override them.

A reasonable default mapping is:

| Core outcome | Suggested HTTP behavior |
| --- | --- |
| `Allow` | Continue pipeline or return success from endpoint logic. |
| `Warn` | Continue with warning metadata or response headers when appropriate. |
| `Deny` | Return `403 Forbidden` or host-selected status. |
| `Defer` | Return `202 Accepted`, `409 Conflict`, or host-selected pending-state response. |
| `RequireAcknowledgment` | Return `428 Precondition Required`, `409 Conflict`, or a custom challenge response. |
| `Escalate` | Return `409 Conflict`, `423 Locked`, `503 Service Unavailable`, or host-selected escalation response. |

Because status-code policy can affect security posture, the package should not force a universal mapping. Hosts that mask unauthorized or scanner traffic as `404 Not Found` should be able to preserve that policy.

Problem Details support should be an adapter, not the only response model.

## Plain ASP.NET Core Host Compatibility

A plain ASP.NET Core application should be able to use the package with only standard ASP.NET Core dependencies.

The plain-host path should support:

- normal `Program.cs` service registration;
- optional middleware registration;
- optional endpoint mapping;
- custom actor resolver;
- custom policy context builder;
- any host-selected authentication and authorization configuration;
- any host-selected storage package or no storage package.

No NetCoreApplicationTemplate conventions should be required.

## NetCoreApplicationTemplate Host Compatibility

NetCoreApplicationTemplate can remain a preferred validation host, but not a package dependency.

A NetCoreApplicationTemplate host may supply richer integrations, such as:

- existing correlation ID conventions;
- existing claims translation;
- existing Problem Details behavior;
- existing security-header and rate-limiting posture;
- existing logging conventions;
- existing authentication endpoint boundaries.

These integrations should live in host code, samples, documentation, or optional adapters only if a later package justifies them. The ASP.NET Core package itself should remain usable without the template.

## Hidden Host Assumptions to Avoid

The package design should avoid assumptions such as:

- every host uses MVC;
- every host uses minimal APIs;
- every host uses Identity;
- every host exposes public acknowledgment endpoints;
- every host stores audit records in a database;
- every host wants AsiBackbone to enforce decisions through middleware;
- every actor is a human user;
- every request has a tenant, region, or email;
- every denial should be returned as the same HTTP status code;
- every application uses NetCoreApplicationTemplate.

The integration should prefer explicit host registration over automatic discovery or hidden behavior.

## Follow-up Implementation Issues

This design note creates a clean implementation path for later issues:

1. Create `CDCavell.AsiBackbone.AspNetCore` project and package metadata.
2. Add ASP.NET Core service registration extensions.
3. Add HTTP actor resolver abstraction and default claims-based resolver.
4. Add HTTP policy context builder abstraction.
5. Add correlation/request-context middleware.
6. Add optional decision outcome to Problem Details mapper.
7. Add optional endpoint mapping helpers for acknowledgment and decision receipt workflows.
8. Add a plain ASP.NET Core sample host.
9. Add a NetCoreApplicationTemplate-based sample host or documentation page.

## Boundary Summary

`CDCavell.AsiBackbone.AspNetCore` should be the web host adapter for AsiBackbone.

It belongs at the edge between ASP.NET Core requests and Core governance primitives. It may prepare request context, resolve actors, build policy contexts, expose optional endpoint helpers, and map decision outcomes into HTTP-friendly responses.

It should not own persistence, policy definitions, authentication schemes, authorization rules, database migrations, signing providers, NetCoreApplicationTemplate conventions, or external execution.

That boundary keeps the package useful for both plain ASP.NET Core applications and NetCoreApplicationTemplate hosts while preserving the broader AsiBackbone principle: governance spine first, host assumptions last.
