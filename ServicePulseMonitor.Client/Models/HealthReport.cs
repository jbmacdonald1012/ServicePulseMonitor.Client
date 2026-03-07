namespace ServicePulseMonitor.Client.Models;

/// <summary>
/// A health report payload sent to the ServicePulseMonitor API on each reporting interval.
/// </summary>
public class HealthReport
{
    /// <summary>Gets or sets the name of the service that produced this report.</summary>
    public string ServiceName { get; set; } = string.Empty;

    /// <summary>Gets or sets the aggregated health status of the service.</summary>
    public ServiceHealthStatus Status { get; set; }

    /// <summary>Gets or sets the total time in milliseconds taken to run all health checks.</summary>
    public long ResponseTimeMs { get; set; }

    /// <summary>Gets or sets the UTC timestamp at which the health checks were evaluated.</summary>
    public DateTime CheckedAt { get; set; }

    /// <summary>Gets or sets the list of individual health check results that make up this report.</summary>
    public List<ServiceHealthCheckResult> Checks { get; set; } = new();
}
