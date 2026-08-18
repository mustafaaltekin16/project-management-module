using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Ozdilek.PM.BuildingBlocks.Web;

/// <summary>Every service exposes the same `/health` shape so the gateway and orchestrator (Docker Compose) can probe it consistently.</summary>
public static class HealthCheckExtensions
{
    public static IServiceCollection AddCwaHealthChecks(this IServiceCollection services)
    {
        services.AddHealthChecks();
        return services;
    }

    public static IEndpointRouteBuilder MapCwaHealthChecks(this IEndpointRouteBuilder app)
    {
        app.MapHealthChecks("/health");
        return app;
    }
}
