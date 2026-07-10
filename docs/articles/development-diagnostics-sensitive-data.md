# Development Diagnostics Sensitive-Data Guidance

Endpoint governance diagnostics are development-gated, but that boundary does not sanitize host-supplied reason messages or metadata values.

Reason messages must remain generic. Never include secrets, credentials, raw tokens, raw prompts, user-provided sensitive text, PII, PHI, legal details, or confidential business data. Put machine-readable specificity in a stable reason code.
