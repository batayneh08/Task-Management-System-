using ManagementSystem.Application.Interfaces;
using ManagementSystem.Infrastructure.Data;
using ManagementSystem.Infrastructure.Authentication;
using ManagementSystem.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ManagementSystem.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Database connection string 'DefaultConnection' is missing.");
        }

        services.AddDbContext<TaskManagementDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<DatabaseSeeder>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ITaskRepository, TaskRepository>();
        services.AddScoped<IActivityLogRepository, ActivityLogRepository>();
        services.AddScoped<IPasswordHasher, IdentityPasswordHasher>();

        return services;
    }
}
