# .NET Aspire Dashboard Integration for ClaimsIQ Platform

## Overview

This guide configures **OpenTelemetry** with the **.NET Aspire Dashboard** for comprehensive telemetry tracking in both local development and Azure production environments.

### What is .NET Aspire Dashboard?

The Aspire Dashboard is a standalone browser-based telemetry viewer that displays:
- **Distributed Traces**: Request flows across services
- **Metrics**: Performance counters, request rates, error rates
- **Logs**: Structured logging with correlation IDs
- **Resources**: Service health and dependencies

---

## 📦 Packages Installed

```xml
<PackageReference Include="OpenTelemetry.Extensions.Hosting" Version="1.9.0" />
<PackageReference Include="OpenTelemetry.Exporter.OpenTelemetryProtocol" Version="1.9.0" />
<PackageReference Include="OpenTelemetry.Instrumentation.Http" Version="1.9.0" />
```

---

## 🚀 Local Development Setup

### Step 1: Install .NET Aspire Dashboard (Standalone)

The Aspire Dashboard runs as a standalone Docker container:

```powershell
# Pull the Aspire Dashboard image
docker pull mcr.microsoft.com/dotnet/aspire-dashboard:8.0

# Run Aspire Dashboard on port 18888 (UI) and 4317 (OTLP gRPC)
docker run -d `
  -p 18888:18888 `
  -p 4317:4317 `
  --name aspire-dashboard `
  -e DOTNET_DASHBOARD_OTLP_ENDPOINT_URL=http://localhost:4317 `
  -e DOTNET_DASHBOARD_UNSECURED_ALLOW_ANONYMOUS=true `
  mcr.microsoft.com/dotnet/aspire-dashboard:8.0
```

**Dashboard URL**: http://localhost:18888

### Step 2: Verify Dashboard is Running

```powershell
# Check container status
docker ps | Select-String aspire-dashboard

# View logs
docker logs aspire-dashboard

# Stop dashboard
docker stop aspire-dashboard

# Start dashboard (if already created)
docker start aspire-dashboard

# Remove dashboard
docker rm -f aspire-dashboard
```

### Step 3: Run Your Blazor App

```powershell
cd C:\Git\AZ\azure-claims-rules-full-bundle\ClaimsPortal.BlazorWasm
dotnet run
```

Navigate to:
- **Blazor App**: http://localhost:5090
- **Aspire Dashboard**: http://localhost:18888

---

## 📊 Dashboard Features

### Traces Tab
View distributed traces for:
- HTTP requests to Azure Functions API
- FHIR server calls
- Cosmos DB operations
- Page navigation timing

**Example Trace**:
```
ClaimsPortal.BlazorWasm
  └─ GET /api/HealthCheck (200 OK) - 45ms
      └─ GET https://funcclaimstest001ncv.azurewebsites.net/api/health (200 OK) - 42ms
```

### Metrics Tab
Real-time metrics:
- `http.client.request.duration` - HTTP request latency histogram
- `http.client.active_requests` - Concurrent requests gauge
- `process.runtime.dotnet.gc.collections.count` - GC collections
- `process.runtime.dotnet.gc.heap.size` - Memory usage

### Logs Tab
Structured logs with correlation:
```json
{
  "Timestamp": "2026-01-10T14:30:00.123Z",
  "TraceId": "a1b2c3d4e5f6",
  "SpanId": "g7h8i9j0",
  "Level": "Information",
  "Message": "Processing claim submission",
  "ClaimId": "CLM-2024-001234",
  "MemberId": "MBR-56789"
}
```

### Resources Tab
Service health dashboard:
- ClaimsPortal.BlazorWasm (Browser)
- Azure Functions API
- FHIR Server
- Cosmos DB

---

## ☁️ Azure Production Setup

### Option 1: Azure Application Insights (Recommended)

Update [appsettings.Production.json](wwwroot/appsettings.Production.json):

```json
{
  "OpenTelemetry": {
    "ServiceName": "ClaimsPortal.BlazorWasm",
    "ServiceVersion": "1.0.0",
    "UseAzureMonitor": true,
    "EnableTracing": true,
    "EnableMetrics": true,
    "EnableLogging": true
  },
  "ApplicationInsights": {
    "ConnectionString": "InstrumentationKey=<your-key>;IngestionEndpoint=https://eastus-8.in.applicationinsights.azure.com/;LiveEndpoint=https://eastus.livediagnostics.monitor.azure.com/"
  }
}
```

