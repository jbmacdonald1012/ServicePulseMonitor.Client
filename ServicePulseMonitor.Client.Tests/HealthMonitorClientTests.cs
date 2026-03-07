using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using ServicePulseMonitor.Client.Configuration;
using ServicePulseMonitor.Client.Models;
using ServicePulseMonitor.Client.Services;
using System.Net;

namespace ServicePulseMonitor.Client.Tests;

[TestFixture]
public class HealthMonitorClientTests
{
    private static HealthMonitoringOptions CreateOptions(int maxRetries = 0) => new()
    {
        MonitorApiUrl = "http://monitor.local/",
        ServiceName = "test-service",
        MaxRetries = maxRetries,
        RetryDelay = TimeSpan.Zero
    };

    private static (HealthMonitorClient client, Mock<HttpMessageHandler> handlerMock) CreateClient(
        HttpResponseMessage response, HealthMonitoringOptions? options = null)
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        var httpClient = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("http://monitor.local/")
        };

        var opts = Options.Create(options ?? CreateOptions());
        var client = new HealthMonitorClient(httpClient, opts, NullLogger<HealthMonitorClient>.Instance);
        return (client, handlerMock);
    }

    private static (HealthMonitorClient client, Mock<HttpMessageHandler> handlerMock) CreateClientThrowing(
        Exception exception, HealthMonitoringOptions? options = null)
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(exception);

        var httpClient = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("http://monitor.local/")
        };

        var opts = Options.Create(options ?? CreateOptions());
        var client = new HealthMonitorClient(httpClient, opts, NullLogger<HealthMonitorClient>.Instance);
        return (client, handlerMock);
    }

    [Test]
    public async Task RegisterServiceAsync_Returns_True_On_200()
    {
        var (client, _) = CreateClient(new HttpResponseMessage(HttpStatusCode.OK));
        var result = await client.RegisterServiceAsync(new ServiceRegistration { ServiceName = "test" });
        Assert.That(result, Is.True);
    }

    [Test]
    public async Task RegisterServiceAsync_Returns_False_On_500()
    {
        var (client, _) = CreateClient(new HttpResponseMessage(HttpStatusCode.InternalServerError));
        var result = await client.RegisterServiceAsync(new ServiceRegistration { ServiceName = "test" });
        Assert.That(result, Is.False);
    }

    [Test]
    public async Task RegisterServiceAsync_Returns_False_On_Exception()
    {
        var (client, _) = CreateClientThrowing(new HttpRequestException("connection refused"));
        var result = await client.RegisterServiceAsync(new ServiceRegistration { ServiceName = "test" });
        Assert.That(result, Is.False);
    }

    [Test]
    public async Task RegisterServiceAsync_Retries_Correct_Number_Of_Times()
    {
        var maxRetries = 2;
        var opts = CreateOptions(maxRetries);
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));

        var httpClient = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("http://monitor.local/")
        };
        var client = new HealthMonitorClient(httpClient, Options.Create(opts), NullLogger<HealthMonitorClient>.Instance);

        var result = await client.RegisterServiceAsync(new ServiceRegistration { ServiceName = "test" });

        Assert.That(result, Is.False);
        // initial attempt + maxRetries
        handlerMock.Protected().Verify(
            "SendAsync",
            Times.Exactly(maxRetries + 1),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>());
    }

    [Test]
    public async Task ReportHealthAsync_Returns_True_On_200()
    {
        var (client, _) = CreateClient(new HttpResponseMessage(HttpStatusCode.OK));
        var result = await client.ReportHealthAsync(new HealthReport { ServiceName = "test" });
        Assert.That(result, Is.True);
    }

    [Test]
    public async Task ReportDependencyAsync_Returns_True_On_200()
    {
        var (client, _) = CreateClient(new HttpResponseMessage(HttpStatusCode.OK));
        var result = await client.ReportDependencyAsync(new DependencyInfo { ServiceName = "test" });
        Assert.That(result, Is.True);
    }

    [Test]
    public async Task DeregisterServiceAsync_Returns_True_On_200()
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

        var httpClient = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("http://monitor.local/")
        };
        var client = new HealthMonitorClient(httpClient, Options.Create(CreateOptions()), NullLogger<HealthMonitorClient>.Instance);

        var result = await client.DeregisterServiceAsync("test-service");
        Assert.That(result, Is.True);
    }

    [Test]
    public async Task DeregisterServiceAsync_Returns_False_On_500()
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.InternalServerError));

        var httpClient = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("http://monitor.local/")
        };
        var client = new HealthMonitorClient(httpClient, Options.Create(CreateOptions()), NullLogger<HealthMonitorClient>.Instance);

        var result = await client.DeregisterServiceAsync("test-service");
        Assert.That(result, Is.False);
    }
}
