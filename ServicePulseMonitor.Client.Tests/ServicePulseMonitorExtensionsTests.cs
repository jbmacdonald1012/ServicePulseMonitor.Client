using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Moq;
using ServicePulseMonitor.Client.Configuration;
using ServicePulseMonitor.Client.Services;

namespace ServicePulseMonitor.Client.Tests;

[TestFixture]
public class ServicePulseMonitorExtensionsTests
{
    private static IServiceCollection CreateServices(Action<HealthMonitoringOptions> configure)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(Mock.Of<IHostApplicationLifetime>());
        services.AddHealthMonitoring(configure);
        return services;
    }

    [Test]
    public void Throws_OptionsValidationException_When_MonitorApiUrl_Missing()
    {
        var services = CreateServices(o =>
        {
            o.ServiceName = "my-service";
            // MonitorApiUrl not set
        });

        var provider = services.BuildServiceProvider();

        Assert.Throws<OptionsValidationException>(() =>
        {
            var _ = provider.GetRequiredService<IOptions<HealthMonitoringOptions>>().Value;
        });
    }

    [Test]
    public void Throws_OptionsValidationException_When_ServiceName_Missing()
    {
        var services = CreateServices(o =>
        {
            o.MonitorApiUrl = "http://monitor.local/";
            // ServiceName not set
        });

        var provider = services.BuildServiceProvider();

        Assert.Throws<OptionsValidationException>(() =>
        {
            var _ = provider.GetRequiredService<IOptions<HealthMonitoringOptions>>().Value;
        });
    }

    [Test]
    public void Resolves_IHealthMonitorClient()
    {
        var services = CreateServices(o =>
        {
            o.MonitorApiUrl = "http://monitor.local/";
            o.ServiceName = "my-service";
        });

        var provider = services.BuildServiceProvider();
        var client = provider.GetService<IHealthMonitorClient>();

        Assert.That(client, Is.Not.Null);
        Assert.That(client, Is.InstanceOf<HealthMonitorClient>());
    }

    [Test]
    public void Resolves_DependencyDetectionHandler()
    {
        var services = CreateServices(o =>
        {
            o.MonitorApiUrl = "http://monitor.local/";
            o.ServiceName = "my-service";
        });

        var provider = services.BuildServiceProvider();
        var handler = provider.GetService<DependencyDetectionHandler>();

        Assert.That(handler, Is.Not.Null);
    }

    [Test]
    public void IHostedService_List_Contains_HealthReportingService()
    {
        var services = CreateServices(o =>
        {
            o.MonitorApiUrl = "http://monitor.local/";
            o.ServiceName = "my-service";
        });

        var provider = services.BuildServiceProvider();
        var hostedServices = provider.GetServices<IHostedService>().ToList();

        Assert.That(hostedServices, Has.Some.InstanceOf<HealthReportingService>());
    }
}
