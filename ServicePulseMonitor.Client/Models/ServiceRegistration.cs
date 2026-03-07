namespace ServicePulseMonitor.Client.Models;

/// <summary>
/// Represents a service registration request sent to the ServicePulseMonitor API.
/// </summary>
public class ServiceRegistration
{
    /// <summary>Gets or sets the unique name of the service being registered.</summary>
    public string ServiceName { get; set; } = string.Empty;

    /// <summary>Gets or sets an optional human-readable description of the service.</summary>
    public string? ServiceDescription { get; set; }

    /// <summary>Gets or sets the optional public URL at which the service can be reached.</summary>
    public string? ServiceUrl { get; set; }

    /// <summary>Gets or sets the UTC timestamp at which the service registered.</summary>
    public DateTime RegisteredAt { get; set; }
}