**Install Azure Monitor Package**:
```powershell
dotnet add package Azure.Monitor.OpenTelemetry.Exporter --version 1.3.0
```

**Update Program.cs** (for Production only):
```csharp
if (builder.HostEnvironment.IsProduction())
{
    var aiConnectionString = builder.Configuration["ApplicationInsights:ConnectionString"];
    if (!string.IsNullOrEmpty(aiConnectionString))
    {
        builder.Services.AddOpenTelemetry()
            .UseAzureMonitor(options =>
            {
                options.ConnectionString = aiConnectionString;
            });
    }
}
```

### Option 2: Azure Container Instances (Self-Hosted Aspire Dashboard)

Deploy Aspire Dashboard to Azure Container Instances:

```powershell
az container create `
  --resource-group rg-claimsiq-prod `
  --name aspire-dashboard `
  --image mcr.microsoft.com/dotnet/aspire-dashboard:8.0 `
  --dns-name-label claimsiq-aspire `
  --ports 18888 4317 `
  --environment-variables `
    DOTNET_DASHBOARD_OTLP_ENDPOINT_URL=http://0.0.0.0:4317 `
    DOTNET_DASHBOARD_UNSECURED_ALLOW_ANONYMOUS=false `
    DOTNET_DASHBOARD_FRONTEND_AUTHMODE=BrowserToken `
  --cpu 1 --memory 1.5
```

**Access Dashboard**:
- URL: http://claimsiq-aspire.eastus.azurecontainer.io:18888
- Token: Retrieved from container logs

**Update appsettings.Production.json**:
```json
{
  "OpenTelemetry": {
    "OtlpEndpoint": "http://claimsiq-aspire.eastus.azurecontainer.io:4317"
  }
}
```

---

## 🔧 Configuration Reference

### appsettings.json (Current)

```json
{
  "OpenTelemetry": {
    "ServiceName": "ClaimsPortal.BlazorWasm",
    "ServiceVersion": "1.0.0",
    "OtlpEndpoint": "http://localhost:4317",
    "UseConsoleExporter": false,
    "EnableTracing": true,
    "EnableMetrics": true,
    "EnableLogging": true
  },
  "AspireDashboard": {
    "Url": "http://localhost:18888",
    "Enabled": true
  }
}
```

### Program.cs Configuration

```csharp
// OpenTelemetry with Aspire Dashboard
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource
        .AddService(serviceName: "ClaimsPortal.BlazorWasm", serviceVersion: "1.0.0")
        .AddAttributes(new Dictionary<string, object>
        {
            ["deployment.environment"] = builder.HostEnvironment.Environment,
            ["service.instance.id"] = Guid.NewGuid().ToString(),
            ["host.name"] = Environment.MachineName
        }))
    .WithTracing(tracing => tracing
        .AddSource("ClaimsPortal.BlazorWasm")
        .AddHttpClientInstrumentation()
        .AddOtlpExporter(options =>
        {
            options.Endpoint = new Uri("http://localhost:4317");
        }))
    .WithMetrics(metrics => metrics
        .AddHttpClientInstrumentation()
        .AddRuntimeInstrumentation()
        .AddOtlpExporter(options =>
        {
            options.Endpoint = new Uri("http://localhost:4317");
        }));
```

---

## 📈 Custom Instrumentation

### Add Custom Traces

Create a custom `ActivitySource`:

```csharp
using System.Diagnostics;

public class FraudDetectionService
{
    private static readonly ActivitySource ActivitySource = new("ClaimsPortal.FraudDetection");

    public async Task<FraudAnalysisResult> AnalyzeClaim(string claimId)
    {
        using var activity = ActivitySource.StartActivity("AnalyzeClaim");
        activity?.SetTag("claim.id", claimId);

        try
        {
            var result = await PerformAnalysis(claimId);
            activity?.SetTag("fraud.risk_score", result.RiskScore);
            activity?.SetStatus(ActivityStatusCode.Ok);
            return result;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
    }
}
```

**Register Activity Source**:
```csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddSource("ClaimsPortal.FraudDetection")
        .AddSource("ClaimsPortal.ClaimsEngine")
        .AddSource("ClaimsPortal.Eligibility"));
```

### Add Custom Metrics

```csharp
using System.Diagnostics.Metrics;

