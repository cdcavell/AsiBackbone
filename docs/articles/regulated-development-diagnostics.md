# Regulated Development Diagnostics

For regulated, shared, or high-sensitivity development environments, keep diagnostics opt-in and set `IncludeDevelopmentDiagnosticsMetadataValues = false`. This retains metadata keys for troubleshooting while replacing every value with `[redacted]`.

```csharp
builder.Services.Configure<AsiBackboneEndpointGovernanceOptions>(options =>
{
