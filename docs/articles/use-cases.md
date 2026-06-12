# Adoption and Target Use Cases

AsiBackbone is most useful when a host application needs an explicit governance boundary between proposed intent and host-owned execution.

It is not a general application framework, AI model host, agent runtime, robotics controller, infrastructure automation tool, or compliance guarantee. It provides governance primitives that help a host application evaluate intent, apply policy, require acknowledgment when appropriate, preserve audit residue, and optionally scope follow-on execution through capability tokens.

> [!IMPORTANT]
> AsiBackbone does not execute AI, robotics, infrastructure, physical-control, or external-system commands by itself. The consuming host remains responsible for execution behavior, operational safeguards, authentication, authorization, persistence, deployment, and compliance review.

## Adoption fit

AsiBackbone is a good candidate when an action needs more than a simple role check.

Good signals include:

- The action is consequential enough to require a structured decision record.
- The decision depends on policy, region, risk, actor, resource, or host context.
- A reviewer may later need to understand why the system allowed, denied, deferred, required acknowledgment, or recommended escalation.
- The host needs reason codes, policy version, policy hash, correlation ID, and metadata captured as audit residue.
- The action may need human acknowledgment before the host proceeds.
- Follow-on execution should be represented as a short-lived, scoped capability grant rather than broad authority.

AsiBackbone may be too much for low-risk CRUD screens, simple role checks, unconstrained internal tools, or applications that only need ordinary logging.

## Why not ad hoc authorization checks?

Ad hoc authorization checks usually answer a narrow question: can this actor perform this operation?

AsiBackbone focuses on a broader decision boundary:

```text
Proposed intent
  -> Build policy context
  -> Evaluate constraints
  -> Compose decision result
  -> Require acknowledgment when needed
  -> Preserve audit residue
  -> Issue optional scoped capability token
  -> Host application decides whether and how to execute
```

That boundary makes policy evaluation easier to test, document, replay, and review. It also keeps consequential decision logic from being scattered across controllers, services, background jobs, and one-off logging statements.

## Why not plain application logging?

Plain logs are useful, but they often capture operational events after or during execution.

AsiBackbone is designed to capture the structured decision before execution. A decision receipt can preserve what was requested, who requested it, which constraints were evaluated, which outcome was selected, whether acknowledgment was required, and which policy version shaped the result.

Logs may still be used by the host application. AsiBackbone does not replace them. It adds a governance record that is closer to the decision itself.

## Current and core use cases

The following use cases fit the current package direction because they rely on policy-driven decision gating, acknowledgment, audit residue, and capability-scoped execution while leaving real execution in the consuming host.

### AI agent gateways before tool or API execution

A host application may let an AI agent propose a tool call, API request, workflow action, or data operation. AsiBackbone can sit between the proposal and execution.

Potential flow:

1. The agent proposes an action.
2. The host builds an AsiBackbone policy context.
3. Constraints evaluate actor, target resource, risk, region, operation type, and policy version.
4. AsiBackbone returns a decision such as `Allow`, `Deny`, `Defer`, `RequireAcknowledgment`, or `Escalate`.
5. The host decides whether and how to execute the tool call.

This keeps the agent from becoming the execution authority. The host remains the boundary owner.

### Human-in-the-loop consequential workflows

Some operations should pause until a human acknowledges risk, responsibility, or intent.

Examples include:

- Approving a high-impact administrative change.
- Submitting a sensitive workflow step.
- Releasing a document or decision that requires accountability.
- Continuing after a policy warning.

AsiBackbone can represent the acknowledgment requirement as part of the decision rather than burying it in UI-specific code.

### High-compliance enterprise microservices

Enterprise services often need traceable decision flow across service boundaries.

AsiBackbone may help when a microservice must preserve reason codes, policy versions and hashes, correlation IDs, actor context, risk category, decision metadata, and audit residue suitable for later review.

The package does not guarantee regulatory compliance, but it can provide a consistent governance model that regulated hosts can adapt to their own compliance program.

### Automated infrastructure or deployment gating

A platform team may want a policy gate before a host performs an infrastructure or deployment action.

Examples include promoting a deployment, running a privileged automation job, applying a configuration change, or triggering a maintenance workflow.

AsiBackbone should not be treated as the deployment engine. It can evaluate whether the host should allow, deny, defer, require acknowledgment, or escalate before the host calls its own automation system.

### Administrative approval workflows

Administrative systems often contain sensitive actions that deserve more structure than ordinary authorization.

Examples include role changes, account recovery actions, sensitive data access approvals, policy override requests, and manual exception handling.

AsiBackbone can help centralize the decision language around these actions while allowing the application to keep its own UI, storage, authentication, and operational rules.

### Simulated external-command validation

A safe early adoption path is simulated command validation.

The host can model an external command as an intent, evaluate it through AsiBackbone, persist the decision, and then stop before performing any real-world action. This is useful for demos, documentation, tests, and gateway design without introducing physical or infrastructure execution risk.

## Future integration scenarios

These scenarios are later-stage integration ideas. They should be treated as policy-gateway scenarios, not as current claims that AsiBackbone directly executes external or physical commands.

### Robotics or physical-control proxy scenarios

A future robotics or physical-control integration should preserve a hard separation between global strategy, regional policy, operational safety checks, and physical execution.

A safe conceptual flow is:

```text
High-level goal or request
  -> Regional or local policy evaluation
  -> Operational gateway validation
  -> Short-lived capability grant
  -> Host-owned controller decides whether execution is allowed
```

AsiBackbone may eventually support policy and audit primitives for this kind of gateway boundary, but it should not be described as a robotics controller. Robots, devices, hardware interlocks, safety governors, and physical execution systems remain outside the current package boundary.

### External system command brokers

Future gateway packages may adapt AsiBackbone decisions into brokered execution flows for external systems such as job schedulers, ticketing systems, deployment platforms, data pipelines, or device-management systems.

The important boundary remains the same: AsiBackbone evaluates and records the governance decision; the host or external controller owns execution.

## Sample paths

Useful starting points:

- [Getting Started](getting-started.md) for project orientation and the basic decision-flow model.
- [Why AsiBackbone?](why-asi-backbone.md) for the practical benefits overview.
- [Policy Evaluator Pipeline](policy-evaluator-pipeline.md) for the core evaluation path.
- [ASP.NET Core Integration Boundary](aspnetcore-integration-boundary.md) for host integration boundaries.
- [Plain ASP.NET Core Host Sample](plain-aspnetcore-host-sample.md) for the canonical in-repository validation sample.
- [NetCoreApplicationTemplate Host Validation](netcoreapplicationtemplate-host-validation.md) for optional external validation against a fuller enterprise-style application baseline.

## Practical first adoption

A team can start small:

1. Choose one consequential host action.
2. Define the intent and target resource.
3. Build a policy context with actor, risk, correlation, and policy metadata.
4. Add one or two constraints.
5. Persist the decision residue.
6. Require acknowledgment only when the decision requires it.
7. Keep execution fully owned by the host.

This lets a consuming application adopt AsiBackbone as a narrow governance spine before considering broader gateway or workflow integration.
