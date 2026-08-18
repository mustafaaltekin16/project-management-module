using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Ozdilek.PM.ProjectService.Application.Interfaces;
using Ozdilek.PM.ProjectService.Application.Services;
using Ozdilek.PM.ProjectService.Infrastructure.Clients;
using Ozdilek.PM.ProjectService.Infrastructure.Persistence;
using Ozdilek.PM.BuildingBlocks.Messaging;
using Ozdilek.PM.BuildingBlocks.Web;
using Ozdilek.PM.SharedKernel.Persistence;

namespace Ozdilek.PM.ProjectService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddProjectServiceInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ProjectDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("ProjectDatabase")));

        services.AddScoped<IProjectRepository, ProjectRepository>();
        services.AddScoped<IProjectTemplateRepository, ProjectTemplateRepository>();
        services.AddScoped<IProjectBoardColumnRepository, ProjectBoardColumnRepository>();
        services.AddScoped<IUnitOfWork, ProjectUnitOfWork>();

        services.AddScoped<ProjectAppService>();
        services.AddScoped<ProjectTimelineAppService>();
        services.AddScoped<ProjectTemplateAppService>();
        services.AddScoped<ProjectBoardAppService>();
        services.AddScoped<ProjectProgressAppService>();

        // Consumes ProjectProgressInputsChangedEvent (published by TaskService/FeasibilityService whenever
        // something that feeds progress/deviation changes) in addition to publishing project creation
        // (see ProjectDepartmentsAssignedEvent).
        services.AddCwaMessaging(configuration, bus =>
        {
            bus.AddConsumer<Messaging.ProjectProgressInputsChangedConsumer>();
        });
        services.AddHostedService<ProjectProgressRecomputeJob>();

        services.AddHttpContextAccessor();
        services.AddTransient<BearerTokenForwardingHandler>();
        services.AddTransient<SystemAuthTokenHandler>();
        services.AddHttpClient<IFeasibilityInfoClient, FeasibilityServiceClient>(client =>
        {
            client.BaseAddress = new Uri(configuration["Services:FeasibilityService"] ?? "http://feasibilityservice-api:8080");
        }).AddHttpMessageHandler<BearerTokenForwardingHandler>();
        services.AddHttpClient<IUserDirectoryClient, UserDirectoryClient>(client =>
        {
            client.BaseAddress = new Uri(configuration["Services:UserDirectoryService"] ?? "http://localhost:6005");
        }).AddHttpMessageHandler<BearerTokenForwardingHandler>();
        // SystemAuthTokenHandler (not the plain forwarding one): these two are now also called from
        // ProjectProgressAppService with no real user request behind it (event consumer, nightly job) —
        // see ProjectProgressInputsChangedConsumer / ProjectProgressRecomputeJob.
        services.AddHttpClient<IProjectTaskTimelineClient, ProjectTaskTimelineClient>(client =>
        {
            client.BaseAddress = new Uri(configuration["Services:TaskService"] ?? "http://localhost:6002");
        }).AddHttpMessageHandler<SystemAuthTokenHandler>();
        services.AddHttpClient<IProjectFeasibilityTimelineClient, ProjectFeasibilityTimelineClient>(client =>
        {
            client.BaseAddress = new Uri(configuration["Services:FeasibilityService"] ?? "http://localhost:6003");
        }).AddHttpMessageHandler<SystemAuthTokenHandler>();

        return services;
    }
}
