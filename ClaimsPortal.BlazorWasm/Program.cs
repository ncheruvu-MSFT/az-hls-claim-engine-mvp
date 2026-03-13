using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using ClaimsPortal.BlazorWasm;
using ClaimsPortal.BlazorWasm.Services;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Register HttpClient
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// Add configuration
builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
{
    ["ApiBaseUrl"] = "http://localhost:7071/api",  // Azure Functions local endpoint
    ["FhirServerUrl"] = "https://ahdswstest001-fhirr4test001.fhir.azurehealthcareapis.com",
    ["CosmosDb:Endpoint"] = "https://localhost:8081",  // Local CosmosDB Emulator
    ["CosmosDb:Key"] = "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==",  // Emulator default key
    ["CosmosDb:DatabaseId"] = "ClaimsIQDB",
    ["CosmosDb:ContainerIds:Accumulators"] = "Accumulators",
    ["CosmosDb:ContainerIds:Claims"] = "Claims"
});

// Configure OpenTelemetry for Aspire Dashboard
var otlpEndpoint = builder.Configuration["OpenTelemetry:OtlpEndpoint"] ?? "http://localhost:4317";
var serviceName = builder.Configuration["OpenTelemetry:ServiceName"] ?? "ClaimsPortal.BlazorWasm";
var serviceVersion = builder.Configuration["OpenTelemetry:ServiceVersion"] ?? "1.0.0";

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource
        .AddService(serviceName: serviceName, serviceVersion: serviceVersion)
        .AddAttributes(new Dictionary<string, object>
        {
            ["deployment.environment"] = builder.HostEnvironment.Environment,
            ["service.instance.id"] = Guid.NewGuid().ToString(),
            ["host.name"] = Environment.MachineName
        }))
    .WithTracing(tracing => tracing
        .AddSource(serviceName)
        .AddHttpClientInstrumentation()
        .AddOtlpExporter(options =>
        {
            options.Endpoint = new Uri(otlpEndpoint);
        }))
    .WithMetrics(metrics => metrics
        .AddHttpClientInstrumentation()
        .AddRuntimeInstrumentation()
        .AddOtlpExporter(options =>
        {
            options.Endpoint = new Uri(otlpEndpoint);
        }));

// Register application services
builder.Services.AddScoped<ClaimsApiService>();
builder.Services.AddScoped<NaturalLanguageService>();
builder.Services.AddScoped<FraudDetectionService>();
builder.Services.AddScoped<HedisCalculationService>();
builder.Services.AddScoped<MemberPortalService>();
builder.Services.AddScoped<ProviderPortalService>();
builder.Services.AddScoped<AuditLogService>();
builder.Services.AddScoped<EOBDocumentService>();
builder.Services.AddScoped<MdmService>();
builder.Services.AddScoped<ActuarialService>();
builder.Services.AddScoped<SdohService>();

// Register data services (with FHIR and Cosmos DB)
builder.Services.AddScoped<FhirDataService>();
builder.Services.AddScoped<CosmosDbService>();
builder.Services.AddScoped<BenefitPlanService>();
builder.Services.AddScoped<AccountService>();

await builder.Build().RunAsync();
