namespace ServicePulseMonitor.Client.Models;

/// <summary>
/// The result of an individual named health check within a service.
/// </summary>
public class ServiceHealthCheckResult
{
    /// <summary>Gets or sets the name of the health check.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the health status reported by this check.</summary>
    public ServiceHealthStatus Status { get; set; }

    /// <summary>Gets or sets an optional human-readable description of the check result.</summary>
    public string? Description { get; set; }
}
