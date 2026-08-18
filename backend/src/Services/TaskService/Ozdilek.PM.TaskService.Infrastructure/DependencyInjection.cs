using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Ozdilek.PM.BuildingBlocks.Messaging;
using Ozdilek.PM.SharedKernel.Persistence;
using Ozdilek.PM.TaskService.Application.Interfaces;
using Ozdilek.PM.TaskService.Application.Services;
using Ozdilek.PM.TaskService.Infrastructure.Messaging;
using Ozdilek.PM.TaskService.Infrastructure.Persistence;

namespace Ozdilek.PM.TaskService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddTaskServiceInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<TaskDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("TaskDatabase")));

        services.AddScoped<ITaskGroupRepository, TaskGroupRepository>();
        services.AddScoped<IProjectDocumentRepository, ProjectDocumentRepository>();
        services.AddScoped<IUnitOfWork, TaskUnitOfWork>();

        services.AddScoped<TaskAppService>();
        services.AddScoped<ProjectDocumentAppService>();

        services.AddCwaMessaging(configuration, bus =>
        {
            bus.AddConsumer<WorkPackageApprovedConsumer>();
            bus.AddConsumer<ProjectDepartmentsAssignedConsumer>();
        });

        return services;
    }
}
