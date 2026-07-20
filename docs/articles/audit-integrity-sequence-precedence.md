# Audit Integrity Sequence Precedence

This note defines how `AuditIntegrityVerifier.Verify(...)` classifies competing sequence and hash-link failures.

The verifier evaluates links in the order supplied by the host. It does not sort, repair, or infer missing links.

## Sequence ownership

The outer verification loop is the single owner of accepted sequence tracking:

```text
validate current link
  -> if validation fails, return the failure
  -> record the accepted sequence
  -> advance expected sequence and previous hash
```

`VerifyLink(...)` only checks whether the candidate sequence has already been accepted. It does not temporarily add or remove sequence values.

A sequence is added to the observed set only after the link passes algorithm, chain ID, sequence continuity, genesis, previous-hash, and canonical link-hash validation.

## Failure precedence

For each candidate link, the verifier applies this order:

1. unsupported hash algorithm;
2. wrong chain ID;
3. sequence already accepted;
4. sequence continuity;
5. genesis previous-hash rule;
6. previous-link hash;
7. canonical link hash.

This produces the following sequence semantics:

| Condition | Category | Failure code |
| --- | --- | --- |
| Candidate sequence was already accepted earlier | `ForkedChain` | `integrity.sequence-duplicate` |
| Candidate sequence is greater than the next expected sequence | `MissingRecord` | `integrity.sequence-missing` |
| Candidate sequence is less than the next expected sequence but was not previously accepted | `ReorderedRecord` | `integrity.sequence-reordered` |

Duplicate detection intentionally precedes continuity checks. Therefore, an adjacent duplicate and a non-adjacent duplicate both produce `ForkedChain`, even though the later duplicate is also numerically behind the expected sequence.

A unique backward sequence remains reachable when verifying a partial chain with `requireGenesis: false`. For example, a supplied sequence beginning at `5` followed by a unique sequence `4` is classified as `ReorderedRecord`.

## Fork interpretation

`ForkedChain` means that more than one supplied link claims a sequence already accepted for the same verification pass. The links may be byte-for-byte duplicates or may contain conflicting record IDs, hashes, or link metadata.

The category does not prove which branch is authoritative. Hosts must resolve that question using durable storage history, signed checkpoints, chain-tip records, external anchors, or operational review.

## Stable behavior

The public failure codes remain unchanged:

- `integrity.sequence-duplicate`;
- `integrity.sequence-missing`;
- `integrity.sequence-reordered`;
- `integrity.previous-link-hash-mismatch`;
- `integrity.link-hash-mismatch`.

Valid continuous-chain verification behavior is unchanged.

## Host guidance

Supply links in the order retained by the authoritative audit store. Do not sort untrusted input before verification, because sorting can hide evidence that records were delivered or retained out of order.

Treat `ForkedChain`, `MissingRecord`, `ReorderedRecord`, and hash failures as integrity signals requiring host-owned review. AsiBackbone reports the condition but does not repair the chain or select a winning branch.
