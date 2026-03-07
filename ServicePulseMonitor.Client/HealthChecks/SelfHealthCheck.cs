using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ServicePulseMonitor.Client.HealthChecks;

/// <summary>
/// A built-in health check that always reports the service as healthy, confirming the process
/// is running and the health check pipeline is reachable.
/// </summary>
public class SelfHealthCheck : IHealthCheck
{
    /// <summary>
    /// Returns a healthy result unconditionally.
    /// </summary>
    /// <param name="context">Provides access to the health check registration.</param>
    /// <param name="cancellationToken">A token to cancel the check.</param>
    /// <returns>A completed task containing a healthy <see cref="HealthCheckResult"/>.</returns>
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        => Task.FromResult(HealthCheckResult.Healthy("Service is running."));
}
