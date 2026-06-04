# Core Domain Language and Alpha Boundary

This article defines the initial domain language for `CDCavell.ASIBackbone.Core` and the intended `0.1.0-alpha.1` package boundary.

`CDCavell.ASIBackbone.Core` is the framework-neutral foundation for the ASIBackbone package family. Its purpose is to define shared primitives for governing consequential software actions through explicit policy, constraint, acknowledgment, audit, and capability boundaries.

> [!IMPORTANT]
> ASIBackbone is governance infrastructure around intelligent or decision-producing systems. It is not a completed ASI implementation, not an AI model package, and not proof of artificial superintelligence.

## Core technical lane

Core should remain small, dependency-light, and host-neutral.

The Core technical lane is:

```text
Intent or request
  -> Policy context
  -> Constraint evaluation
  -> Decision result
  -> Optional acknowledgment
  -> Audit residue
  -> Optional capability token
  -> Host or gateway execution
```

Core defines the language and primitive contracts for this lane. It should not decide how a host persists records, exposes HTTP endpoints, wires middleware, or connects to an AI model.

## Domain terms

### Governance spine

The governance spine is the software path that every consequential action follows before execution.

It is not the intelligence engine. It is the control structure around decision-producing systems: policy evaluation, constraint checks, acknowledgment requirements, audit receipts, capability grants, and gateway enforcement.

### Intent or request

An intent is the proposed action that needs evaluation.

Examples include approving a document, calling an external API, starting an administrative workflow, executing a simulated command, or granting access to a protected operation.

Core should represent the intent in a way that can be evaluated without assuming ASP.NET Core, EF Core, a database, or a specific host application.

### Constraint

A constraint is a rule, condition, boundary, or policy requirement that narrows which outcomes are available.

Examples include:

* regional or local law
* role or permission requirements
* resource ownership
* risk thresholds
* time windows
* gateway capability limits
* required acknowledgment rules
* policy version requirements

A constraint may allow, deny, warn, or determine that it does not apply to a request.

Governance decisions may defer, require acknowledgment, or recommend escalation after constraint evaluation is composed into a decision.

### Active policy structure

The active policy structure is the set of constraints in force for a decision at a specific moment.

This term maps the ASI Backbone software model to the broader structure-conditioned collapse language: a request should not collapse into any possible action whatsoever. It should collapse only into the outcomes allowed by the active policy structure.

In software terms, the active policy structure should be explainable through policy version, policy hash, reason codes, and audit residue.

### Collapse boundary

A collapse boundary is the point where open possibility becomes a concrete software decision or action.

In Core, this is a practical software boundary, not a physical collapse claim. It marks the transition from proposed intent to one of the supported decision outcomes.

Supported initial decision outcomes are:

* `Allowed`
* `Warning`
* `Denied`
* `Deferred`
* `AcknowledgmentRequired`
* `EscalationRecommended`

### Actor context

The Core abstraction for this concept is `IAsiBackboneActorContext`.

Host integrations may map their current-user, current-service, worker, or agent identity model into this abstraction. For example, a future ASP.NET Core integration may adapt a host `ClaimsPrincipal` into `IAsiBackboneActorContext`, but Core itself must not depend on `ClaimsPrincipal`, `HttpContext`, authentication middleware, or any specific host identity provider.

Actor context describes who or what is requesting the action.

It may include:

* actor identifier
* actor type, such as user, service, system, or agent
* display name or label
* roles or claims when supplied by the host
* region or jurisdiction when supplied by the host
* authentication or provenance metadata when supplied by the host

Core should not own authentication. It should define framework-neutral shapes that host packages can populate.

### Policy context

Policy context is the evaluation input assembled from the intent, actor context, target resource, environment, risk classification, policy version, and correlation metadata.

Core should make policy context explicit so that decisions are explainable and repeatable.

### Decision result

A decision result is the primary output of policy evaluation.

A useful decision result should answer:

* what was requested
* who or what requested it
* what outcome was selected
* which policy version and hash produced the outcome
* whether acknowledgment is required
* whether escalation is required
* which reason codes explain the outcome
* which correlation ID links the decision to logs and audit records

The decision result should be one of the central primitives in Core.

### Operation result

An operation result represents whether a Core operation completed successfully and, if not, why it failed.

Decision results describe governance outcomes. Operation results describe package execution outcomes such as validation errors, missing context, invalid state, or failed receipt creation.

