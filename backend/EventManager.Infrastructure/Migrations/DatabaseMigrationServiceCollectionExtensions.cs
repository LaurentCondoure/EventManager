using Microsoft.Extensions.DependencyInjection;

namespace EventManager.Infrastructure.Migrations;

public static class DatabaseMigrationServiceCollectionExtensions
{
    public static IServiceCollection AddDatabaseMigrations(this IServiceCollection services)
    {
        services.AddHostedService<DatabaseMigrationHostedService>();
        return services;
    }
}