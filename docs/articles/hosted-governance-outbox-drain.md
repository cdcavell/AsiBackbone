# Hosted Governance Outbox Drain

Issue #198 adds host integration for running the provider-neutral governance outbox drain from ASP.NET Core or generic-host applications.

Core remains free of hosting, scheduling, ASP.NET Core, EF Core, OpenTelemetry, Azure, or provider-specific dependencies. The hosted worker lives in `AsiBackbone.AspNetCore` and resolves the Core drain through dependency injection.

## What the worker does

The hosted drain worker:

- resolves `AsiBackboneGovernanceOutboxDrain` from a scoped service provider;
- uses `IAsiBackboneGovernanceOutboxStore` to read pending and retry-ready outbox entries;
- uses `IAsiBackboneGovernanceEmitter` to attempt provider-neutral delivery;
- persists delivered, deferred, failed, retryable, or dead-letter transitions through the store;
- keeps provider selection outside Core and outside the worker itself.

The worker is intentionally an integration host, not an emitter provider. It can run with the no-op emitter for proof-path validation, with an in-memory store for development, or with durable EF Core storage and an OpenTelemetry-style emitter when those provider packages are available.

## No-op proof path

For local validation, tests, and samples, wire the worker with an outbox store and the provider-neutral no-op emitter:

```csharp
builder.Services.AddSingleton<IAsiBackboneGovernanceOutboxStore, InMemoryGovernanceOutboxStore>();
builder.Services.AddSingleton<IAsiBackboneGovernanceEmitter>(NoOpGovernanceEmitter.Instance);

builder.Services.AddAsiBackboneGovernanceOutboxDrainWorker(options =>
{
    options.BatchSize = 25;
    options.PollingInterval = TimeSpan.FromSeconds(15);
});
```

This path proves that queued governance emission envelopes can leave the outbox lifecycle without adding external telemetry, cloud, or exporter dependencies.

## Production-style path

A production host should normally use durable persistence and an actual governance emission provider:

```csharp
builder.Services.AddDbContext<AppDbContext>(/* host-owned EF Core configuration */);
builder.Services.AddScoped<IAsiBackboneGovernanceOutboxStore, EfCoreGovernanceOutboxStore>();
builder.Services.AddScoped<IAsiBackboneGovernanceEmitter, OpenTelemetryGovernanceEmitter>();

builder.Services.Configure<AsiBackboneGovernanceOutboxOptions>(options =>
{
    options.RetryDelay = TimeSpan.FromMinutes(2);
    options.DeferredDelay = TimeSpan.FromMinutes(5);
});

builder.Services.AddAsiBackboneGovernanceOutboxDrainWorker(options =>
{
    options.BatchSize = 100;
    options.PollingInterval = TimeSpan.FromSeconds(30);
    options.FailureDelay = TimeSpan.FromMinutes(1);
    options.RetryClock = () => DateTimeOffset.UtcNow;
});
```

The EF Core `DbContext`, migrations, provider SDKs, exporters, authentication, storage durability, and operational monitoring remain host responsibilities.

## Worker options

| Option | Default | Purpose |
| --- | ---: | --- |
| `Enabled` | `true` | Allows the worker to be registered but disabled by configuration. In scaled deployments, set this to `true` only for the selected worker role or partition owner unless the host has implemented durable claiming. |
| `BatchSize` | `100` | Maximum number of pending/retry-ready entries attempted per drain pass. |
| `PollingInterval` | `30s` | Delay between normal drain passes. |
| `FailureDelay` | `30s` | Delay after worker-level failures such as DI or storage exceptions. Provider failures returned through the emitter are still persisted by the Core drain. |
| `RetryClock` | `DateTimeOffset.UtcNow` | Clock source used for retry-ready lookups. Tests can replace this with a fixed clock. |
| `DrainOnShutdown` | `false` | Optionally attempts one final drain pass after the background loop has stopped. Do not enable this on many replicas unless duplicate drain behavior is understood. |
| `ShutdownDrainTimeout` | `5s` | Time budget for the optional shutdown drain. |

## Core outbox retry options

`AsiBackboneGovernanceOutboxOptions` controls retry timestamps persisted by the Core drain when the emitter does not supply a provider-specific retry-after value.

| Option | Default | Purpose |
| --- | ---: | --- |
| `RetryDelay` | `1m` | Delay applied after an unexpected emitter exception is converted into a retryable governance emission failure. |
| `DeferredDelay` | `1m` | Delay applied when an emitter returns `Pending` or `Deferred` without a `RetryAfterUtc` value. |

Emitter-supplied `RetryAfterUtc` values continue to take precedence over `DeferredDelay`. Negative delays are rejected during options validation. A zero delay is allowed for hosts that intentionally want retry-ready entries to become eligible on the next drain lookup.

## Duplicate worker guidance

Run one active drain worker per durable outbox partition. Multiple workers pointed at the same durable store may duplicate provider calls unless the store implements leasing, row claiming, partition ownership, or provider-side idempotency.

The current provider-neutral store APIs return candidate entries, not claimed entries. `FindPendingAsync` and `FindRetryReadyAsync` do not prevent another process from reading the same entries before provider emission occurs. EF Core optimistic concurrency can detect conflicting state updates, but it cannot undo a duplicate provider call that already happened.

For a single ASP.NET Core app instance, register the worker once. For scaled-out deployments, prefer one of these patterns:

- designate a single worker instance or separate worker role;
- disable the hosted drain worker in web/API replicas and enable it only in the selected worker process;
- partition outbox entries by tenant, region, workload, or provider path;
- add durable claiming/lease behavior in the storage provider;
- make the downstream provider idempotent by envelope or outbox entry identifier.

See [Outbox Multi-Worker Concurrency](outbox-multi-worker-concurrency.md) for the detailed review of EF Core optimistic concurrency, claim-before-emit patterns, provider-specific SQL options, and safe multi-replica deployment guidance.

## Polling interval guidance

Choose polling intervals based on operational urgency and provider stability:

- development/no-op validation: 5-30 seconds;
- normal production telemetry: 15-60 seconds;
- outage-sensitive providers: use a larger `FailureDelay` and `RetryDelay` to avoid hammering unavailable infrastructure;
- high-throughput environments: prefer larger batches plus durable claiming rather than very aggressive polling.

## Provider outage guidance

Emitter failures should be returned as provider-neutral `GovernanceEmissionResult` values whenever possible. The Core drain then persists deferred, retryable, failed, or dead-letter state transitions through the outbox store.

If the provider throws unexpectedly, the Core drain converts the exception into a retryable provider-neutral outbox failure and schedules the next retry using `AsiBackboneGovernanceOutboxOptions.RetryDelay`. If the worker itself fails before or outside emission, such as a DI or storage exception, the hosted worker waits for `FailureDelay` before the next pass.

## Operational reliability guidance

Durable local persistence keeps governance emission records available for retry and review, but it does not prove that centralized monitoring, compliance ledgers, SIEM tools, or governance catalogs received the event.

Production hosts should monitor queue depth, oldest pending record age, retry and dead-letter counts, drain failure rate, last successful drain timestamp, and worker heartbeat. Sustained backlog or stale worker heartbeat should be treated as an operational incident in review-sensitive deployments.

See [Outbox Drain Reliability and Alerting](outbox-drain-reliability-and-alerting.md) for provider-neutral monitoring, alerting, retry, dead-letter, and recovery guidance.

## Boundary reminder

The hosted drain worker is part of the host/integration layer. It does not make Core responsible for hosting, scheduling, exporter selection, cloud SDKs, database configuration, or production operations.
