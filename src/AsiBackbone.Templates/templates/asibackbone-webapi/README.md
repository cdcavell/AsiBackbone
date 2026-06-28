# Company.AsibackboneTemplate

This project was generated from the `asibackbone-webapi` template.

Selected host style: `__HOST_STYLE__`

## What this app demonstrates

- `AddAsiBackboneAspNetCore()` registration.
- `UseAsiBackboneEndpointGovernance()` middleware.
- A sample host-owned constraint and decision policy.
- A Minimal API endpoint with fluent endpoint-governance metadata.
- A controller action with `[RequireGovernancePolicy]`, `[RequireCapabilityGrant]`, and `[EmitGovernanceAudit]`.
- Non-durable in-memory audit storage for local development and test-friendly inspection.
- The AsiBackbone analyzer package reference.

## Run locally

```powershell
dotnet restore
dotnet run
```

Useful local endpoints:

```http
GET /
GET /sample/decision
GET /sample/audit/{correlationId}
POST /sample/minimal/execute
POST /sample/controller/execute
```

The `/sample/decision` endpoint returns the decision outcome, reason codes, correlation ID, policy version/hash, and audit event ID. Use that correlation ID with `/sample/audit/{correlationId}` to inspect the in-memory audit residue produced by the generated host.

## Host-style notes

`plain` is the smallest standard ASP.NET Core Web API shape.

`netcoretemplate` keeps the generated project independent from NetCoreApplicationTemplate while using wording and structure that can be adapted toward that enterprise baseline. NetCoreApplicationTemplate is not required by AsiBackbone and is not a dependency of this generated app.

## Production next steps

This generated app uses safe local-development defaults. Before production use, replace or add:

- real authentication and authorization;
- host-owned durable audit/outbox persistence;
- host-owned capability validation for scope, expiry, replay, actor binding, and downstream authorization;
- production signing/key-management if signed governance artifacts are required;
- operational logging, monitoring, backup, retention, and incident response;
- legal, compliance, security, and DLP review appropriate to the host system.

## Boundaries

This scaffold is a runnable adoption starting point. It keeps execution behavior and operational controls with the host application. For the full AsiBackbone scope statement, see [Project Boundaries and Non-Claims](https://cdcavell.github.io/AsiBackbone/articles/project-boundaries.html).

## Documentation

- https://cdcavell.github.io/AsiBackbone/articles/templates.html
- https://cdcavell.github.io/AsiBackbone/articles/quickstart-api-gating.html
- https://cdcavell.github.io/AsiBackbone/articles/aspnetcore-endpoint-governance.html
