# CDCavell.AsiBackbone.OpenTelemetry

`CDCavell.AsiBackbone.OpenTelemetry` adapts provider-neutral AsiBackbone governance emission envelopes into OpenTelemetry-friendly .NET diagnostics primitives.

The package implements `IAsiBackboneGovernanceEmitter` without making `CDCavell.AsiBackbone.Core` depend on OpenTelemetry exporters, Azure SDKs, SIEM SDKs, Event Hubs, Purview, robotics packages, or cloud-provider dependencies.

## Boundary

This package emits governance envelopes through:

- `ActivitySource` activity events and tags;
- `Meter` counters and histograms;
- provider-neutral `GovernanceEmissionResult` and `GovernanceEmissionError` outcomes.

It does not configure exporters. Hosts can route the diagnostics pipeline to Azure Monitor, Datadog, Grafana, Splunk, Elastic, or another backend through normal host-owned OpenTelemetry configuration.

## Recommended flow

```text
GovernanceEmissionEnvelope
  -> durable governance outbox
  -> AsiBackboneGovernanceOutboxDrain
  -> OpenTelemetryGovernanceEmitter
  -> ActivitySource / Meter
  -> host-configured OpenTelemetry exporters
```

The durable audit/outbox record remains the accountability baseline. OpenTelemetry is a projection path, not the authoritative ledger.
