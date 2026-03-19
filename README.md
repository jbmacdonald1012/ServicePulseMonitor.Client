# ServicePulseMonitor.Client

A .NET 8 client library for integrating microservices with the ServicePulseMonitor health monitoring API. The library handles service registration, periodic health reporting, automatic HTTP dependency detection, and graceful deregistration on shutdown — all with a single call to `AddHealthMonitoring()`.

## Features

- **Automatic registration & deregistration** — registers on startup, deregisters on graceful shutdown
- **Periodic health reporting** — evaluates all registered `IHealthCheck` implementations and posts results at a configurable interval
- **HTTP dependency detection** — intercepts outgoing HTTP requests to discover and report service dependencies automatically
- **Retry logic** — configurable retry loop for transient API failures (no Polly required)
- **Built-in self health check** — baseline check that always reports the service as reachable
- **Fail-fast mode** — optionally stops the application if initial registration fails

## Installation

```bash
dotnet add package ServicePulseMonitor.Client
```

**Target framework:** .NET 8.0

## Quick Start

```csharp
var builder = WebApplication.CreateBuilder(args);

// Add your application's health checks
builder.Services.AddHealthChecks()
    .AddUrlGroup(new Uri("https://db.internal/health"), name: "database");

// Register the ServicePulseMonitor client
builder.Services.AddHealthMonitoring(options =>
{
    options.MonitorApiUrl = builder.Configuration["Monitor:ApiUrl"]!;
    options.ServiceName   = "my-service";
    options.ServiceUrl    = "https://my-service.example.com";
});

var app = builder.Build();
app.MapHealthChecks("/health");
app.Run();
```

`HealthReportingService` starts automatically as a hosted background service — no additional wiring required.

## Dependency Detection

To have the library automatically detect and report HTTP dependencies, add `DependencyDetectionHandler` to any `HttpClient` you register:

```csharp
builder.Services.AddHttpClient<IOrdersClient, OrdersClient>()
    .AddHttpMessageHandler<DependencyDetectionHandler>();
```

Each unique base URL (scheme + authority) discovered via that client will be reported to the monitor once. Duplicate detection is thread-safe.

## Configuration

Pass an `Action<HealthMonitoringOptions>` to `AddHealthMonitoring()`, or bind from `appsettings.json`.

| Property | Type | Default | Required | Description |
|---|---|---|---|---|
| `MonitorApiUrl` | `string` | — | Yes | Base URL of the ServicePulseMonitor API |
| `ServiceName` | `string` | — | Yes | Unique name for this service in the monitor |
| `ServiceDescription` | `string?` | `null` | No | Human-readable description |
| `ServiceUrl` | `string?` | `null` | No | Public URL at which this service is reachable |
| `ReportInterval` | `TimeSpan` | `00:00:30` | No | How often to post a health report |
| `HealthCheckTimeout` | `TimeSpan` | `00:00:05` | No | Maximum time for health check evaluation |
| `EnableDependencyDetection` | `bool` | `true` | No | Whether to report discovered HTTP dependencies |
| `FailOnMonitorUnavailable` | `bool` | `false` | No | Stop the host if initial registration fails |
| `MaxRetries` | `int` | `3` | No | Maximum retry attempts per API call |
| `RetryDelay` | `TimeSpan` | `00:00:02` | No | Delay between retry attempts |

`MonitorApiUrl` and `ServiceName` are validated with `[Required]` data annotations. Missing values throw `OptionsValidationException` at host startup.

### Binding from appsettings.json

```json
{
  "HealthMonitoring": {
    "MonitorApiUrl": "https://monitor.example.com",
    "ServiceName": "order-service",
    "ServiceDescription": "Handles order processing",
    "ServiceUrl": "https://orders.example.com",
    "ReportInterval": "00:00:30"
  }
}
```

```csharp
builder.Services.AddHealthMonitoring(
    builder.Configuration.GetSection("HealthMonitoring").Bind);
```

## Models

### `ServiceHealthStatus`

Maps to the framework `HealthStatus` enum for reporting.

| Value | Meaning |
|---|---|
| `Healthy` | Service is operating normally |
| `Degraded` | Operational but with degraded performance or partial failures |
| `Unhealthy` | Not operational or failing health checks |

### Monitor API Endpoints

The client posts to the following endpoints on the monitor server:

| Method | Path | When |
|---|---|---|
| `POST` | `/api/services/register` | On host startup |
| `POST` | `/api/health/report` | Every `ReportInterval` |
| `POST` | `/api/dependencies/report` | On new dependency discovery |
| `DELETE` | `/api/services/{serviceName}` | On graceful shutdown |

All payloads use `application/json` via `System.Text.Json`.

## Logging

The library logs via the standard `ILogger<T>` abstraction:

| Level | Event |
|---|---|
| `Information` | Successful registration / deregistration |
| `Warning` | Non-2xx HTTP responses from the monitor API |
| `Error` | Exceptions during API calls or health check evaluation |
| `Critical` | Application stop triggered by `FailOnMonitorUnavailable` |

## Building & Testing

```bash
# Build
dotnet build ServicePulseMonitor.Client.sln

# Run tests (21 tests)
dotnet test ServicePulseMonitor.Client.sln

# Pack NuGet package
dotnet pack ServicePulseMonitor.Client/ServicePulseMonitor.Client.csproj --configuration Release
# Output: bin/Release/ServicePulseMonitor.Client.1.0.0.nupkg
```

Tests use **NUnit 3.14** and **Moq 4.18.4** and cover extension method DI registration, retry logic, health report generation, dependency detection and deduplication, and graceful shutdown.

## License

MIT