public class ClaimsApiService
{
    private static readonly Meter Meter = new("ClaimsPortal.Api");
    private readonly Counter<long> _claimSubmissions = Meter.CreateCounter<long>("claims.submitted");
    private readonly Histogram<double> _claimProcessingTime = Meter.CreateHistogram<double>("claims.processing_time");

    public async Task<ClaimResponse> SubmitClaim(ClaimRequest request)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/claims", request);
            _claimSubmissions.Add(1, new("status", "success"), new("claim_type", request.Type));
            _claimProcessingTime.Record(sw.Elapsed.TotalMilliseconds);
            return await response.Content.ReadFromJsonAsync<ClaimResponse>();
        }
        catch (Exception ex)
        {
            _claimSubmissions.Add(1, new("status", "error"), new("error_type", ex.GetType().Name));
            throw;
        }
    }
}
```

**Register Meter**:
```csharp
builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics => metrics
        .AddMeter("ClaimsPortal.Api")
        .AddMeter("ClaimsPortal.FraudDetection"));
```

---

## 🎯 Testing Telemetry

### Scenario 1: Submit a Claim

1. Navigate to **Claims Management** page
2. Fill out claim form and submit
3. Open Aspire Dashboard: http://localhost:18888
4. Go to **Traces** tab
5. Find trace: `POST /api/claims` (should see full request lifecycle)

### Scenario 2: Fraud Detection Analysis

1. Navigate to **Fraud Detection** page
2. Enter claim data and click "Analyze"
3. Check **Traces** for:
   - `POST /api/fraud/analyze`
   - Downstream ML model calls
   - Risk score calculation
4. Check **Metrics** for:
   - `fraud_detection.analysis_count`
   - `fraud_detection.high_risk_claims`

### Scenario 3: HEDIS Quality Measures

1. Navigate to **HEDIS Quality** page
2. Calculate quality metrics
3. Check **Traces** for:
   - Multiple FHIR queries (Patient, Coverage, Claim)
   - Aggregation logic timing
4. Check **Metrics** for:
   - `hedis.measure_calculations`
   - `hedis.data_gaps_identified`

---

## 🔍 Troubleshooting

### Dashboard Not Receiving Data

**Check OTLP Endpoint**:
```powershell
# Test OTLP gRPC endpoint
Test-NetConnection -ComputerName localhost -Port 4317
```

**Verify Browser Console**:
```
Failed to export telemetry: ERR_CONNECTION_REFUSED
```

**Solution**: Ensure Docker container is running:
```powershell
docker start aspire-dashboard
docker logs aspire-dashboard
```

### Missing Traces

**Issue**: No traces appear in dashboard

**Solution**: Add more Activity Sources:
```csharp
.WithTracing(tracing => tracing
    .AddSource("ClaimsPortal.*") // Wildcard for all sources
    .SetSampler(new AlwaysOnSampler())) // Sample 100% of traces
```

### High Memory Usage

**Issue**: Blazor WASM + telemetry uses too much memory

**Solution**: Reduce sampling rate in production:
```csharp
.WithTracing(tracing => tracing
    .SetSampler(new TraceIdRatioBasedSampler(0.1))) // Sample 10% of traces
```

---

## 📚 Resources

- [.NET Aspire Dashboard Docs](https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/dashboard/overview)
- [OpenTelemetry .NET SDK](https://opentelemetry.io/docs/languages/net/)
- [OTLP Exporter Configuration](https://opentelemetry.io/docs/specs/otel/protocol/exporter/)
- [Azure Monitor OpenTelemetry](https://learn.microsoft.com/en-us/azure/azure-monitor/app/opentelemetry-enable)

---

## 🎯 Next Steps

1. **Run Dashboard Locally**:
   ```powershell
   docker start aspire-dashboard
   dotnet run
   # Open http://localhost:18888
   ```

2. **Add Custom Traces**: Instrument critical code paths (fraud detection, claims processing)

3. **Configure Alerts**: Set up Azure Monitor alerts for:
   - High error rates (> 5%)
   - Slow requests (P95 > 2 seconds)
   - Memory leaks (heap growth > 200 MB/hour)

4. **Create Dashboards**: Build custom Grafana dashboards with OpenTelemetry metrics

---

**Last Updated**: January 2026  
**Platform**: .NET 10 Blazor WebAssembly  
**OpenTelemetry Version**: 1.9.0  
**Aspire Dashboard Version**: 8.0
