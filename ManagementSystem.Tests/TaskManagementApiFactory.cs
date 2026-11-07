using ManagementSystem.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace ManagementSystem.Tests;

public sealed class TaskManagementApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"task-management-tests-{Guid.NewGuid()}";

    static TaskManagementApiFactory()
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
        Environment.SetEnvironmentVariable("Jwt__Key", "test-only-signing-key-with-more-than-thirty-two-bytes");
        Environment.SetEnvironmentVariable("Jwt__Issuer", "TaskManagementSystem.Tests");
        Environment.SetEnvironmentVariable("Jwt__Audience", "TaskManagementSystem.Tests.Client");
        Environment.SetEnvironmentVariable("Database__MigrateOnStartup", "false");
        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", "Host=localhost;Database=unused_in_tests");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureLogging(logging =>
        {
            logging.ClearProviders();
            logging.SetMinimumLevel(LogLevel.Warning);
            logging.AddFilter("Microsoft.AspNetCore.DataProtection", LogLevel.Critical);
        });
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "test-only-signing-key-with-more-than-thirty-two-bytes",
                ["Jwt:Issuer"] = "TaskManagementSystem.Tests",
                ["Jwt:Audience"] = "TaskManagementSystem.Tests.Client",
                ["Database:MigrateOnStartup"] = "false",
                ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=unused_in_tests"
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll(typeof(IDbContextOptionsConfiguration<TaskManagementDbContext>));
            services.RemoveAll<DbContextOptions<TaskManagementDbContext>>();
            services.RemoveAll<TaskManagementDbContext>();
            services.AddDbContext<TaskManagementDbContext>(options =>
                options.UseInMemoryDatabase(_databaseName));
        });
    }

    public async Task ResetDatabaseAsync(CancellationToken cancellationToken)
    {
        await using var scope = Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TaskManagementDbContext>();
        await dbContext.Database.EnsureDeletedAsync(cancellationToken);
        await dbContext.Database.EnsureCreatedAsync(cancellationToken);
    }
}
