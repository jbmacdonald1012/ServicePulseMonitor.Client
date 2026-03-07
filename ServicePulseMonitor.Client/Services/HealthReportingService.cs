using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ServicePulseMonitor.Client.Configuration;
using ServicePulseMonitor.Client.Models;

namespace ServicePulseMonitor.Client.Services;

/// <summary>
/// A hosted background service that registers the service on startup, periodically reports its
/// health to the ServicePulseMonitor API, and deregisters on graceful shutdown.
/// </summary>
public class HealthReportingService(
    IHealthMonitorClient monitorClient,
    HealthCheckService healthCheckService,
    IOptions<HealthMonitoringOptions> options,
    IHostApplicationLifetime lifetime,
    ILogger<HealthReportingService> logger) : BackgroundService
{
    private readonly HealthMonitoringOptions _options = options.Value;

    /// <summary>
    /// Waits for an initial startup delay, registers the service, then enters a
    /// <see cref="PeriodicTimer"/> loop that reports health on each tick.
    /// If registration fails and <see cref="HealthMonitoringOptions.FailOnMonitorUnavailable"/>
    /// is <see langword="true"/>, the application is stopped immediately.
    /// </summary>
    /// <param name="stoppingToken">Triggered when the host is performing a graceful shutdown.</param>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);

        var registration = new ServiceRegistration
        {
            ServiceName = _options.ServiceName,
            ServiceDescription = _options.ServiceDescription,
            ServiceUrl = _options.ServiceUrl,
            RegisteredAt = DateTime.UtcNow
        };

        var registered = await monitorClient.RegisterServiceAsync(registration, stoppingToken);

        if (!registered)
        {
            logger.LogWarning("Failed to register service {ServiceName} with health monitor.", _options.ServiceName);

            if (_options.FailOnMonitorUnavailable)
            {
                logger.LogCritical("FailOnMonitorUnavailable is set. Stopping application.");
                lifetime.StopApplication();
                return;
            }
        }
        else
        {
            logger.LogInformation("Service {ServiceName} registered with health monitor.", _options.ServiceName);
        }

        using var timer = new PeriodicTimer(_options.ReportInterval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await ReportHealthAsync(stoppingToken);
        }
    }

    /// <summary>
    /// Deregisters the service from the ServicePulseMonitor API before stopping the background task.
    /// </summary>
    /// <param name="cancellationToken">Triggered when the host shutdown timeout elapses.</param>
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Deregistering service {ServiceName} from health monitor.", _options.ServiceName);
        await monitorClient.DeregisterServiceAsync(_options.ServiceName, CancellationToken.None);
        await base.StopAsync(cancellationToken);
    }

    private async Task ReportHealthAsync(CancellationToken cancellationToken)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();

        Microsoft.Extensions.Diagnostics.HealthChecks.HealthReport? healthReport = null;

        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(_options.HealthCheckTimeout);

            healthReport = await healthCheckService.CheckHealthAsync(null, cts.Token);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error running health checks for service {ServiceName}.", _options.ServiceName);
        }

        sw.Stop();

        var report = new Models.HealthReport
        {
            ServiceName = _options.ServiceName,
            Status = MapStatus(healthReport?.Status ?? Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy),
            ResponseTimeMs = sw.ElapsedMilliseconds,
            CheckedAt = DateTime.UtcNow,
            Checks = healthReport?.Entries
                .Select(e => new ServiceHealthCheckResult
                {
                    Name = e.Key,
                    Status = MapStatus(e.Value.Status),
                    Description = e.Value.Description
                })
                .ToList() ?? new List<ServiceHealthCheckResult>()
        };

        await monitorClient.ReportHealthAsync(report, cancellationToken);
    }

    private static ServiceHealthStatus MapStatus(Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus status) =>
        status switch
        {
            Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Healthy => Models.ServiceHealthStatus.Healthy,
            Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Degraded => Models.ServiceHealthStatus.Degraded,
            _ => Models.ServiceHealthStatus.Unhealthy
        };
}
