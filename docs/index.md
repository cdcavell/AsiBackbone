# ASI Backbone Documentation

Welcome to the ASI Backbone documentation site.

In this software project, **ASI** means **Accountable Systems Infrastructure**. AsiBackbone is a .NET governance and policy-control framework inspired by broader Eden/Backbone governance concepts, but implemented as practical software infrastructure. The project is a **governance spine**, not an intelligence engine. Its purpose is to define practical software patterns for policy evaluation, decision results, acknowledgment workflows, audit receipts, capability-gated execution, durable audit/outbox persistence, optional governance emission, signing-ready artifacts, and host integration.

> [!IMPORTANT]
> AsiBackbone does not implement artificial superintelligence. It provides framework-neutral building blocks for governing consequential actions in software systems.

## Start here

* [Why AsiBackbone?](articles/why-asi-backbone.md)  
  High-level purpose, target audience, and adoption rationale.

* [Getting Started](articles/getting-started.md)  
  Project orientation, local build instructions, and stable package direction.

* [1.0.0 Quickstart](articles/quickstart-100.md)  
  Package-consumer guidance for the first stable release line.

* [1.0.0 Release Notes](articles/release-notes-100.md)  
  First stable package-family release identity, known limitations, and stable package boundary.

* [1.1.0 Release Notes](articles/release-notes-110.md)  
  Additive observability, durable outbox, governance emission, OpenTelemetry, DLP/classification, and signing-ready release guidance.

* [Upgrade Guide: 1.0.0 to 1.1.0](articles/upgrade-100-to-110.md)  
  Incremental adoption guidance for existing consumers.

* [Core Domain Language](articles/core-domain-language.md)  
  Terminology and Core boundary for Accountable Systems Infrastructure, governance spine, constraints, collapse boundary, actor context, decision results, audit residue, acknowledgment, capability tokens, and gateway boundaries.

* [Policy Evaluator Pipeline](articles/policy-evaluator-pipeline.md)  
  Host-neutral policy and constraint evaluation flow.

* [Equations and Toy Models](articles/equations-and-toy-models.md)  
  Explains the conceptual progression from `Λ(t)` to `Λ(τ)` to `ΛS(x, τ)` and maps the Eden/Backbone collapse notation into practical AsiBackbone software terms: active policy structure, allowed decision states, acknowledgment, audit residue, and gateway-safe execution.

* [Documentation Articles](articles/)  
  Conceptual and implementation documentation for the AsiBackbone package family.

