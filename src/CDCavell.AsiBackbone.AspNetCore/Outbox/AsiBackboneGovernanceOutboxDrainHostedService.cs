using CDCavell.AsiBackbone.Core.Outbox;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CDCavell.AsiBackbone.AspNetCore.Outbox;

/// <summary>
/// Runs the provider-neutral governance outbox drain from an ASP.NET Core or generic-host background worker.
/// </summary>
/// <remarks>
/// Hosting remains outside Core. The worker resolves the drain through a scoped service provider so durable providers that depend on scoped infrastructure, such as a host-owned EF Core <c>DbContext</c>, remain safe to use.
/// </remarks>
public sealed class AsiBackboneGovernanceOutboxDrainHostedService(
    IServiceScopeFactory scopeFactory,
    IOptionsMonitor<AsiBackboneGovernanceOutboxDrainWorkerOptions> optionsMonitor,
    ILogger<AsiBackboneGovernanceOutboxDrainHostedService> logger) : BackgroundService
{
    private readonly IServiceScopeFactory scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
    private readonly IOptionsMonitor<AsiBackboneGovernanceOutboxDrainWorkerOptions> optionsMonitor = optionsMonitor ?? throw new ArgumentNullException(nameof(optionsMonitor));
    private readonly ILogger<AsiBackboneGovernanceOutboxDrainHostedService> logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc />
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken).ConfigureAwait(false);

        AsiBackboneGovernanceOutboxDrainWorkerOptions options = optionsMonitor.CurrentValue;

        if (options.Enabled && options.DrainOnShutdown && !cancellationToken.IsCancellationRequested)
        {
            using CancellationTokenSource shutdownDrainCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            shutdownDrainCancellation.CancelAfter(options.ShutdownDrainTimeout);

            try
            {
                _ = await DrainOnceAsync(options, shutdownDrainCancellation.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (shutdownDrainCancellation.IsCancellationRequested)
            {
                logger.LogDebug("Governance outbox shutdown drain was canceled.");
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Governance outbox shutdown drain failed.");
            }
        }
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        AsiBackboneGovernanceOutboxDrainWorkerOptions startupOptions = optionsMonitor.CurrentValue;

        if (!startupOptions.Enabled)
        {
            logger.LogDebug("Governance outbox drain worker is disabled.");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            AsiBackboneGovernanceOutboxDrainWorkerOptions options = optionsMonitor.CurrentValue;

            if (!options.Enabled)
            {
                await DelayAsync(options.PollingInterval, stoppingToken).ConfigureAwait(false);
                continue;
            }

            try
            {
                int drainedCount = await DrainOnceAsync(options, stoppingToken).ConfigureAwait(false);
                logger.LogDebug("Governance outbox drain attempted {DrainedCount} entries.", drainedCount);
                await DelayAsync(options.PollingInterval, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Governance outbox drain worker failed before the next polling interval.");
                await DelayAsync(options.FailureDelay, stoppingToken).ConfigureAwait(false);
            }
        }
    }

    private async ValueTask<int> DrainOnceAsync(
        AsiBackboneGovernanceOutboxDrainWorkerOptions options,
        CancellationToken cancellationToken)
    {
        options.Validate();
        cancellationToken.ThrowIfCancellationRequested();

        using IServiceScope scope = scopeFactory.CreateScope();
        AsiBackboneGovernanceOutboxDrain drain = scope.ServiceProvider.GetRequiredService<AsiBackboneGovernanceOutboxDrain>();
        DateTimeOffset retryUtc = options.RetryClock().ToUniversalTime();
        IReadOnlyList<GovernanceOutboxEntry> drainedEntries = await drain.DrainAsync(
            retryUtc,
            options.BatchSize,
            cancellationToken)
            .ConfigureAwait(false);

        return drainedEntries.Count;
    }

    private static async ValueTask DelayAsync(TimeSpan delay, CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Host shutdown cancels the delay. The outer loop handles termination.
        }
    }
}