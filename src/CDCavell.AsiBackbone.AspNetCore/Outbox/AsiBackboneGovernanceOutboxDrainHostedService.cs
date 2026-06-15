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
    private static readonly Action<ILogger, Exception?> LogShutdownDrainCanceled = LoggerMessage.Define(
        LogLevel.Debug,
        new EventId(19801, nameof(LogShutdownDrainCanceled)),
        "Governance outbox shutdown drain was canceled.");

    private static readonly Action<ILogger, Exception?> LogShutdownDrainFailed = LoggerMessage.Define(
        LogLevel.Warning,
        new EventId(19802, nameof(LogShutdownDrainFailed)),
        "Governance outbox shutdown drain failed.");

    private static readonly Action<ILogger, Exception?> LogWorkerDisabled = LoggerMessage.Define(
        LogLevel.Debug,
        new EventId(19803, nameof(LogWorkerDisabled)),
        "Governance outbox drain worker is disabled.");

    private static readonly Action<ILogger, int, Exception?> LogDrainAttempted = LoggerMessage.Define<int>(
        LogLevel.Debug,
        new EventId(19804, nameof(LogDrainAttempted)),
        "Governance outbox drain attempted {DrainedCount} entries.");

    private static readonly Action<ILogger, Exception?> LogWorkerFailed = LoggerMessage.Define(
        LogLevel.Warning,
        new EventId(19805, nameof(LogWorkerFailed)),
        "Governance outbox drain worker failed before the next polling interval.");

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
            using var shutdownDrainCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            shutdownDrainCancellation.CancelAfter(options.ShutdownDrainTimeout);

            try
            {
                _ = await DrainOnceAsync(options, shutdownDrainCancellation.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (shutdownDrainCancellation.IsCancellationRequested)
            {
                LogShutdownDrainCanceled(logger, null);
            }
            catch (Exception ex)
            {
                LogShutdownDrainFailed(logger, ex);
            }
        }
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        AsiBackboneGovernanceOutboxDrainWorkerOptions startupOptions = optionsMonitor.CurrentValue;

        if (!startupOptions.Enabled)
        {
            LogWorkerDisabled(logger, null);
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
                LogDrainAttempted(logger, drainedCount, null);
                await DelayAsync(options.PollingInterval, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                LogWorkerFailed(logger, ex);
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
