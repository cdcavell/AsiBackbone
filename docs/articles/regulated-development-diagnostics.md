# Regulated Development Diagnostics

Regulated, shared, and high-sensitivity development environments must set `IncludeDevelopmentDiagnosticsMetadataValues = false`. The strict governance profile applies this setting.

Metadata keys remain available while every value is replaced with `[redacted]`. Keep diagnostics opt-in and Development-only. This setting does not sanitize reason messages; keep those messages generic and free of sensitive content.
