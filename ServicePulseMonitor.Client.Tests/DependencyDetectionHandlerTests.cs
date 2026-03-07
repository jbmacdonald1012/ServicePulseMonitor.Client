using Microsoft.Extensions.Options;
using Moq;
using ServicePulseMonitor.Client.Configuration;
using ServicePulseMonitor.Client.Models;
using ServicePulseMonitor.Client.Services;
using System.Net;

namespace ServicePulseMonitor.Client.Tests;

[TestFixture]
public class DependencyDetectionHandlerTests
{
    private sealed class TestHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
    }

    private static (DependencyDetectionHandler handler, Mock<IHealthMonitorClient> monitorMock) CreateHandler(
        bool enableDetection = true)
    {
        var monitorMock = new Mock<IHealthMonitorClient>();
        monitorMock.Setup(m => m.ReportDependencyAsync(It.IsAny<DependencyInfo>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var opts = Options.Create(new HealthMonitoringOptions
        {
            MonitorApiUrl = "http://monitor.local/",
            ServiceName = "test-service",
            EnableDependencyDetection = enableDetection
        });

        var handler = new DependencyDetectionHandler(monitorMock.Object, opts)
        {
            InnerHandler = new TestHandler()
        };

        return (handler, monitorMock);
    }

    [Test]
    public async Task Reports_Dependency_On_First_Call()
    {
        var (handler, monitorMock) = CreateHandler();
        var invoker = new HttpMessageInvoker(handler);

        var request = new HttpRequestMessage(HttpMethod.Get, "http://api.example.com/data");
        await invoker.SendAsync(request, CancellationToken.None);

        // Allow fire-and-forget to complete
        await Task.Delay(50);

        monitorMock.Verify(m => m.ReportDependencyAsync(
            It.Is<DependencyInfo>(d => d.DependsOnUrl == "http://api.example.com"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Skips_Duplicate_Base_Url()
    {
        var (handler, monitorMock) = CreateHandler();
        var invoker = new HttpMessageInvoker(handler);

        await invoker.SendAsync(new HttpRequestMessage(HttpMethod.Get, "http://api.example.com/foo"), CancellationToken.None);
        await invoker.SendAsync(new HttpRequestMessage(HttpMethod.Get, "http://api.example.com/bar"), CancellationToken.None);

        await Task.Delay(50);

        monitorMock.Verify(m => m.ReportDependencyAsync(
            It.IsAny<DependencyInfo>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Skips_All_When_EnableDependencyDetection_False()
    {
        var (handler, monitorMock) = CreateHandler(enableDetection: false);
        var invoker = new HttpMessageInvoker(handler);

        await invoker.SendAsync(new HttpRequestMessage(HttpMethod.Get, "http://api.example.com/data"), CancellationToken.None);

        await Task.Delay(50);

        monitorMock.Verify(m => m.ReportDependencyAsync(
            It.IsAny<DependencyInfo>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task Reports_Different_Hosts_Separately()
    {
        var (handler, monitorMock) = CreateHandler();
        var invoker = new HttpMessageInvoker(handler);

        await invoker.SendAsync(new HttpRequestMessage(HttpMethod.Get, "http://api1.example.com/data"), CancellationToken.None);
        await invoker.SendAsync(new HttpRequestMessage(HttpMethod.Get, "http://api2.example.com/data"), CancellationToken.None);

        await Task.Delay(50);

        monitorMock.Verify(m => m.ReportDependencyAsync(
            It.IsAny<DependencyInfo>(),
            It.IsAny<CancellationToken>()), Times.Exactly(2));
    }
}
