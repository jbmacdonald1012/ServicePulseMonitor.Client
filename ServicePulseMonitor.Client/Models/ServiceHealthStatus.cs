namespace ServicePulseMonitor.Client.Models;

/// <summary>
/// Represents the aggregated health status of a service.
/// </summary>
public enum ServiceHealthStatus
{
    /// <summary>The service is operating normally.</summary>
    Healthy,

    /// <summary>The service is operational but experiencing degraded performance or partial failures.</summary>
    Degraded,

    /// <summary>The service is not operational or is failing health checks.</summary>
    Unhealthy
}
