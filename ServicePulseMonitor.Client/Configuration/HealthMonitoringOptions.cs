using System.ComponentModel.DataAnnotations;

namespace ServicePulseMonitor.Client.Configuration;

/// <summary>
/// Configuration options for the ServicePulseMonitor client.
/// </summary>
public class HealthMonitoringOptions
{
    /// <summary>
    /// Gets or sets the base URL of the ServicePulseMonitor API.
    /// Required. Example: <c>https://monitor.example.com</c>.
    /// </summary>
    [Required]
    public string MonitorApiUrl { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the unique name used to identify this service in the monitor.
    /// Required.
    /// </summary>
    [Required]
    public string ServiceName { get; set; } = string.Empty;

    /// <summary>Gets or sets an optional human-readable description of this service.</summary>
    public string? ServiceDescription { get; set; }

    /// <summary>Gets or sets the optional public URL at which this service can be reached.</summary>
    public string? ServiceUrl { get; set; }

    /// <summary>
    /// Gets or sets how often the service reports its health status to the monitor.
    /// Defaults to 30 seconds.
    /// </summary>
    public TimeSpan ReportInterval { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets or sets the maximum time allowed for a single health check evaluation.
    /// Defaults to 5 seconds.
    /// </summary>
    public TimeSpan HealthCheckTimeout { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Gets or sets whether outgoing HTTP dependencies are automatically detected and reported.
    /// Defaults to <see langword="true"/>.
    /// </summary>
    public bool EnableDependencyDetection { get; set; } = true;

    /// <summary>
    /// Gets or sets whether the application should stop if the initial service registration fails.
    /// Defaults to <see langword="false"/>.
    /// </summary>
    public bool FailOnMonitorUnavailable { get; set; } = false;

    /// <summary>
    /// Gets or sets the maximum number of retry attempts for each API call.
    /// Defaults to 3.
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Gets or sets the delay between retry attempts.
    /// Defaults to 2 seconds.
    /// </summary>
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(2);
}
