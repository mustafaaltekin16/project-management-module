using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Sinks.OpenSearch;

namespace Ozdilek.PM.BuildingBlocks.Logging;

/// <summary>
/// Centralized structured logging for every service. Always logs to console (and a rolling file);
/// additionally ships to OpenSearch when "Serilog:ElasticsearchUrl" is configured, so `docker compose up`
/// gives centralized log collection/visualization (OpenSearch Dashboards) out of the box while
/// `dotnet run` locally stays console-only.
///
/// Uses OpenSearch rather than a literal Elasticsearch/Kibana stack: this environment's network can
/// reach the docker.elastic.co registry's API but its backing blob/CDN storage drops mid-pull (verified
/// with an actual `docker compose up` attempt, not just a reachability probe). OpenSearch speaks the
/// same Elasticsearch bulk/REST protocol and is hosted on Docker Hub, which pulls reliably here. The
/// config key is deliberately still named "ElasticsearchUrl" so switching back (real Elastic sink +
/// docker.elastic.co images in docker-compose.yml) is a two-line change if this network constraint
/// changes.
/// </summary>
public static class CwaLoggingExtensions
{
    public static WebApplicationBuilder UseCwaSerilog(this WebApplicationBuilder builder, string serviceName)
    {
        var openSearchUrl = builder.Configuration["Serilog:ElasticsearchUrl"];

        builder.Host.UseSerilog((context, services, configuration) =>
        {
            configuration
                .Enrich.FromLogContext()
                .Enrich.WithProperty("Service", serviceName)
                .MinimumLevel.Information()
                .WriteTo.Console();

            configuration.WriteTo.File(
                path: $"logs/{serviceName}-.log",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 14);

            if (!string.IsNullOrWhiteSpace(openSearchUrl))
            {
                configuration.WriteTo.OpenSearch(new OpenSearchSinkOptions(new Uri(openSearchUrl))
                {
                    IndexFormat = $"cwa-logs-{serviceName.ToLowerInvariant()}-{{0:yyyy.MM.dd}}",
                    AutoRegisterTemplate = true,
                    NumberOfReplicas = 0,
                    EmitEventFailure = EmitEventFailureHandling.WriteToSelfLog
                });
            }
        });

        return builder;
    }
}
