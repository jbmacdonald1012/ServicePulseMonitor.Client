namespace ServicePulseMonitor.Client.Models;

/// <summary>
/// Describes an outgoing HTTP dependency discovered by <see cref="ServicePulseMonitor.Client.Services.DependencyDetectionHandler"/>.
/// </summary>
public class DependencyInfo
{
    /// <summary>Gets or sets the name of the service that has this dependency.</summary>
    public string ServiceName { get; set; } = string.Empty;

    /// <summary>Gets or sets the hostname of the service being depended upon.</summary>
    public string DependsOnServiceName { get; set; } = string.Empty;

    /// <summary>Gets or sets the base URL (scheme + authority) of the dependency.</summary>
    public string DependsOnUrl { get; set; } = string.Empty;

    /// <summary>Gets or sets the UTC timestamp at which the dependency was first discovered.</summary>
    public DateTime DiscoveredAt { get; set; }
}