* [API Reference](https://cdcavell.github.io/AsiBackbone/api/CDCavell.AsiBackbone.Core.html)  
  Generated API documentation for public types.

* [Quality Reports](quality/)  
  Landing page for coverage and mutation-analysis reports when generated.

* [Repository](https://github.com/cdcavell/AsiBackbone)  
  Source code, issues, pull requests, and release history.

## Concept and model pages

The AsiBackbone documentation distinguishes software implementation, structural analogy, and theoretical inspiration. The software package is practical Accountable Systems Infrastructure. It can be used around intelligent or decision-producing systems, but it does not implement artificial superintelligence, host AI models, control robots, or prove the Eden/Backbone framework.

### ASI Backbone concept synopsis

The ASI Backbone concept, as implemented in this repository, is a governance pattern for placing accountable infrastructure around consequential software actions.

A host application may receive a request, recommendation, AI-agent proposal, administrative action, workflow step, or external execution intent. AsiBackbone gives that host a structured path for evaluating the intent before execution:

```text
Intent or request
  -> Build actor and policy context
  -> Evaluate constraints
  -> Compose governance decision
  -> Require acknowledgment when needed
  -> Preserve audit residue and lifecycle events
  -> Issue optional scoped capability grant
  -> Preserve local audit/outbox record when provider emission is used
  -> Optionally emit a minimized governance envelope
  -> Host decides whether and how to execute
```

The framework deliberately sits at the **decision boundary**. It helps answer which active policies applied, which reason codes shaped the result, whether acknowledgment was required, which policy version/hash was in force, whether execution should be capability-scoped, and what durable accountability residue should remain.

The broader Eden/Backbone framing uses collapse language to describe open possibility narrowing into realized form. In software terms, AsiBackbone narrows proposed intent into supported governance outcomes:

* `Allowed`
* `Warning`
* `Denied`
* `Deferred`
* `AcknowledgmentRequired`
* `EscalationRecommended`

That is the practical meaning of controlled collapse in this code base: proposed action becomes an explainable software decision under active constraints.

See also:

* [ASI Backbone Concept Synopsis](articles/asi-backbone-concept.md)
* [Core Domain Language](articles/core-domain-language.md)
* [Equations and Toy Models](articles/equations-and-toy-models.md)

### Dynamic Liability Handshake

The Dynamic Liability Handshake is documented in the code base as an acknowledgment and responsibility-handshake pattern. The public implementation remains careful: it records acknowledgment intent and responsibility context, but it does not provide legal protection, legal non-repudiation, compliance certification, or production tamper-evidence by itself.

The implemented Core language supports a framework-neutral request/response shape:

```text
Governance decision requires acknowledgment
  -> Create handshake request
  -> Present required acknowledgment text/code
  -> Actor accepts or rejects
  -> Preserve acknowledgment result
  -> Link acknowledgment to audit residue and later lifecycle events
```

A handshake request can carry actor identity, operation name, reason code, message, required acknowledgment code/text, risk level, risk category, correlation ID, trace ID, policy version, policy hash, schema version, and host-provided metadata. A handshake acknowledgment records the actor response, acknowledgment code, accepted/rejected result, UTC timestamp, correlation ID, trace ID, and metadata.

In practical terms, the handshake is the pause point before consequential execution. It helps the host say: the system evaluated the request, found that acknowledgment was required, presented the actor with the required responsibility statement, recorded whether the actor accepted or rejected it, and preserved enough context for later audit review.

See also:

* [Dynamic Liability Handshake](articles/dynamic-liability-handshake.md)
* [ASP.NET Core Integration Boundary](articles/aspnetcore-integration-boundary.md)
* [ASP.NET Core Endpoint Governance](articles/aspnetcore-endpoint-governance.md)

### Gateway and regional policy flow

Gateway flow is the external-execution safety pattern for systems where a decision may lead to API calls, infrastructure changes, workflow execution, tool invocation, robotics simulation, or other consequential downstream behavior.

AsiBackbone should not be described as a robot controller or external execution engine. The host or gateway owns execution. AsiBackbone supplies governance primitives that can be used before that execution occurs.

The recommended flow is:

```text
Global or upstream intent
  -> Regional/local policy context
  -> Constraint evaluation
  -> Governance decision
  -> Required acknowledgment if applicable
  -> Capability grant if execution is allowed and scoped
  -> Operational gateway validation
  -> Host-owned external execution or safe rejection
  -> Audit lifecycle and optional provider emission
```

The key rule is **no direct global-to-edge command pattern**. High-level intent should be translated through regional/local policy context and validated at an operational gateway before it reaches externally consequential systems. Robotics remains a later integration scenario and should be treated as an example of the gateway pattern, not as a current package capability.

Regional/local policy context may include jurisdiction, organization, environment, risk level, actor type, target resource, policy version, policy hash, DLP/classification posture, capability scope, and operational gateway limits. Gateway validation may then enforce command shape, rate limits, allowed verbs, safety constraints, expiration, revocation, and host-owned execution rules.

See also:

* [Gateway and Regional Policy Flow](articles/gateway-and-regional-policy-flow.md)
* [AI Agent Gateway](articles/scenarios/ai-agent-gateway.md)
* [Robotics Operational Gateway](articles/scenarios/robotics-operational-gateway.md)
* [Capability Grant Hardening](articles/capability-grant-hardening.md)

## Package documentation

The AsiBackbone package family should remain modular. Consumers should be able to adopt the pieces they need without inheriting unnecessary host assumptions.

The first stable `1.0.0` published package boundary covers:

```text
CDCavell.AsiBackbone.Core
CDCavell.AsiBackbone.Storage.InMemory
CDCavell.AsiBackbone.EntityFrameworkCore
CDCavell.AsiBackbone.AspNetCore
```

Current source and package-validation metadata also include these additional package projects for the `1.x` line:

```text
CDCavell.AsiBackbone.Analyzers
CDCavell.AsiBackbone.OpenTelemetry
CDCavell.AsiBackbone.Signing.LocalDevelopment
CDCavell.AsiBackbone.Signing.ManagedKey
```

A package project being present in source does not mean it provides production guarantees. Release notes and package READMEs define each package boundary.

Planned or later package areas remain separate unless a future release explicitly ships them as stable packages:

```text
CDCavell.AsiBackbone.EventHubs
CDCavell.AsiBackbone.Purview
CDCavell.AsiBackbone.Robotics
CDCavell.AsiBackbone.ImmutableStorage
```

## CDCavell.AsiBackbone.Core

Stable package.

`CDCavell.AsiBackbone.Core` is the dependency-light foundation package. It defines shared contracts, domain abstractions, result primitives, provider-neutral governance emission contracts, durable outbox contracts, DLP/classification policy primitives, signing-ready metadata abstractions, and framework-neutral language used by the rest of the package family.

Core remains free of direct ASP.NET Core, Entity Framework Core, database-provider, host-template, robotics, cloud SDK, OpenTelemetry SDK, and AI-model assumptions.

Primary responsibilities:

* Core domain abstractions
* Policy and constraint contracts
* Decision result primitives
* Operation result primitives
* Acknowledgment and audit abstractions
* Audit residue lifecycle events
* Governance emission contracts
* Durable outbox contracts
* DLP/classification failure policy primitives
* Capability-token abstractions
* Signing-ready metadata abstractions
* Policy version and policy hash fields
* Shared value objects
* Framework-neutral domain language

## CDCavell.AsiBackbone.Storage.InMemory

Stable package.

`CDCavell.AsiBackbone.Storage.InMemory` provides non-durable in-memory storage helpers for local validation, samples, and tests. It supports integration validation without requiring a database and should not be used as durable production storage.

Primary responsibilities:

* In-memory acknowledgment records
* In-memory audit receipts
* In-memory lifecycle events
* In-memory governance outbox proof paths
* Local validation behavior
* Non-production sample support

## CDCavell.AsiBackbone.EntityFrameworkCore

Stable package.

`CDCavell.AsiBackbone.EntityFrameworkCore` provides EF Core model configuration and persistence integration through a host-owned `DbContext`. AsiBackbone contributes model configuration and storage helpers while the consuming application owns the database provider, connection string, migrations, deployment, retention, access controls, and schema lifecycle.

Primary responsibilities:

* EF Core model configuration
* Entity mappings
* Audit ledger persistence
* Acknowledgment persistence
* Audit residue lifecycle persistence
* Governance outbox persistence
* Policy version persistence
* Host-owned DbContext integration

## CDCavell.AsiBackbone.AspNetCore

Stable package.

`CDCavell.AsiBackbone.AspNetCore` provides ASP.NET Core host integration seams while keeping Core framework-neutral. It adapts HTTP request context into AsiBackbone governance language and helps hosts map governance outcomes to HTTP-friendly responses when explicitly used by the application.

Primary responsibilities:

* Dependency injection extensions
* ASP.NET Core options and startup validation
* HTTP actor context resolution
* Request correlation and audit enrichment
* HTTP result mapping helpers
* Acknowledgment challenge models and response handling
* Hosted governance outbox drain integration
* Endpoint governance metadata and validation seams

The host application remains responsible for authentication, authorization, endpoint exposure, persistence registration, UI rendering, operational execution, exporter configuration, and production policy.

## CDCavell.AsiBackbone.Analyzers

Source package project.

`CDCavell.AsiBackbone.Analyzers` provides Roslyn analyzer safety rails for governance persistence and continuation flows. Analyzer packages should remain advisory development-time guardrails and should not be described as runtime enforcement.

Primary responsibilities:

* Static-analysis safety rails
* Governance persistence flow diagnostics
* Continuation-flow diagnostics
* Build-time feedback for package consumers

## CDCavell.AsiBackbone.OpenTelemetry

Source package project and documented `1.x` provider path.

`CDCavell.AsiBackbone.OpenTelemetry` provides a concrete OpenTelemetry governance emission provider. It adapts provider-neutral governance envelopes into .NET diagnostics primitives such as `ActivitySource` and `Meter` while leaving exporters, Azure Monitor, Application Insights, Log Analytics, SIEMs, dashboards, and backend routing to the host application.

Primary responsibilities:

* `IAsiBackboneGovernanceEmitter` implementation
* Activity/event projection
* Metric projection
* Stable `asibackbone.*` attribute names
* Provider-neutral result handling
* No cloud-provider SDK dependency by default

## CDCavell.AsiBackbone.Signing.LocalDevelopment

Source package project.

`CDCavell.AsiBackbone.Signing.LocalDevelopment` provides local-development RSA signing and verification for tests, samples, and host wiring proof paths. It is not a production managed-key provider and does not provide protected key custody, immutability, tamper-evidence, legal non-repudiation, or compliance certification.

Primary responsibilities:

* Local-only signing service implementation
* Local-only verification service implementation
* Signing metadata flow validation
* Sample/test wiring support
* Explicit non-production warning posture

## CDCavell.AsiBackbone.Signing.ManagedKey

Source package project.

`CDCavell.AsiBackbone.Signing.ManagedKey` provides a managed-key signing adapter boundary. The package defines the adapter shape and service integration while the host supplies the actual managed-key client, credentials, key identity, provider configuration, monitoring, and operational policy.

Primary responsibilities:

* Managed-key signing adapter
* Host-owned `IManagedKeySigningClient` boundary
* Signing request/result mapping
* Safe signing metadata preservation
* Retry/failure policy seams
* No raw private key material in Core
* No live Azure Key Vault, Managed HSM, cloud KMS, or HSM implementation by default

## CDCavell.AsiBackbone.Abstractions

Future split candidate.

If Core grows too large, shared interfaces and primitive contracts may be separated into an Abstractions package. Until that split is justified, these types remain in Core and `CDCavell.AsiBackbone.Abstractions` should not be described as part of the current package lineup.

Potential responsibilities if later added:

* Minimal shared interfaces
* Decision result contracts
* Policy evaluation contracts
* Audit receipt contracts
* Capability token contracts
* No implementation dependencies

## CDCavell.AsiBackbone.Robotics

Later integration package.

Robotics should remain a later-stage integration example. The current project line proves the policy, decision, acknowledgment, audit, capability-token, outbox, provider-emission, signing-ready, and gateway patterns before moving toward physical or robotic execution scenarios.

Primary responsibilities, if later added:

* Simulated robot command validation
* Operational gateway contracts
* Capability-scoped command authorization
* Safety-bound command envelopes
* Regional/local policy enforcement examples

## Project direction

The stable implementation path is:

1. Core governance primitives
2. Policy evaluator pipeline
3. Decision result model
4. Acknowledgment/handshake workflow
5. Audit residue and audit ledger contracts
6. Capability token abstractions
7. In-memory local validation storage
8. EF Core host-owned persistence integration
9. ASP.NET Core host integration
10. Durable audit lifecycle and governance outbox persistence
11. Provider-neutral governance emission contracts
12. OpenTelemetry provider projection
13. Signing-ready metadata and provider package boundaries
14. Plain ASP.NET Core sample host
15. Documentation and host-validation guidance

Future work may add Event Hubs, Purview, Azure-specific integration guidance, additional samples, gateway integrations, robotics examples, immutable-storage patterns, and follow-up release packaging.

This gives AsiBackbone a practical software foundation while preserving the broader framework boundary.

## Design principle

AsiBackbone should make consequential software actions easier to govern, audit, constrain, acknowledge, preserve, emit, verify, and explain.

It should be understood as Accountable Systems Infrastructure, not an intelligence engine.