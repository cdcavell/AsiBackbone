# Glossary

This glossary maps AsiBackbone domain language to everyday software architecture concepts.

The terms below are intended to help first-time readers understand the package as practical governance infrastructure: a decision boundary around consequential software actions, not an AI model host, compliance guarantee, or execution engine.

> [!IMPORTANT]
> Terms such as responsibility handshake or liability handshake describe an accountability workflow inside the software model. They do not create legal protection, legal advice, regulatory compliance, or a substitute for organizational review.

## Core glossary

| AsiBackbone term | Plain-language meaning | Developer analogy | Related API/type | Example use |
| --- | --- | --- | --- | --- |
| Governance spine | The structured path a consequential action follows before the host executes it. | A policy pipeline between request intake and execution. | [`IAsiBackbonePolicyEvaluator<TContext>`](../../src/CDCavell.AsiBackbone.Core/Evaluation/IAsiBackbonePolicyEvaluator.cs) | Route a proposed admin action through constraints before the host performs it. |
| Intent or request | The proposed action that needs evaluation. | A command, tool call, workflow request, or service operation. | [`AsiBackboneConstraintEvaluationContext`](../../src/CDCavell.AsiBackbone.Core/Constraints/AsiBackboneConstraintEvaluationContext.cs) | An agent proposes `notification.send` or a user proposes `workflow.approve`. |
| Actor context | Who or what is requesting the action. | Current user, service account, system actor, or agent identity. | [`IAsiBackboneActorContext`](../../src/CDCavell.AsiBackbone.Core/Actors/IAsiBackboneActorContext.cs) | Map a host user or service into a framework-neutral actor. |
| Policy context | The data used to evaluate the request. | A DTO containing actor, operation, risk, region, policy, and correlation metadata. | [`IAsiBackboneConstraintEvaluationContext`](../../src/CDCavell.AsiBackbone.Core/Constraints/IAsiBackboneConstraintEvaluationContext.cs) | Include `risk=high`, `region=south`, and `operation=file.delete`. |
| Constraint evaluation | A rule checks whether the request is acceptable under policy. | Authorization requirement, business rule, risk gate, or validation rule. | [`IAsiBackboneConstraint<TContext>`](../../src/CDCavell.AsiBackbone.Core/Constraints/IAsiBackboneConstraint.cs), [`ConstraintEvaluationResult`](../../src/CDCavell.AsiBackbone.Core/Constraints/ConstraintEvaluationResult.cs) | Deny protected resources or warn when metadata is incomplete. |
| Decision policy | The composition rule that can raise or reshape the final decision after constraints run. | A final policy combiner after individual validators report results. | [`IAsiBackboneDecisionPolicy<TContext>`](../../src/CDCavell.AsiBackbone.Core/Evaluation/IAsiBackboneDecisionPolicy.cs) | Convert a high-risk allowed result into `AcknowledgmentRequired`. |
| Decision result | The outcome returned by the governance evaluation. | A structured result object from a policy engine. | [`GovernanceDecision`](../../src/CDCavell.AsiBackbone.Core/Decisions/GovernanceDecision.cs) | Return outcome, reason codes, policy version, policy hash, and correlation ID. |
| Allowed | The request may proceed if the host still chooses to execute it. | Success result from the policy boundary. | [`GovernanceDecisionOutcome`](../../src/CDCavell.AsiBackbone.Core/Decisions/GovernanceDecisionOutcome.cs) | Allow a low-risk notification after audit residue is written. |
| Warning | The request may proceed, but warning reasons should be retained or shown. | Successful validation with warnings. | [`GovernanceDecisionOutcome`](../../src/CDCavell.AsiBackbone.Core/Decisions/GovernanceDecisionOutcome.cs) | Allow a workflow step while recording that optional metadata was missing. |
| Denied | The request should not proceed. | Authorization failure or policy rejection. | [`GovernanceDecisionOutcome`](../../src/CDCavell.AsiBackbone.Core/Decisions/GovernanceDecisionOutcome.cs) | Deny deletion of a protected file path. |
| Deferred | The request should be evaluated later or by another process. | Pending state or retry-after decision. | [`GovernanceDecisionOutcome`](../../src/CDCavell.AsiBackbone.Core/Decisions/GovernanceDecisionOutcome.cs) | Defer when a required external policy feed is unavailable. |
| Acknowledgment required | A human accountability step is required before the host may proceed. | Confirmation challenge for a high-impact action. | [`IAsiBackboneAcknowledgmentChallengeService`](../../src/CDCavell.AsiBackbone.AspNetCore/Handshakes/IAsiBackboneAcknowledgmentChallengeService.cs) | Ask an administrator to acknowledge risk before a sensitive override. |
| Escalation recommended | The request should move to a higher review path before execution. | Manual review queue or supervisor approval path. | [`GovernanceDecisionOutcome`](../../src/CDCavell.AsiBackbone.Core/Decisions/GovernanceDecisionOutcome.cs) | Route a high-risk workflow approval to a compliance or operations reviewer. |
| Audit residue | The structured trace left by a decision flow. | Audit event payload or decision receipt data. | [`AuditResidue`](../../src/CDCavell.AsiBackbone.Core/Audit/AuditResidue.cs) | Preserve actor, operation, outcome, reason codes, policy version, and correlation ID. |
| Audit receipt | A human-readable way to describe the retained decision evidence. | Receipt for a governed operation. | [`AuditResidue`](../../src/CDCavell.AsiBackbone.Core/Audit/AuditResidue.cs) | Show a support reviewer why a request required acknowledgment. |
| Audit ledger | A durable collection of decision records. | Append-only audit table or event ledger. | [`AuditLedgerRecord`](../../src/CDCavell.AsiBackbone.Core/Audit/AuditLedgerRecord.cs), [`IAsiBackboneAuditLedgerStore`](../../src/CDCavell.AsiBackbone.Core/Audit/IAsiBackboneAuditLedgerStore.cs) | Store decision records through an EF Core-backed host database. |
| Audit sink | The component that receives audit residue. | Logging sink, event sink, or audit writer. | [`IAsiBackboneAuditSink`](../../src/CDCavell.AsiBackbone.Core/Audit/IAsiBackboneAuditSink.cs) | Write decision residue to an in-memory ledger during sample validation. |
| Responsibility or liability handshake | A structured request-and-acknowledgment workflow around consequential action. | A confirm-and-record accountability workflow. | [`LiabilityHandshakeRequest`](../../src/CDCavell.AsiBackbone.Core/Handshakes/LiabilityHandshakeRequest.cs), [`LiabilityHandshakeAcknowledgment`](../../src/CDCavell.AsiBackbone.Core/Handshakes/LiabilityHandshakeAcknowledgment.cs) | Require the actor to acknowledge a high-risk action before the host proceeds. |
| Acknowledgment | The actor's response to a challenge. | Accepted confirmation response with metadata. | [`LiabilityHandshakeAcknowledgment`](../../src/CDCavell.AsiBackbone.Core/Handshakes/LiabilityHandshakeAcknowledgment.cs), [`AsiBackboneAcknowledgmentChallenge`](../../src/CDCavell.AsiBackbone.AspNetCore/Handshakes/AsiBackboneAcknowledgmentChallenge.cs) | Capture who accepted the challenge and when. |
| Capability token | A scoped grant that represents limited permission to continue. | Short-lived delegated permission or operation-specific permit. | [`AuditLedgerRecord`](../../src/CDCavell.AsiBackbone.Core/Audit/AuditLedgerRecord.cs) | Record a token reference showing that a host may continue a specific operation. |
| Policy version | A readable identifier for the policy generation used in evaluation. | Version string for the active ruleset. | [`GovernanceDecision`](../../src/CDCavell.AsiBackbone.Core/Decisions/GovernanceDecision.cs) | Store `agent-gateway-v1` with the decision record. |
| Policy hash | A stable fingerprint of the effective policy content or configuration. | Checksum for a ruleset snapshot. | [`GovernanceDecision`](../../src/CDCavell.AsiBackbone.Core/Decisions/GovernanceDecision.cs) | Compare two decisions to see whether they used the same effective policy shape. |
| Reason codes | Machine-readable explanations for a result. | Error codes or validation codes for governance decisions. | [`GovernanceDecision`](../../src/CDCavell.AsiBackbone.Core/Decisions/GovernanceDecision.cs), [`ConstraintEvaluationResult`](../../src/CDCavell.AsiBackbone.Core/Constraints/ConstraintEvaluationResult.cs) | Return `risk.high.requires_acknowledgment` for a consequential request. |
| Regional or local constraints | Policy rules that vary by region, agency, tenant, environment, or program. | Tenant-specific authorization or jurisdiction-aware business rules. | [`IAsiBackboneConstraint<TContext>`](../../src/CDCavell.AsiBackbone.Core/Constraints/IAsiBackboneConstraint.cs) | Apply one approval rule for one region and a stricter one for another. |
| Operational gateway | The host-owned boundary that decides whether a governed result becomes real execution. | Adapter or facade in front of external tools, APIs, devices, or workflow systems. | [AI Agent Gateway Scenario](scenarios/ai-agent-gateway.md) | Let an agent propose a tool call while the host remains execution authority. |
| Correlation ID | The identifier used to connect related decision, audit, log, and workflow records. | Request ID or operation ID. | [`AsiBackboneHttpRequestCorrelation`](../../src/CDCavell.AsiBackbone.AspNetCore/Correlation/AsiBackboneHttpRequestCorrelation.cs) | Find all audit records produced by one HTTP request. |
| Operation result | Whether a package operation succeeded as software, separate from policy outcome. | `Result<T>` or service execution status. | [`OperationResult`](../../src/CDCavell.AsiBackbone.Core/Results/OperationResult.cs) | Distinguish a policy denial from a missing-input or invalid-state error. |

## Usage notes

The glossary favors implementation-grounded language. Broader framework terms can still appear in conceptual documentation, but API and adoption guidance should make clear where the software boundary actually is.

Useful starting points:

- [Core Domain Language and Alpha Boundary](core-domain-language.md)
- [Policy Evaluator Pipeline](policy-evaluator-pipeline.md)
- [Adoption and Target Use Cases](use-cases.md)
- [Framework-Neutral Integration and Host-Owned Persistence](integration-boundaries.md)
- [AI Agent Gateway Scenario](scenarios/ai-agent-gateway.md)
