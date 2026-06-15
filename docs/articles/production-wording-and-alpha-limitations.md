# Production Wording and Alpha Limitations

This article defines the documentation wording boundary for current AsiBackbone capabilities, signing-ready design, durable local/outbox behavior, optional provider integrations, alpha or preview packages, and future production-grade tamper-evidence.

Issue: #148.

In this software project, **ASI** means **Accountable Systems Infrastructure**. AsiBackbone is a governance spine for consequential software decision flow. It is not an artificial superintelligence implementation, AI model host, robot controller, signing product, compliance product, immutable ledger, cloud governance platform, or legal guarantee by itself.

> [!IMPORTANT]
> Documentation should describe only implemented and released behavior as current behavior. Signing, verification, key rotation, managed-key providers, external anchoring, immutable storage, robotics gateways, and cloud governance integrations must be described as optional, preview, future, provider-specific, sample-only, or host-owned until they are implemented and released.

## Current-stage wording rules

| Topic | Preferred wording | Avoid wording |
| --- | --- | --- |
| Project identity | `Accountable Systems Infrastructure`, governance spine, decision governance infrastructure | artificial superintelligence implementation, AI model, ASI engine |
| Current audit records | audit residue, audit ledger record, durable local/outbox record when configured | immutable ledger, tamper-proof record, legal evidence guarantee |
| Signing model | signing-ready metadata, provider-neutral signing seam, future signing provider | cryptographically signed by default, non-repudiable by default |
| Tamper-evidence | tamper-evident-ready, or able to support future tamper-evidence after signing, verification, key-management, and storage controls are implemented | tamper-evident, tamper-proof, immutable by default |
| Provider integrations | optional provider package, host adapter, downstream emission or enrichment | required platform, Core dependency, authoritative audit store by default |
| Cloud observability | operational search, alerting, dashboards, and diagnostics after local persistence | substitute for durable local accountability |
| Purview | optional governance, catalog, classification, and lineage enrichment | raw audit ledger by default |
| Robotics or gateway work | future or sample gateway pattern unless released | direct ASI-to-robot control implementation |

## Required qualifiers for future or provider work

Use explicit qualifiers when behavior is not part of the currently released stable package surface:

- `planned`;
- `optional`;
- `preview`;
- `sample-only`;
- `host-owned`;
- `provider-specific`;
- `future provider package`;
- `when configured`;
- `after durable local persistence`;
- `after signing, verification, key-management, and storage guarantees are implemented`.

Avoid wording that implies future provider work is required for Core consumers.

## Tamper-evidence wording boundary

A record may be described as **signing-ready** when it carries stable identifiers, policy version, policy hash, schema version, timestamps, and signing metadata fields that can support later signing.

A record may be described as **signed** only when a concrete signer has produced a signature over a deterministic payload or hash and the signature metadata is stored with the record.

A record may be described as **verified** only when a verification service checks the signature against the expected payload or hash, algorithm, key identity, key version, and verification policy.

A record should not be described as **tamper-evident** unless documentation also identifies:

- deterministic payload hashing or canonicalization;
- signing provider and key protection model;
- verification behavior;
- key rotation and retired-key verification policy;
- durable storage expectations;
- chain, anchoring, or continuity behavior if deletion, reordering, or external proof is claimed;
- host operational controls.

When those pieces are not implemented, use **signing-ready** or **tamper-evident-ready** instead.

## Durable local accountability

External providers can enrich, search, alert, stream, classify, catalog, or display governance events. They should not be described as replacing durable local accountability unless the host explicitly designs and documents them as the authoritative audit store.

Default wording should preserve this sequence:

```text
Decision or acknowledgment
  -> audit residue / governance record
  -> durable local store or outbox when configured
  -> optional provider emission
  -> provider-specific enrichment, search, alerting, or cataloging
```

Recommended language:

- preserve local audit/outbox records before provider emission;
- emit minimized envelopes to external systems;
- treat provider failures as retry, defer, quarantine, or escalate conditions;
- keep provider packages optional and downstream of Core.

## Package dependency language

Core remains provider-neutral. Provider packages depend on Core. Core must not depend on OpenTelemetry, Azure Monitor, Event Hubs, Purview, SIEM, signing providers, cloud SDKs, robotics SDKs, or immutable storage systems.

Appropriate wording:

- `CDCavell.AsiBackbone.Core` defines provider-neutral governance primitives and seams.
- Provider packages or host adapters translate Core records into provider-specific systems.
- Provider packages are optional integration surfaces.
- Host applications choose whether to install and configure a provider package.

Avoid wording that implies:

- Core requires a cloud provider;
- Core requires a certificate or managed key;
- OpenTelemetry, Azure Monitor, Event Hubs, Purview, or SIEM replaces local accountability;
- provider-specific packages define Core semantics.

## Alpha, preview, and sample language

Use alpha, preview, or sample wording for APIs or packages that are not part of the stable package family.

Do not imply that an alpha, preview, or sample package:

- is production-hardened;
- is covered by the same compatibility promise as stable packages;
- provides legal, compliance, cryptographic, or operational guarantees;
- is required to use Core;
- makes AsiBackbone a completed ASI implementation.

Stable documentation may discuss alpha history, but should keep historical alpha limitations separate from current stable behavior and future provider planning.

## Release and PR checklist

Before merging documentation for provider or security work, confirm:

- current behavior is separated from planned or future behavior;
- tamper-evidence claims are tied to implemented features;
- provider dependencies remain outside Core;
- durable local/outbox persistence remains the reliability baseline before provider emission;
- host responsibilities for security, privacy, retention, and compliance are explicit;
- no wording claims AsiBackbone implements artificial superintelligence, controls robots, or replaces governance, legal, security, or operational review.

## Related documentation

- [Privacy and Signing Boundaries](privacy-and-signing-boundaries.md)
- [Signing-Ready Receipts and Key Handling](signing-ready-receipts-and-key-handling.md)
- [Observability and Governance Emission Architecture](observability-and-governance-emission-architecture.md)
- [Governance Emission Contract](governance-emission-contract.md)
- [Durable Audit and Outbox Persistence](durable-audit-outbox-persistence.md)
- [API Compatibility and SemVer](api-compatibility-and-semver.md)
