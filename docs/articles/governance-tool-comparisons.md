# Governance Tool Comparisons

This article compares Azure Policy, Open Policy Agent (OPA), Microsoft Agent Governance Toolkit, and AsiBackbone as complementary governance tools. It is not a competitive ranking and should not be read as criticism of any tool.

The practical guidance is simple:

| Need | Better starting point |
| --- | --- |
| Azure resource-plane governance, compliance assessment, and remediation | Azure Policy |
| A mature, language-neutral policy decision engine | Open Policy Agent |
| Governance around autonomous AI agent tool calls, delegation, identity, sandboxing, and agent operations | Microsoft Agent Governance Toolkit |
| Actor-accountable consequential-action governance inside a .NET application, including acknowledgment, capability grants, audit residue, and gateway execution boundaries | AsiBackbone |

> [!IMPORTANT]
> In this repository, **ASI** means **Accountable Systems Infrastructure**. AsiBackbone is not an artificial superintelligence implementation, AI model host, AI training framework, legal-compliance guarantee, or replacement for cloud governance, policy decision engines, agent runtimes, or security platforms.

## The short version

Use the tool that owns the boundary being governed.

- Use **Azure Policy** when the question is whether Azure resources and Azure resource actions comply with organizational rules.
- Use **OPA** when the question is whether a domain-neutral policy decision engine should evaluate structured input and return a decision to an enforcement point.
- Use **Microsoft Agent Governance Toolkit** when the question is whether autonomous AI agent tool calls, delegations, identities, sandboxes, and agent operations need deterministic governance.
- Use **AsiBackbone** when the question is whether a .NET application should turn a proposed consequential action into an explicit, actor-accountable governance decision before execution.

The clean architecture is often not one tool instead of another. It is layered governance: infrastructure policy, decision policy, agent policy, and application accountability each happen at their proper boundary.

## Boundary comparison

| Tool | Primary boundary | Main question answered | Common output |
| --- | --- | --- | --- |
| Azure Policy | Azure resource plane | Is this Azure resource state or Azure resource action compliant with assigned business rules? | Compliance state, deny/modify/audit/deploy-style platform effect, remediation path |
| OPA | Policy decision point | Given structured input and policy, what decision should the caller enforce? | Structured policy decision, often allow/deny plus supporting data |
| Microsoft Agent Governance Toolkit | AI agent and tool-call boundary | Is this agent action, tool call, delegation, or runtime behavior allowed under agent governance policy? | Allowed or denied tool call, governance record, identity/trust/audit signal |
| AsiBackbone | .NET application consequential-action boundary | Should this actor-proposed operation be allowed, warned, denied, deferred, acknowledged, or escalated before host execution? | `GovernanceDecision`, reason codes, policy version/hash, acknowledgment requirement, audit residue, optional capability grant |

## Azure Policy

Azure Policy is the right fit for Azure resource-plane governance.

Use it when the organization needs to:

- enforce resource consistency across Azure;
- assess compliance at scale;
- require tags, allowed locations, logging settings, resource types, or SKUs;
- apply platform effects such as deny, audit, modify, deploy related resources, or block actions;
- remediate existing or newly created Azure resources.

Azure Policy is not primarily an application-level responsibility handshake. It does not replace application audit receipts, user acknowledgment workflows, host-specific operation decisions, or capability-scoped execution inside a .NET application.

A typical composition is:

```text
Azure Policy governs the Azure environment.
AsiBackbone governs consequential actions inside the .NET application running in that environment.
```

## Open Policy Agent

OPA is the right fit when the system needs a mature, general-purpose policy decision engine.

Use it when the organization needs to:

- centralize policy decision logic across different platforms;
- evaluate structured input such as JSON;
- use policy-as-code with Rego;
- keep policy decision-making separate from policy enforcement;
- return structured decisions to microservices, API gateways, Kubernetes, CI/CD, infrastructure-as-code, or custom applications.

OPA is intentionally broad. That breadth is its strength. It can be used beside AsiBackbone when a .NET host wants OPA to decide a policy question and AsiBackbone to compose the application governance result.

A typical composition is:

```text
Host application builds policy input
  -> OPA evaluates domain policy
  -> AsiBackbone composes the governance decision
  -> Host persists residue, handles acknowledgment, and controls execution
```

In that pattern, OPA can answer a policy question, while AsiBackbone can preserve actor context, reason codes, policy version/hash, acknowledgment status, capability grants, and audit residue in the host application's native decision flow.

## Microsoft Agent Governance Toolkit

Microsoft Agent Governance Toolkit is the right fit when the immediate concern is autonomous AI agent governance.

Use it when the organization needs to:

- intercept agent tool calls before they reach external systems;
- govern multi-agent delegation or agent identity;
- add agent-oriented policy, sandboxing, trust, audit, SRE, or compliance surfaces;
- integrate governance with agent frameworks and tool-call middleware;
- make denied agent actions structurally impossible at the agent tool boundary.

