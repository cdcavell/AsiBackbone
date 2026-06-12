# Why AsiBackbone?

AsiBackbone is a .NET governance package for systems that need to decide what may happen before anything consequential happens.

In this software project, **ASI** means **Accountable Systems Infrastructure**. The package family is designed as infrastructure for accountable decision flow, not as artificial superintelligence.

It provides policy, acknowledgment, audit, and capability-control primitives that can sit between intent and execution. A host application can use these primitives to evaluate a proposed action, explain why it was allowed or blocked, require human acknowledgment when appropriate, and leave an audit trail that can be reviewed later.

> [!IMPORTANT]
> AsiBackbone is not an artificial superintelligence implementation, AI model host, AI training framework, legal-compliance guarantee, or robotics control system. It is Accountable Systems Infrastructure for software systems that need governed, auditable decision flow.

## The problem it solves

Many applications eventually grow beyond simple authorization checks.

A system may need to answer questions such as:

- Should this action be allowed right now?
- Which policy or constraint produced the decision?
- Does this action require human acknowledgment first?
- What reason codes should be preserved for later review?
- Which policy version and hash were active when the decision was made?
- Should execution be scoped through a short-lived capability token?
- How can the host prove what happened without burying the decision in ordinary logs?

AsiBackbone provides a shared vocabulary and package structure for those concerns.

## What it provides

AsiBackbone helps a host application build a governance spine around consequential software actions.

Core benefits include:

| Benefit | What it means |
| --- | --- |
| Policy-driven decision gating | Proposed actions can be evaluated before the host executes them. |
| Explicit decision outcomes | Decisions can be allowed, warned, denied, deferred, acknowledgment-required, or escalation-recommended. |
| Human acknowledgment workflow | Consequential actions can pause until an authorized actor acknowledges responsibility or risk. |
| Audit residue | Decisions can leave structured reason codes, policy version, policy hash, correlation ID, timestamp, and metadata. |
| Capability-scoped execution | Follow-on execution can be represented as a short-lived, scoped permission grant rather than open-ended authority. |
| Host-owned integration | Applications keep ownership of their web host, persistence lifecycle, migrations, deployment, and execution behavior. |
| Framework-neutral core | Core primitives can be used without requiring ASP.NET Core, EF Core, NetCoreApplicationTemplate, AI packages, or robotics dependencies. |

## Where it fits

A typical AsiBackbone flow looks like this:

```text
Intent or request
  -> Build policy context
  -> Evaluate constraints
  -> Compose decision result
  -> Require acknowledgment when needed
  -> Write audit residue
  -> Issue optional scoped capability token
  -> Host application decides whether and how to execute
```

The host application remains responsible for the actual operation. AsiBackbone helps the host evaluate, document, and constrain the decision path before execution.

## Who should consider using it?

AsiBackbone may be useful for:

- Enterprise .NET applications with consequential administrative actions.
- AI-agent gateways that need policy checks before tool or API execution.
- Human-in-the-loop workflows where approval or acknowledgment matters.
- Government, public-sector, education, healthcare, finance, legal, or other regulated systems that need stronger auditability.
- Platform engineering workflows that need clear allow/deny/defer/escalate decisions before external execution.
- Applications that need capability-scoped grants instead of broad, long-lived authority.

These groups often include senior developers, system engineers, enterprise architects, platform engineering teams, AI integration architects, and security-conscious engineering teams. The package is intended to support those roles without making assumptions about their host framework, identity model, database ownership, deployment process, or operational controls.

See [Enterprise Adoption Personas](enterprise-adoption-personas.md) for a role-oriented view of how different enterprise readers may evaluate the package.

## When not to use it

AsiBackbone is probably not the right fit when:

- The application only needs ordinary role-based authorization.
- The action has no meaningful risk, audit, or policy-review requirement.
- The team wants an AI model framework, agent framework, or orchestration engine.
- The team expects a package to guarantee compliance with a law, regulation, or security standard.
- The host application is not prepared to own execution behavior, persistence configuration, and operational policy.

## Why not just use logs and authorization?

Authorization answers whether a user or service is generally permitted to do something.

Logs record that something happened.

AsiBackbone focuses on the decision boundary between those two points:

```text
A proposed action exists.
The system evaluates active policy and context.
The system produces a structured decision.
The decision may require acknowledgment or escalation.
The decision leaves audit residue.
Only then does the host consider execution.
```

That makes the decision path easier to reason about, test, document, and review.

## Practical adoption path

A small first adoption can be simple:

1. Pick one consequential action in the host application.
2. Build an AsiBackbone policy context for that action.
3. Add one or more constraints.
4. Evaluate the request.
5. Persist audit residue.
6. Require acknowledgment only for the cases that need it.
7. Keep execution in the host application.

This lets a team introduce governance incrementally without rewriting the entire application.

## Relationship to the broader Backbone concept

The package is inspired by the broader Eden/Backbone framework, especially the idea that open possibility should not become arbitrary action. In software terms, a request should only become an executable action when the active policy structure allows it.

That conceptual inspiration is useful, but the package boundary remains practical:

- It implements governance-oriented software primitives.
- It treats ASI as Accountable Systems Infrastructure.
- It does not implement artificial superintelligence.
- It does not prove the Eden Hypothesis or any theory of intelligence.
- It does not replace legal review, AI safety governance, organizational accountability, or operational security.

## Related documentation

- [Getting Started](getting-started.md)
- [Core Domain Language](core-domain-language.md)
- [Policy Evaluator Pipeline](policy-evaluator-pipeline.md)
- [Enterprise Adoption Personas](enterprise-adoption-personas.md)
- [Adoption and Target Use Cases](use-cases.md)
- [EF Core Integration Boundary](ef-core-integration-boundary.md)
- [Plain ASP.NET Core Host Sample](plain-aspnetcore-host-sample.md)
