using Microsoft.Extensions.Options;
using ServicePulseMonitor.Client.Configuration;
using ServicePulseMonitor.Client.Models;
using System.Collections.Concurrent;

namespace ServicePulseMonitor.Client.Services;

/// <summary>
/// A <see cref="DelegatingHandler"/> that intercepts outgoing HTTP requests and reports newly
/// discovered base URLs as dependencies to the ServicePulseMonitor API.
/// Each unique base URL (scheme + authority) is reported only once per handler instance.
/// </summary>
/// <remarks>
/// Register this handler on your own <see cref="System.Net.Http.HttpClient"/> instances via
/// <c>.AddHttpMessageHandler&lt;DependencyDetectionHandler&gt;()</c>. Never register it on the
/// internal monitor client to avoid circular dependency reporting.
/// </remarks>
public class DependencyDetectionHandler(
    IHealthMonitorClient monitorClient,
    IOptions<HealthMonitoringOptions> options) : DelegatingHandler
{
    private readonly HealthMonitoringOptions _options = options.Value;
    private readonly ConcurrentDictionary<string, bool> _reportedUrls = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Sends the request via the inner handler and, if dependency detection is enabled and the
    /// base URL has not been seen before, fires a background task to report the dependency.
    /// </summary>
    /// <param name="request">The outgoing HTTP request.</param>
    /// <param name="cancellationToken">A token to cancel the send operation.</param>
    /// <returns>The <see cref="HttpResponseMessage"/> from the inner handler.</returns>
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = await base.SendAsync(request, cancellationToken);

        if (_options.EnableDependencyDetection && request.RequestUri is not null)
        {
            var baseUrl = request.RequestUri.GetLeftPart(UriPartial.Authority);

            if (_reportedUrls.TryAdd(baseUrl, true))
            {
                var dependency = new DependencyInfo
                {
                    ServiceName = _options.ServiceName,
                    DependsOnServiceName = request.RequestUri.Host,
                    DependsOnUrl = baseUrl,
                    DiscoveredAt = DateTime.UtcNow
                };

                _ = Task.Run(() => monitorClient.ReportDependencyAsync(dependency), CancellationToken.None);
            }
        }

        return response;
    }
}
