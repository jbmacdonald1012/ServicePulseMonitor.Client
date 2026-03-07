using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ServicePulseMonitor.Client.HealthChecks;
using ServicePulseMonitor.Client.Services;

namespace ServicePulseMonitor.Client.Configuration;

/// <summary>
/// Extension methods for registering ServicePulseMonitor client services with the dependency injection container.
/// </summary>
public static class ServicePulseMonitorExtensions
{
    /// <summary>
    /// Registers all ServicePulseMonitor client services, including health reporting, dependency detection,
    /// and the self health check.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <param name="configureOptions">A delegate to configure <see cref="HealthMonitoringOptions"/>.</param>
    /// <returns>The same <see cref="IServiceCollection"/> so calls can be chained.</returns>
    /// <exception cref="Microsoft.Extensions.Options.OptionsValidationException">
    /// Thrown at host startup if required options (<see cref="HealthMonitoringOptions.MonitorApiUrl"/> or
    /// <see cref="HealthMonitoringOptions.ServiceName"/>) are not set.
    /// </exception>
    public static IServiceCollection AddHealthMonitoring(
        this IServiceCollection services,
        Action<HealthMonitoringOptions> configureOptions)
    {
        services.AddOptions<HealthMonitoringOptions>()
            .Configure(configureOptions)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddHealthChecks()
            .AddCheck<SelfHealthCheck>("self");

        services.AddHttpClient<IHealthMonitorClient, HealthMonitorClient>((serviceProvider, client) =>
        {
            var opts = serviceProvider.GetRequiredService<IOptions<HealthMonitoringOptions>>().Value;
            var baseUrl = opts.MonitorApiUrl.TrimEnd('/') + '/';
            client.BaseAddress = new Uri(baseUrl);
        });

        services.AddHostedService<HealthReportingService>();

        services.AddTransient<DependencyDetectionHandler>();

        return services;
    }
}