This is a different boundary from AsiBackbone. Agent Governance Toolkit sits close to the autonomous agent runtime and tool-call path. AsiBackbone sits at the .NET host application's consequential-action boundary.

A typical composition is:

```text
Agent Governance Toolkit governs the agent's tool-call boundary.
AsiBackbone governs the host application's actor-accountable consequential action.
```

For example, an agent runtime may use Agent Governance Toolkit to determine whether an agent may attempt a tool call at all. If the tool call reaches a .NET business application where a real administrative or external action may occur, that host can use AsiBackbone to require policy evaluation, acknowledgment, audit residue, and a capability-scoped execution boundary before the host performs the operation.

## AsiBackbone

AsiBackbone is the right fit when a .NET application needs a governance spine for consequential actions.

Use it when the application needs to:

- build a policy context around a proposed action;
- evaluate local, regional, organizational, or host-specific constraints;
- produce explicit decision outcomes such as `Allowed`, `Warning`, `Denied`, `Deferred`, `AcknowledgmentRequired`, or `EscalationRecommended`;
- preserve reason codes, policy version, policy hash, correlation identifiers, actor context, and operation metadata;
- require an acknowledgment or responsibility handshake before consequential execution;
- issue a short-lived capability grant for tightly scoped follow-on execution;
- leave durable audit residue, with signing or signature metadata where the host or integration package supports it;
- enforce a gateway boundary before external systems, administrative workflows, AI tools, or simulated/physical operations are executed by host-owned code.

AsiBackbone does not need to replace Azure Policy, OPA, or Agent Governance Toolkit. It can complement them by giving .NET applications a native accountability surface around the moment where intent becomes host action.

## What each tool should not be asked to do

| Tool | Do not ask it to be |
| --- | --- |
| Azure Policy | A full application acknowledgment workflow, general policy engine for arbitrary host operations, or .NET governance spine. |
| OPA | The complete host accountability layer, persistence model, acknowledgment UX, capability-token issuer, or execution gateway by itself. |
| Microsoft Agent Governance Toolkit | A substitute for cloud resource compliance, every application-specific business workflow, or the final host-owned execution boundary. |
| AsiBackbone | A cloud governance platform, OPA replacement, AI agent framework, AI model runtime, legal guarantee, or artificial superintelligence implementation. |

## Layered example

A regulated .NET system that uses AI agents and runs on Azure could use all four tools without conflict.

```text
Azure Policy
  Governs Azure resources, allowed regions, diagnostic settings, and cloud compliance.

OPA
  Evaluates shared organization policy as a language-neutral decision engine.

Microsoft Agent Governance Toolkit
  Intercepts autonomous agent tool calls, identity, delegation, and sandbox concerns.

AsiBackbone
  Governs the .NET application's consequential action boundary with actor context,
  acknowledgment, reason codes, durable audit residue, optional capability grants,
  and gateway-safe execution.
```

This keeps each tool in its strongest lane.

## Decision guide

Choose **Azure Policy** first when the primary object being governed is an Azure resource, Azure resource action, Azure compliance state, or Azure remediation workflow.

Choose **OPA** first when the primary need is a mature, domain-neutral policy decision point that can be queried by many enforcement points.

Choose **Microsoft Agent Governance Toolkit** first when the primary risk is autonomous agent behavior: tool calls, multi-agent delegation, agent identity, runtime sandboxing, prompt-injection exposure, or agent operational controls.

Choose **AsiBackbone** first when the primary risk is a consequential action inside a .NET host application and the application needs an accountable decision record before execution.

## Complementary adoption patterns

### Azure Policy plus AsiBackbone

Use Azure Policy to keep the Azure environment compliant. Use AsiBackbone to govern what application actors, services, or agents may do inside the application.

### OPA plus AsiBackbone

Use OPA as a policy decision point. Use AsiBackbone to normalize the host governance outcome, require acknowledgment when needed, persist audit residue, and control capability-scoped execution.

### Agent Governance Toolkit plus AsiBackbone

Use Agent Governance Toolkit to prevent unsafe agent tool calls. Use AsiBackbone when those tool calls cross into .NET application operations that need actor-accountable acknowledgment, audit, and gateway enforcement.

### All four together

Use Azure Policy for cloud resource governance, OPA for shared policy decisions, Agent Governance Toolkit for agent runtime/tool governance, and AsiBackbone for host-level consequential-action governance.

## Source links

- [Azure Policy overview](https://learn.microsoft.com/azure/governance/policy/overview)
- [Open Policy Agent documentation](https://www.openpolicyagent.org/docs)
- [Microsoft Agent Governance Toolkit](https://github.com/microsoft/agent-governance-toolkit)
- [Why AsiBackbone?](why-asi-backbone.md)
- [AI Agent Gateway Scenario](scenarios/ai-agent-gateway.md)
- [Policy Evaluator Pipeline](policy-evaluator-pipeline.md)
- [Core Domain Language and Alpha Boundary](core-domain-language.md)
