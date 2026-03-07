using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ServicePulseMonitor.Client.Configuration;
using ServicePulseMonitor.Client.Models;
using System.Net.Http.Json;

namespace ServicePulseMonitor.Client.Services;

/// <summary>
/// Typed <see cref="HttpClient"/> implementation of <see cref="IHealthMonitorClient"/> that communicates
/// with the ServicePulseMonitor REST API with configurable retry logic.
/// </summary>
public class HealthMonitorClient(
    HttpClient httpClient,
    IOptions<HealthMonitoringOptions> options,
    ILogger<HealthMonitorClient> logger) : IHealthMonitorClient
{
    private readonly HealthMonitoringOptions _options = options.Value;

    /// <inheritdoc/>
    public async Task<bool> RegisterServiceAsync(ServiceRegistration registration, CancellationToken cancellationToken = default)
        => await SendWithRetryAsync("api/services/register", registration, cancellationToken);

    /// <inheritdoc/>
    public async Task<bool> ReportHealthAsync(HealthReport report, CancellationToken cancellationToken = default)
        => await SendWithRetryAsync("api/health/report", report, cancellationToken);

    /// <inheritdoc/>
    public async Task<bool> ReportDependencyAsync(DependencyInfo dependency, CancellationToken cancellationToken = default)
        => await SendWithRetryAsync("api/dependencies/report", dependency, cancellationToken);

    /// <inheritdoc/>
    public async Task<bool> DeregisterServiceAsync(string serviceName, CancellationToken cancellationToken = default)
    {
        for (var attempt = 0; attempt <= _options.MaxRetries; attempt++)
        {
            try
            {
                var response = await httpClient.DeleteAsync($"api/services/{Uri.EscapeDataString(serviceName)}", cancellationToken);
                if (response.IsSuccessStatusCode)
                    return true;

                logger.LogWarning("Deregister returned {StatusCode} for service {ServiceName} (attempt {Attempt})",
                    (int)response.StatusCode, serviceName, attempt + 1);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error deregistering service {ServiceName} (attempt {Attempt})", serviceName, attempt + 1);
            }

            if (attempt < _options.MaxRetries)
                await Task.Delay(_options.RetryDelay, cancellationToken);
        }

        return false;
    }

    private async Task<bool> SendWithRetryAsync<T>(string relativeUri, T payload, CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt <= _options.MaxRetries; attempt++)
        {
            try
            {
                var response = await httpClient.PostAsJsonAsync(relativeUri, payload, cancellationToken);
                if (response.IsSuccessStatusCode)
                    return true;

                logger.LogWarning("POST {Uri} returned {StatusCode} (attempt {Attempt})",
                    relativeUri, (int)response.StatusCode, attempt + 1);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error sending POST {Uri} (attempt {Attempt})", relativeUri, attempt + 1);
            }

            if (attempt < _options.MaxRetries)
                await Task.Delay(_options.RetryDelay, cancellationToken);
        }

        return false;
    }
}
