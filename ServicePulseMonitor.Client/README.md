# ServicePulseMonitor.Client

A .NET 8 client library for integrating microservices with the ServicePulseMonitor health monitoring API.

## Quick Start

```csharp
// Program.cs / Startup.cs
builder.Services.AddHealthMonitoring(options =>
{
    options.MonitorApiUrl = "https://your-monitor-api.example.com";
    options.ServiceName   = "my-api";
    options.ServiceDescription = "My API service";
    options.ServiceUrl    = "https://my-api.example.com";
});
```

That's it. The library will:

1. Register your service with the monitor on startup.
2. Periodically report health status (default: every 30 seconds).
3. Deregister on graceful shutdown.

## Dependency Detection

To automatically report outgoing HTTP dependencies, add `DependencyDetectionHandler` to any `HttpClient` you configure:

```csharp
builder.Services.AddHttpClient<IMyServiceClient, MyServiceClient>()
    .AddHttpMessageHandler<DependencyDetectionHandler>();
```

> **Important:** Do *not* add `DependencyDetectionHandler` to the internal monitor client — only to your own application clients.

## Configuration Reference

| Property | Type | Default | Description |
|---|---|---|---|
| `MonitorApiUrl` | `string` | *(required)* | Base URL of the ServicePulseMonitor API |
| `ServiceName` | `string` | *(required)* | Unique name identifying this service |
| `ServiceDescription` | `string?` | `null` | Human-readable description |
| `ServiceUrl` | `string?` | `null` | Public URL of this service |
| `ReportInterval` | `TimeSpan` | `00:00:30` | How often health is reported |
| `HealthCheckTimeout` | `TimeSpan` | `00:00:05` | Timeout for each health check run |
| `EnableDependencyDetection` | `bool` | `true` | Enable/disable outgoing dependency tracking |
| `FailOnMonitorUnavailable` | `bool` | `false` | Stop the app if initial registration fails |
| `MaxRetries` | `int` | `3` | Retry attempts per API call |
| `RetryDelay` | `TimeSpan` | `00:00:02` | Delay between retries |
