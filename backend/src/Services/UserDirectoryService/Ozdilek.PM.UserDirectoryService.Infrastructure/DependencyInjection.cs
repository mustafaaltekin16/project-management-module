using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Ozdilek.PM.SharedKernel.Persistence;
using Ozdilek.PM.UserDirectoryService.Application.Interfaces;
using Ozdilek.PM.UserDirectoryService.Application.Services;
using Ozdilek.PM.UserDirectoryService.Infrastructure.Persistence;
using Ozdilek.PM.UserDirectoryService.Infrastructure.Security;

namespace Ozdilek.PM.UserDirectoryService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddUserDirectoryServiceInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<UserDirectoryDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("UserDirectoryDatabase")));

        services.AddScoped<IEmployeeRepository, EmployeeRepository>();
        services.AddScoped<IDepartmentRepository, DepartmentRepository>();
        services.AddSingleton<IPasswordHasherService, PasswordHasherService>();
        services.AddScoped<IUnitOfWork, UserDirectoryUnitOfWork>();
        services.AddScoped<EmployeeAppService>();
        services.AddScoped<DepartmentAppService>();
        services.AddScoped<AuthAppService>();

        return services;
    }
}
