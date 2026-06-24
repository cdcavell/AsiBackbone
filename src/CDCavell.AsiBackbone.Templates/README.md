# AsiBackbone.Templates

`AsiBackbone.Templates` provides `dotnet new` templates for creating Accountable Systems Infrastructure governed ASP.NET Core hosts.

The package is a developer-experience scaffold. It is not a runtime dependency, production host baseline, compliance guarantee, storage provider, signing provider, or execution engine.

## Install

```powershell
dotnet new install AsiBackbone.Templates
```

## Create a governed Web API

```powershell
dotnet new asibackbone-webapi -n MyGovernedApi --hostStyle plain
```

Available host styles:

| Host style | Description |
| --- | --- |
| `plain` | Standard ASP.NET Core Web API structure with direct AsiBackbone package registration. |
| `netcoretemplate` | NetCoreApplicationTemplate-aligned wording and defaults for teams that want to adapt the generated host toward that baseline. This does not make NetCoreApplicationTemplate a dependency of AsiBackbone. |

## What the generated app demonstrates

The `asibackbone-webapi` template includes:

- `AddAsiBackboneAspNetCore()` registration;
- `UseAsiBackboneEndpointGovernance()` middleware;
- a sample governance constraint and decision policy;
- a Minimal API endpoint with fluent endpoint-governance metadata;
- a controller action with `[RequireGovernancePolicy]`, `[RequireCapabilityGrant]`, and `[EmitGovernanceAudit]` attributes;
- non-durable in-memory audit storage for local development and test-friendly inspection;
- the AsiBackbone analyzer package reference;
- a generated README with boundaries and next steps.

## Boundary notes

Generated projects are intentionally local-first. They do not provide production durability, production signing, legal non-repudiation, tamper-evidence, authentication, authorization, deployment hardening, or compliance certification by default.

The generated host remains responsible for authentication, authorization, database/provider selection, migrations, signing/key-management, observability, backup, retention, legal/compliance review, and actual business execution.
