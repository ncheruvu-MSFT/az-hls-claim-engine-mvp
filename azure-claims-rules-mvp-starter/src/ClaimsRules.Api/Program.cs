
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using ClaimsRules.Api.Services;

var host = new HostBuilder()
    .ConfigureAppConfiguration(c =>
    {
        c.AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
         .AddEnvironmentVariables();
    })
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices((ctx, services) =>
    {
        services.AddHttpClient();
        services.AddSingleton<FhirClient>();
        services.AddSingleton<CosmosAudit>();
        services.AddSingleton<EventPublisher>();
        services.AddSingleton<AuditService>();
        services.AddSingleton<ProviderNetworkService>();
        services.AddSingleton<MdmMatchingService>();
        services.AddSingleton<MdmLinkService>();
        services.AddSingleton<AIMatchingService>();
        services.AddSingleton<HedisService>();
    })
    .Build();

host.Run();
