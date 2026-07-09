# Follow-up: first-denial warning/denial composition allocation

## Context

The July 2026 performance code review found that `policy.first_denial_short_circuit` can be slower and more allocating than `policy.warning_and_denial_full` when a warning is accumulated before the first denial.

The suspected hot path is `DefaultAsiBackbonePolicyEvaluator.Compose()`, where the short-circuit warning-plus-denial path calls `GovernanceDecision.Deny(warnings.Concat(denials), ...)`. The current accumulator concatenation is iterator-based, which means `GovernanceDecision.NormalizeReasons(...)` cannot use its `ICollection<OperationReason>` fast path.

## Proposed direction

Replace iterator-based warning/denial concatenation with a sized collection path that avoids the compiler-generated iterator and allows downstream reason normalization to take the same collection-backed path used by other decision-composition branches.

One possible shape:

- Add an accumulator helper that materializes the combined warning-plus-denial reasons into a sized array or read-only list.
- Use that helper only for the `ShortCircuitOnFirstDenial` warning-plus-denial branch.
- Preserve warning-before-denial reason ordering.
- Preserve public API shape.

## Acceptance criteria

- `ShortCircuitOnFirstDenial = true` still preserves warnings produced before the first denial.
- Constraints after the first denial remain skipped.
- No skipped constraint warning or denial reason appears in the final decision.
- No public API change is required unless separately justified.
- BenchmarkDotNet results are captured before/after using the policy evaluator allocation benchmarks and the first-denial expensive-tail benchmarks.
