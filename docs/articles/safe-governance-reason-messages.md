# Safe Governance Reason Messages

Prefer stable reason codes and generic messages.

Safe: `policy.region.denied` — `The requested operation is not permitted.`

Unsafe: `Denied for patient Jane Doe; token abc123.`

Never place secrets, credentials, raw tokens, raw prompts, user text, PII, PHI, legal details, or confidential data in reason messages.