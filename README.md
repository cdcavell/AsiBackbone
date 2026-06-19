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

## The problem this is about

Most software can tell you *what* happened. Far less can show that an action's **intent was acknowledged before it executed** — which constraints shaped the decision, who affirmed it, under which policy version, how follow-on authority was scoped, and whether the execution that followed matched the decision that authorized it.

As more consequential actions are proposed by agents, services, and automated workflows, the distance between *"we logged it"* and *"we can account for it"* keeps widening. Ordinary authorization answers "is this caller allowed?" Ordinary logging answers "did something occur?" Neither captures the link in between: a deliberate, inspectable record of intent passing through policy into execution.

AsiBackbone is one concrete, .NET-native way to make that link a first-class part of a system. It turns the path from **intent to execution** into an explicit spine — policy context, constraint evaluation, decision, acknowledgment, audit residue, capability scoping — that the host application owns and can later explain.

It is meant to be useful whether you adopt the packages or simply study the pattern. If it gives you a clearer way to talk about accountable decision flow in your own stack, it has done its job.

In this software project, **ASI** means **Accountable Systems Infrastructure**.

## Why this repository exists even if you never install it

AsiBackbone is also intended as a concrete reference specimen for discussing accountable decision flow: how intent becomes policy-reviewed action, how acknowledgment is captured, how authority is scoped, how audit residue is preserved, and where execution remains host-owned. The packages are one implementation path; the pattern is the larger conversation.

## The shape of the idea

A typical AsiBackbone flow:

```
Intent or request
  -> Build policy context
  -> Evaluate constraints
  -> Compose decision result
  -> Require acknowledgment when needed
  -> Write audit residue
  -> Issue optional scoped capability token
  -> Preserve local audit/outbox record when provider emission is used
  -> Optionally sign or verify governance artifacts when a provider is configured
  -> Optionally emit a minimized governance envelope to a downstream provider
  -> Host application decides whether and how to execute
```

The point of interest is the **decision boundary**: the moment between proposed intent and host execution. AsiBackbone focuses there, and deliberately leaves execution itself to the host.

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

## What is AsiBackbone?

AsiBackbone helps a host application evaluate intent before execution, apply policy constraints, require acknowledgment when needed, preserve audit records, and optionally scope follow-on execution through short-lived capability tokens. It is designed for systems where consequential actions need to be governed, explained, audited, constrained, preserved locally, and optionally projected into observability or governance systems before the host application executes or reviews them.

AsiBackbone should be understood as **governance infrastructure**, not an intelligence engine.

> **Important**
>
> These packages do **not** implement an intelligence engine, host AI models, train AI models, control physical systems, certify compliance, prove the Eden/Backbone framework, or provide production tamper-evidence by default. They provide framework-neutral building blocks and host integration seams for governing consequential actions in software systems.

## What questions does it help a system answer?

Many systems eventually need more than ordinary authorization and application logging. A consequential action may need to answer:

- Is this action allowed right now?
- Which constraints, policies, and reason codes shaped the decision?
- Does this request require human acknowledgment before execution?
- Which policy version and policy hash were active?
- Can execution be scoped through a short-lived capability token?
- Was the decision preserved in durable local storage before downstream provider emission?
- Can signing or verification metadata be attached without forcing Core to own key management?
- Can a reviewer later understand why the system allowed, warned, denied, deferred, required acknowledgment, recommended escalation, or emitted a governance event?

## What it does *not* do

AsiBackbone does not:

- Replace normal authentication or authorization.
- Guarantee compliance with any law, regulation, audit framework, or security standard.
- Host, train, run, or orchestrate AI models.
- Implement an intelligence engine.
- Perform host operational actions by itself.
- Own the consuming application's database, migrations, deployment, observability backend, exporter configuration, key-management boundary, verification path, or operational policy.
- Provide production tamper-evidence, immutability, legal non-repudiation, external anchoring, or compliance certification by default.
- Replace legal review, AI safety governance, organizational accountability, operational security, DLP review, or key-management controls.

The host application remains responsible for execution behavior and operational controls.

## Who might find it useful

- Enterprise .NET applications with consequential administrative actions.
- AI-agent gateways that need policy checks before tool or API execution.
- Human-in-the-loop workflows where approval or acknowledgment matters.
- Government, public-sector, education, healthcare, finance, legal, or other regulated systems that need stronger auditability.
- Platform engineering workflows that need clear allow, deny, defer, acknowledgment, or escalation decisions before external execution.
- Applications that need capability-scoped grants instead of broad, long-lived authority.
- Teams who want a concrete reference for what an intent-to-execution audit trail can look like, whether or not they take the dependency.

## The package family

Stable `1.1.x` package family. `1.1.1` is the current patch release; `1.1.0` expanded the original `1.0.0` boundary (Core, in-memory storage, EF Core, ASP.NET Core) with analyzer, OpenTelemetry, and signing-provider packages for the compatible `1.x` line.

