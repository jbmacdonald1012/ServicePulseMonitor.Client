using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using ServicePulseMonitor.Client.Configuration;
using ServicePulseMonitor.Client.Services;
using ClientHealthReport = ServicePulseMonitor.Client.Models.HealthReport;
using ServiceRegistration = ServicePulseMonitor.Client.Models.ServiceRegistration;

namespace ServicePulseMonitor.Client.Tests;

[TestFixture]
public class HealthReportingServiceTests
{
    private static HealthMonitoringOptions CreateOptions(bool failOnUnavailable = false) => new()
    {
        MonitorApiUrl = "http://monitor.local/",
        ServiceName = "test-service",
        ReportInterval = TimeSpan.FromMilliseconds(100),
        HealthCheckTimeout = TimeSpan.FromSeconds(5),
        FailOnMonitorUnavailable = failOnUnavailable
    };

    private static Mock<HealthCheckService> CreateHealthCheckServiceMock()
    {
        var mock = new Mock<HealthCheckService>();
        var emptyReport = new Microsoft.Extensions.Diagnostics.HealthChecks.HealthReport(
            new Dictionary<string, HealthReportEntry>(),
            Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Healthy,
            TimeSpan.FromMilliseconds(1));

        mock.Setup(h => h.CheckHealthAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(emptyReport);

        return mock;
    }

    [Test]
    public async Task ExecuteAsync_Registers_Service_On_Start()
    {
        var monitorMock = new Mock<IHealthMonitorClient>();
        monitorMock.Setup(m => m.RegisterServiceAsync(It.IsAny<ServiceRegistration>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        monitorMock.Setup(m => m.ReportHealthAsync(It.IsAny<ClientHealthReport>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var lifetimeMock = new Mock<IHostApplicationLifetime>();
        var healthCheckMock = CreateHealthCheckServiceMock();
        var opts = Options.Create(CreateOptions());

        var service = new HealthReportingService(
            monitorMock.Object,
            healthCheckMock.Object,
            opts,
            lifetimeMock.Object,
            NullLogger<HealthReportingService>.Instance);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        _ = service.StartAsync(cts.Token);

        // Wait for startup delay + registration
        await Task.Delay(TimeSpan.FromSeconds(2.5), CancellationToken.None);
        await service.StopAsync(CancellationToken.None);

        monitorMock.Verify(m => m.RegisterServiceAsync(
            It.Is<ServiceRegistration>(r => r.ServiceName == "test-service"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task StopAsync_Deregisters_Service()
    {
        var monitorMock = new Mock<IHealthMonitorClient>();
        monitorMock.Setup(m => m.RegisterServiceAsync(It.IsAny<ServiceRegistration>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        monitorMock.Setup(m => m.ReportHealthAsync(It.IsAny<ClientHealthReport>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        monitorMock.Setup(m => m.DeregisterServiceAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var lifetimeMock = new Mock<IHostApplicationLifetime>();
        var healthCheckMock = CreateHealthCheckServiceMock();
        var opts = Options.Create(CreateOptions());

        var service = new HealthReportingService(
            monitorMock.Object,
            healthCheckMock.Object,
            opts,
            lifetimeMock.Object,
            NullLogger<HealthReportingService>.Instance);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        _ = service.StartAsync(cts.Token);
        await Task.Delay(TimeSpan.FromSeconds(2.5), CancellationToken.None);
        await service.StopAsync(CancellationToken.None);

        monitorMock.Verify(m => m.DeregisterServiceAsync("test-service", CancellationToken.None), Times.Once);
    }

    [Test]
    public async Task ExecuteAsync_Stops_Application_When_FailOnMonitorUnavailable_And_Registration_Fails()
    {
        var monitorMock = new Mock<IHealthMonitorClient>();
        monitorMock.Setup(m => m.RegisterServiceAsync(It.IsAny<ServiceRegistration>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var lifetimeMock = new Mock<IHostApplicationLifetime>();
        var healthCheckMock = CreateHealthCheckServiceMock();
        var opts = Options.Create(CreateOptions(failOnUnavailable: true));

        var service = new HealthReportingService(
            monitorMock.Object,
            healthCheckMock.Object,
            opts,
            lifetimeMock.Object,
            NullLogger<HealthReportingService>.Instance);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(4));
        await service.StartAsync(cts.Token);

        await Task.Delay(TimeSpan.FromSeconds(3), CancellationToken.None);
        await service.StopAsync(CancellationToken.None);

        lifetimeMock.Verify(l => l.StopApplication(), Times.Once);
    }

    [Test]
    public async Task ExecuteAsync_Does_Not_Stop_Application_When_FailOnMonitorUnavailable_False_And_Registration_Fails()
    {
        var monitorMock = new Mock<IHealthMonitorClient>();
        monitorMock.Setup(m => m.RegisterServiceAsync(It.IsAny<ServiceRegistration>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        monitorMock.Setup(m => m.ReportHealthAsync(It.IsAny<ClientHealthReport>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var lifetimeMock = new Mock<IHostApplicationLifetime>();
        var healthCheckMock = CreateHealthCheckServiceMock();
        var opts = Options.Create(CreateOptions(failOnUnavailable: false));

        var service = new HealthReportingService(
            monitorMock.Object,
            healthCheckMock.Object,
            opts,
            lifetimeMock.Object,
            NullLogger<HealthReportingService>.Instance);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(4));
        _ = service.StartAsync(cts.Token);

        await Task.Delay(TimeSpan.FromSeconds(3), CancellationToken.None);
        await service.StopAsync(CancellationToken.None);

        lifetimeMock.Verify(l => l.StopApplication(), Times.Never);
    }
}
