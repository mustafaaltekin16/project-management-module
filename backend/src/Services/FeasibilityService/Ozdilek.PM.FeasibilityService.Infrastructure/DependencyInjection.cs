using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Ozdilek.PM.BuildingBlocks.Messaging;
using Ozdilek.PM.FeasibilityService.Application.Interfaces;
using Ozdilek.PM.FeasibilityService.Application.Services;
using Ozdilek.PM.FeasibilityService.Infrastructure.Persistence;
using Ozdilek.PM.SharedKernel.Persistence;

namespace Ozdilek.PM.FeasibilityService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddFeasibilityServiceInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<FeasibilityDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("FeasibilityDatabase")));

        services.AddScoped<IFeasibilityMainGroupRepository, FeasibilityMainGroupRepository>();
        services.AddScoped<IUnitOfWork, FeasibilityUnitOfWork>();
        services.AddScoped<FeasibilityAppService>();

        // Publish-only: FeasibilityService announces item/approval status changes (see
        // ProjectProgressInputsChangedEvent) so ProjectService can recompute progress/deviation, but
        // never consumes anything itself, so no consumer registration lambda is passed.
        services.AddCwaMessaging(configuration);

        return services;
    }
}