| Package | Role |
| --- | --- |
| `CDCavell.AsiBackbone.Core` | Framework-neutral governance primitives: decisions, constraints, acknowledgments, audit residue, lifecycle events, capability-token abstractions, durable outbox contracts, provider-neutral emission contracts, DLP/classification policy primitives, signing-ready metadata, canonical hashing/signing seams, and verification-policy primitives. |
| `CDCavell.AsiBackbone.Storage.InMemory` | Non-durable in-memory storage helpers for tests, samples, local validation, lifecycle events, and outbox proof paths. |
| `CDCavell.AsiBackbone.EntityFrameworkCore` | EF Core model configuration and host-owned persistence for audit ledger, acknowledgments, lifecycle events, and governance outbox records. |
| `CDCavell.AsiBackbone.AspNetCore` | ASP.NET Core host adapters for actor context, request correlation, audit enrichment, HTTP result mapping, acknowledgment challenge flows, endpoint governance, and hosted outbox drain integration. |
| `CDCavell.AsiBackbone.Analyzers` | Roslyn analyzer safety rails for governance persistence and continuation flows (advisory, build-time; not runtime enforcement). |
| `CDCavell.AsiBackbone.OpenTelemetry` | Released OpenTelemetry governance emission provider that projects provider-neutral envelopes into .NET diagnostics. |
| `CDCavell.AsiBackbone.Signing.LocalDevelopment` | Local-development signing and verification for tests, samples, and wiring proof paths only. Not for production key custody. |
| `CDCavell.AsiBackbone.Signing.ManagedKey` | Managed-key signing adapter boundary. The host supplies the actual managed-key client, credentials, key operations, verification path, monitoring, and operational policy. |

Future Event Hubs, Purview, Azure-specific, gateway, robotics, immutable-storage, or additional provider packages are not part of the stable contract unless separately reviewed and released. The stable `1.x` compatibility policy is documented in [API Compatibility and SemVer](https://github.com/cdcavell/AsiBackbone/blob/main/docs/articles/api-compatibility-and-semver.md).

## Start here

If you came to **understand the idea**:

- [Intent to Execution: An Accountability Pattern](https://cdcavell.github.io/AsiBackbone/articles/intent-to-execution-pattern.html) — start here; the idea with no .NET prerequisite.
- [ASI Backbone Concept Synopsis](https://cdcavell.github.io/AsiBackbone/articles/asi-backbone-concept.html) — what the governance spine is and why it is shaped this way.
- [Dynamic Liability Handshake](https://cdcavell.github.io/AsiBackbone/articles/dynamic-liability-handshake.html) — the acknowledgment-of-intent link, the part most audit trails skip.
- [Core Domain Language](https://cdcavell.github.io/AsiBackbone/articles/core-domain-language.html) — the vocabulary of decision flow.
- [Host-Owned Execution Enforcement](https://cdcavell.github.io/AsiBackbone/articles/host-owned-execution-enforcement.html) — where the spine ends and the host's responsibility begins.

If you came to **wire it into a host**:

- [Why AsiBackbone?](https://cdcavell.github.io/AsiBackbone/articles/why-asi-backbone.html)
- [Progressive Adoption Ladder](https://cdcavell.github.io/AsiBackbone/articles/progressive-adoption.html) — smallest Core-only path first, optional add-ons after.
- [First 15 Minutes: Standard API Gating](https://cdcavell.github.io/AsiBackbone/articles/quickstart-api-gating.html)
- [Terminology Map](https://cdcavell.github.io/AsiBackbone/articles/terminology-map.html)

The full, categorized documentation set lives at the [documentation site](https://cdcavell.github.io/AsiBackbone/).

## Current status

Stable `1.1.x` is the current released line for the compatible `1.x` API, with `1.1.1` as the latest patch. The repository includes the Core foundation, in-memory validation storage, EF Core host-owned persistence, ASP.NET Core integration, analyzers, the OpenTelemetry provider, local-development signing, the managed-key signing adapter boundary, samples, release validation, and host-validation documentation.

The stable API contract is documented in [API Compatibility and SemVer](https://github.com/cdcavell/AsiBackbone/blob/main/docs/articles/api-compatibility-and-semver.md); the original `1.0.0` baseline and `1.1.0` addendum are recorded in the [Historical Stable API Review](https://github.com/cdcavell/AsiBackbone/blob/main/docs/articles/stable-api-review.md). The [Production Wording and Stable Signing Boundaries](https://github.com/cdcavell/AsiBackbone/blob/main/docs/articles/production-wording-and-alpha-limitations.md) page distinguishes stable signing-ready primitives, the local-development signer, the managed-key adapter boundary, future concrete provider packages, and unsupported production tamper-evidence claims.

## Security and vulnerability reporting

Please report vulnerabilities or sensitive concerns through the repository [Security Policy](SECURITY.md). The policy defines the supported `1.x` release line, reporting expectations, and boundaries around compliance certification, production tamper-evidence, legal protection, host-owned execution, persistence, deployment, key custody, and production hardening.

Do not include exploit details, secrets, private keys, customer data, or other sensitive operational information in a public issue or pull request.

## Relationship to NetCoreApplicationTemplate

NetCoreApplicationTemplate may be used as a preferred host baseline during development and validation, but AsiBackbone does not require it.

```
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
- Let integration packages own persistence, web integration, signing, storage, samples, observability, provider-specific emission, and external execution concerns.
- Keep package boundaries clear before adding behavior.
- Treat durable local/outbox persistence as the reliability baseline before external emission.
- Treat provider signing as one part of an operational trust model, not as tamper-evidence by itself.
- Treat NetCoreApplicationTemplate as a preferred host, not a parent framework.
- Treat AsiBackbone as Accountable Systems Infrastructure: governance infrastructure, not an intelligence engine.

## License

MIT. See [LICENSE.txt](https://github.com/cdcavell/AsiBackbone/blob/main/LICENSE.txt).