Keeping these separate avoids confusing policy denial with infrastructure failure.

### Audit residue

Audit residue is the durable trace left by a decision flow.

It may include:

* decision ID
* correlation ID
* actor reference
* intent summary
* decision outcome
* reason codes
* policy version
* policy hash
* acknowledgment reference when applicable
* capability token reference when applicable
* timestamp
* optional signature metadata

Core should define audit-oriented primitives without requiring a specific storage provider.

### Acknowledgment and responsibility handshake

Some consequential actions should require acknowledgment before execution.

The public API should favor implementation-grounded terms such as acknowledgment, responsibility handshake, or reflexive acknowledgment. Documentation may explain that this supports the broader Dynamic Liability Handshake concept.

Core should define the acknowledgment workflow boundary, not claim legal protection by itself.

### Capability token

A capability token is a short-lived, scoped permission grant for a specific operation.

A capability token should be:

* time-bound
* purpose-bound
* scope-bound
* traceable
* revocable where possible
* signed or verifiable where appropriate

Core should define token concepts and contracts. Signing implementation can live in a later signing package.

### Gateway boundary

A gateway boundary protects external or consequential systems from direct execution.

The gateway pattern ensures that a host or integration layer validates the decision result, acknowledgment status, capability token, and safety constraints before execution.

Robotics, physical execution, and other high-risk gateway examples should remain later integration scenarios after the Core governance pattern is stable.

## `0.1.0-alpha.1` Core boundary

The `0.1.0-alpha.1` boundary should establish language and primitives, not a full integration stack.

### In scope for Core

`CDCavell.ASIBackbone.Core` may include:

* framework-neutral domain abstractions
* actor context primitives
* policy context primitives
* constraint evaluation contracts
* decision result primitives
* operation result primitives
* reason code primitives
* acknowledgment or responsibility-handshake abstractions
* audit residue abstractions
* capability-token abstractions
* policy version and policy hash fields
* correlation ID support
* shared value objects
* XML documentation for public types

### Out of scope for Core

`CDCavell.ASIBackbone.Core` should not include:

* Entity Framework Core dependencies
* ASP.NET Core dependencies
* web middleware
* endpoint mapping
* database provider assumptions
* host startup logic
* direct dependency on NetCoreApplicationTemplate
* concrete ledger or database persistence
* concrete signing or key-management implementation
* robotics control implementation
* AI model hosting, training, inference, or orchestration

## Future package ownership

Future packages can build on Core without changing its host-neutral boundary.

| Package area | Ownership |
| --- | --- |
| `CDCavell.ASIBackbone.Core` | Framework-neutral governance primitives and domain language. |
| `CDCavell.ASIBackbone.AspNetCore` | ASP.NET Core service registration, middleware, endpoints, current-actor resolution, and HTTP policy hooks. |
| `CDCavell.ASIBackbone.Storage.InMemory` | Demo and test storage for early validation. |
| `CDCavell.ASIBackbone.Storage.EntityFrameworkCore` | EF Core model configuration and persistence hooks while the host owns the `DbContext`. |
| `CDCavell.ASIBackbone.Signing` | Signing and verification helpers for receipts, policy hashes, and capability tokens. |
| `CDCavell.ASIBackbone.Samples` | Console, worker, ASP.NET Core, and NetCoreApplicationTemplate-based examples. |
| `CDCavell.ASIBackbone.Robotics` | Later simulated or physical gateway examples after the governance spine is proven. |

## Alignment boundary

ASIBackbone documentation may reference the ASI Backbone concept and the Eden/ASI framework as conceptual inspiration. It should remain careful about claims.

Safe language:

* ASIBackbone is inspired by the ASI Backbone framework.
* ASIBackbone implements governance-oriented software primitives.
* ASIBackbone helps structure consequential decision flow through constraints, acknowledgment, audit, and capability boundaries.
* ASIBackbone can surround intelligent or decision-producing systems with accountable execution infrastructure.

Avoid language such as:

* ASIBackbone implements ASI.
* ASIBackbone proves the Eden Hypothesis.
* ASIBackbone is an AI model.
* ASIBackbone replaces AI safety governance, legal review, or organizational accountability.

## Guiding principle

`CDCavell.ASIBackbone.Core` should make consequential software actions easier to govern, audit, constrain, acknowledge, and explain while remaining free of host, persistence, web, and AI-model assumptions.
