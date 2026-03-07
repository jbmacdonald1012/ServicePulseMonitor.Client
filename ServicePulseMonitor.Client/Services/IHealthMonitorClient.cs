using ServicePulseMonitor.Client.Models;

namespace ServicePulseMonitor.Client.Services;

/// <summary>
/// Defines the contract for communicating with the ServicePulseMonitor API.
/// </summary>
public interface IHealthMonitorClient
{
    /// <summary>
    /// Registers the service with the ServicePulseMonitor API.
    /// </summary>
    /// <param name="registration">The registration details to send.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns><see langword="true"/> if registration succeeded; otherwise <see langword="false"/>.</returns>
    Task<bool> RegisterServiceAsync(ServiceRegistration registration, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reports the current health status of the service to the ServicePulseMonitor API.
    /// </summary>
    /// <param name="report">The health report to send.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns><see langword="true"/> if the report was accepted; otherwise <see langword="false"/>.</returns>
    Task<bool> ReportHealthAsync(HealthReport report, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reports a discovered outgoing HTTP dependency to the ServicePulseMonitor API.
    /// </summary>
    /// <param name="dependency">The dependency information to send.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns><see langword="true"/> if the dependency was accepted; otherwise <see langword="false"/>.</returns>
    Task<bool> ReportDependencyAsync(DependencyInfo dependency, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deregisters the service from the ServicePulseMonitor API.
    /// </summary>
    /// <param name="serviceName">The name of the service to deregister.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns><see langword="true"/> if deregistration succeeded; otherwise <see langword="false"/>.</returns>
    Task<bool> DeregisterServiceAsync(string serviceName, CancellationToken cancellationToken = default);
}
