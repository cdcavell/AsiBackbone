# CDCavell.AsiBackbone

[![CI](https://github.com/cdcavell/AsiBackbone/actions/workflows/ci.yml/badge.svg)](https://github.com/cdcavell/AsiBackbone/actions/workflows/ci.yml)
[![Line Coverage Gate](https://img.shields.io/badge/line%20coverage%20gate-75%25-brightgreen)](https://cdcavell.github.io/AsiBackbone/coverage/index.html)
[![Core Branch Coverage Gate](https://img.shields.io/badge/core%20branch%20gate-90%25-brightgreen)](https://cdcavell.github.io/AsiBackbone/coverage/core/index.html)
[![Documentation](https://github.com/cdcavell/AsiBackbone/actions/workflows/publish-docs.yml/badge.svg)](https://github.com/cdcavell/AsiBackbone/actions/workflows/publish-docs.yml)
[![Docs](https://img.shields.io/badge/docs-GitHub%20Pages-blue)](https://cdcavell.github.io/AsiBackbone/)
[![.NET](https://img.shields.io/badge/.NET-10.0-purple)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/license-MIT-blue)](LICENSE.txt)
[![Security Policy](https://img.shields.io/badge/security-policy-blue)](SECURITY.md)
[![GitHub Release](https://img.shields.io/github/v/release/cdcavell/AsiBackbone?sort=semver&display_name=tag&label=release)](https://github.com/cdcavell/AsiBackbone/releases)
[![Zenodo DOI](https://img.shields.io/badge/DOI-10.5281%2Fzenodo.20546032-blue)](https://doi.org/10.5281/zenodo.20546032)

**Accountable Systems Infrastructure for governed .NET decision flow.**

> AI may provide the intellect. AsiBackbone provides the accountable spine.

---

## The practical problem

Most software can tell you *what* happened. Far less can show that an action was evaluated before it executed: which rules shaped the decision, which policy version applied, whether acknowledgment was required, how follow-on authority was scoped, and where the host took responsibility for execution.

AsiBackbone is a .NET package family for that decision boundary. It helps a host application build safe policy context, evaluate constraints, return a structured decision, preserve a decision receipt, optionally scope continuation, and then let the host decide whether and how to execute.

In this software project, **ASI** means **Accountable Systems Infrastructure**.

## Implementation-first mental model

A normal API adoption path looks like this:

```text
HTTP request
  -> host builds safe evaluation context
  -> host-owned rules evaluate the request
  -> AsiBackbone returns a GovernanceDecision
  -> host writes audit residue / decision receipt
  -> host continues only when decision.CanProceed is true
```

Use plain engineering translations first:

| Project term | Practical meaning |
| --- | --- |
| Governance spine | Policy decision pipeline around consequential operations. |
| Audit residue | Decision receipt or audit-log payload. |
| Acknowledgment handshake | Confirmation workflow before a risky operation. |
| Capability grant | Short-lived scoped permission. |
| Governance outbox | Durable outbox pattern for governance events. |
| OpenTelemetry projection | Optional traces/metrics projection after local records exist. |
| Host-owned execution boundary | The application code that performs or refuses the protected operation. |

## First code path

The snippet below is an intentionally small README slice, not a complete `Program.cs`. It shows the primary governance path: build safe context, evaluate policy, write audit residue, and let the host execute only after the decision allows it. The full compile-ready walkthrough lives in [First 15 Minutes: Standard API Gating](https://cdcavell.github.io/AsiBackbone/articles/quickstart-api-gating.html).

```csharp
// Registration: Core evaluator + one host-owned rule + local in-memory audit sink.
builder.Services.AddAsiBackboneAspNetCore();
builder.Services.AddSingleton<InMemoryAuditLedger>();
builder.Services.AddSingleton<IAsiBackboneAuditSink>(sp =>
    sp.GetRequiredService<InMemoryAuditLedger>());
builder.Services.AddSingleton<IAsiBackboneConstraint<AsiBackboneConstraintEvaluationContext>, AllowedRegionConstraint>();
builder.Services.AddSingleton<IAsiBackbonePolicyEvaluator<AsiBackboneConstraintEvaluationContext>>(sp =>
    new DefaultAsiBackbonePolicyEvaluator<AsiBackboneConstraintEvaluationContext>(
        sp.GetServices<IAsiBackboneConstraint<AsiBackboneConstraintEvaluationContext>>(),
        decisionPolicy: null,
        options: new AsiBackbonePolicyEvaluatorOptions { DenyWhenNoConstraints = true }));

app.MapPost("/api/orders/{region}/approve", async (
    string region,
    HttpContext httpContext,
    IAsiBackbonePolicyEvaluator<AsiBackboneConstraintEvaluationContext> evaluator,
    IAsiBackboneAuditSink auditSink,
    CancellationToken cancellationToken) =>
{
    var metadata = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["operation"] = "orders.approve",
        ["region"] = region,
        ["risk"] = "routine-api-write"
    };

    var context = new AsiBackboneConstraintEvaluationContext(
        correlationId: httpContext.TraceIdentifier,
        policyVersion: "policy-v1",
        policyHash: "policy-hash-v1",
        metadata: metadata);

    GovernanceDecision decision = await evaluator.EvaluateAsync(context, cancellationToken);

    AuditResidue residue = AuditResidue.FromDecision(
        AsiBackboneActorContext.Human("example-user", "Example User"),
        operationName: "orders.approve",
        decision,
        metadata: context.Metadata);

    await auditSink.WriteAsync(residue, cancellationToken);

    if (!decision.CanProceed)
    {
        return Results.Json(new
        {
            allowed = false,
            decision = decision.Outcome.ToString(),
            decision.ReasonCodes,
            auditEventId = residue.EventId
        }, statusCode: StatusCodes.Status403Forbidden);
    }

    // Host-owned execution starts here. AsiBackbone does not approve the order itself.
    return Results.Ok(new
    {
        allowed = true,
        message = "Host order approval would run after this governance decision.",
        auditEventId = residue.EventId
    });
});
```

For production-style hosts, add durable audit/outbox persistence, signing or verification, DLP/classification, provider emission, and operational monitoring only where the host has explicitly chosen and configured those boundaries.

## Package family

Stable `1.1.x` package family. `1.1.1` is the current patch release; `1.1.0` expanded the original `1.0.0` boundary with analyzer, OpenTelemetry, signing-provider, and testing-harness packages for the compatible `1.x` line.

| Package | Role |
| --- | --- |
| `CDCavell.AsiBackbone.Core` | Framework-neutral governance primitives: decisions, constraints, acknowledgments, audit residue, lifecycle events, capability-token abstractions, durable outbox contracts, provider-neutral emission contracts, DLP/classification policy primitives, signing-ready metadata, canonical hashing/signing seams, and verification-policy primitives. |
| `CDCavell.AsiBackbone.Storage.InMemory` | Non-durable in-memory storage helpers for tests, samples, local validation, lifecycle events, and outbox proof paths. |
| `CDCavell.AsiBackbone.EntityFrameworkCore` | EF Core model configuration and host-owned persistence for audit ledger, acknowledgments, lifecycle events, and governance outbox records. |
| `CDCavell.AsiBackbone.AspNetCore` | ASP.NET Core host adapters for actor context, request correlation, audit enrichment, HTTP result mapping, acknowledgment challenge flows, endpoint governance, and hosted outbox drain integration. |
| `CDCavell.AsiBackbone.Testing` | Test-only harness helpers for deterministic endpoint governance, policy results, capability validation, in-memory audit inspection, non-durable outbox storage, and no-signature signing seams. |
| `CDCavell.AsiBackbone.Analyzers` | Roslyn analyzer safety rails for governance persistence and continuation flows. |
| `CDCavell.AsiBackbone.OpenTelemetry` | Released OpenTelemetry governance emission provider that projects provider-neutral envelopes into .NET diagnostics. |
| `CDCavell.AsiBackbone.Signing.LocalDevelopment` | Local-development signing and verification for tests, samples, and wiring proof paths only. Not for production key custody. |
| `CDCavell.AsiBackbone.Signing.ManagedKey` | Managed-key signing adapter boundary. The host supplies the actual managed-key client, credentials, key operations, verification path, monitoring, and operational policy. |

Future Event Hubs, Purview, Azure-specific, gateway, robotics, immutable-storage, or additional provider packages are not part of the stable contract unless separately reviewed and released.

## Start here

For **implementation-first adoption**:

- [Implementation-First Adoption Path](https://cdcavell.github.io/AsiBackbone/articles/implementation-first-adoption.html) — plain engineering translations and the recommended first reading path.
- [First 15 Minutes: Standard API Gating](https://cdcavell.github.io/AsiBackbone/articles/quickstart-api-gating.html)
- [Reference Deployment: Plain ASP.NET Core Host Evidence](https://cdcavell.github.io/AsiBackbone/articles/reference-deployment.html)
- [Testing Harness](https://cdcavell.github.io/AsiBackbone/articles/testing-harness.html)
- [Terminology Map](https://cdcavell.github.io/AsiBackbone/articles/terminology-map.html)

For **optional conceptual background**:

- [Intent to Execution: An Accountability Pattern](https://cdcavell.github.io/AsiBackbone/articles/intent-to-execution-pattern.html)
- [Core Governance Flow Diagrams](https://cdcavell.github.io/AsiBackbone/articles/core-governance-flow-diagrams.html)
- [ASI Backbone Concept Synopsis](https://cdcavell.github.io/AsiBackbone/articles/asi-backbone-concept.html)
- [Dynamic Liability Handshake](https://cdcavell.github.io/AsiBackbone/articles/dynamic-liability-handshake.html)
- [Core Domain Language](https://cdcavell.github.io/AsiBackbone/articles/core-domain-language.html)
- [Host-Owned Execution Enforcement](https://cdcavell.github.io/AsiBackbone/articles/host-owned-execution-enforcement.html)

The full, categorized documentation set lives at the [documentation site](https://cdcavell.github.io/AsiBackbone/).

## Current status

Stable `1.1.x` is the current released line for the compatible `1.x` API, with `1.1.1` as the latest patch. The repository includes the Core foundation, in-memory validation storage, EF Core host-owned persistence, ASP.NET Core integration, test harness helpers, analyzers, the OpenTelemetry provider, local-development signing, the managed-key signing adapter boundary, samples, release validation, and host-validation documentation.

The stable API contract is documented in [API Compatibility and SemVer](https://github.com/cdcavell/AsiBackbone/blob/main/docs/articles/api-compatibility-and-semver.md); the original `1.0.0` baseline and `1.1.0` addendum are recorded in the [Historical Stable API Review](https://github.com/cdcavell/AsiBackbone/blob/main/docs/articles/stable-api-review.md).

## Security and vulnerability reporting

Please report sensitive concerns through the repository [Security Policy](SECURITY.md).

## Relationship to NetCoreApplicationTemplate

NetCoreApplicationTemplate may be used as a preferred host baseline during development and validation, but AsiBackbone does not require it.

```text
NetCoreApplicationTemplate = preferred host baseline
AsiBackbone               = optional governance/module package family
Consumer application      = chooses whether to use either or both
```

A consumer should be able to use AsiBackbone in an application generated from NetCoreApplicationTemplate, in an existing ASP.NET Core application, or in a custom host that provides the required infrastructure. See [NetCoreApplicationTemplate Host Validation](https://github.com/cdcavell/AsiBackbone/blob/main/docs/articles/netcoreapplicationtemplate-host-validation.md).

## Alignment boundary

In this repository, ASI means **Accountable Systems Infrastructure**. AsiBackbone documentation may reference the broader Eden/Backbone framework as conceptual inspiration, but implementation claims should remain grounded in practical software governance.

Safe language:

- AsiBackbone stands for Accountable Systems Infrastructure Backbone.
- AsiBackbone implements governance-oriented software primitives.
- AsiBackbone helps structure consequential decision flow through constraints, acknowledgment, audit, capability boundaries, durable outbox persistence, provider emission, and signing-provider boundaries.
- AsiBackbone can surround intelligent or decision-producing systems with accountable execution infrastructure.
- AsiBackbone is inspired by broader Eden/Backbone governance concepts without claiming to implement an intelligence engine.

Avoid language such as:

- AsiBackbone implements an intelligence engine.
- AsiBackbone proves the Eden Hypothesis.
- AsiBackbone is an AI model.
- AsiBackbone is tamper-evident or immutable by default.
- AsiBackbone replaces AI safety governance, legal review, operational security, DLP review, or organizational accountability.

## Design principles

- Keep Core small.
- Keep Core dependency-light.
- Avoid hidden host assumptions.
- Prefer explicit integration over magic.
- Let the host own infrastructure.
