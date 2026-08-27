using EventManager.Domain.Events.Interfaces;
using EventManager.Domain.Identity.Interfaces;
using EventManager.Infrastructure.Events;
using EventManager.Infrastructure.Identity;
using EventManager.Infrastructure.Mappings;
using EventManager.Infrastructure.Migrations;
using EventManager.Infrastructure.Options;
using EventManager.Infrastructure.Repositories;
using EventManager.Infrastructure.Search;

using Elastic.Clients.Elasticsearch;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using StackExchange.Redis;

namespace EventManager.Infrastructure.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        MongoDbMappings.Register();

        services.Configure<DatabaseOptions>(configuration.GetSection(DatabaseOptions.SectionName));
        services.Configure<RedisOptions>(configuration.GetSection(RedisOptions.SectionName));
        services.Configure<MongoDbOptions>(configuration.GetSection(MongoDbOptions.SectionName));
        services.Configure<ElasticsearchOptions>(configuration.GetSection(ElasticsearchOptions.SectionName));
        services.Configure<SeedAdminOptions>(options =>
        {
            options.Email = configuration["SEED_ADMIN_EMAIL"] ?? string.Empty;
            options.Password = configuration["SEED_ADMIN_PASSWORD"] ?? string.Empty;
        });

        services.AddDbContext<EventManagerIdentityDbContext>((sp, options) =>
            options.UseSqlServer(sp.GetRequiredService<IOptions<DatabaseOptions>>().Value.IdentityConnection));

        services.AddDbContext<EventManagerDbContext>((sp, options) =>
            options.UseSqlServer(sp.GetRequiredService<IOptions<DatabaseOptions>>().Value.DefaultConnection));

        services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
        {
            options.Password.RequiredLength         = 8;
            options.Password.RequireDigit           = true;
            options.Password.RequireLowercase       = true;
            options.Password.RequireUppercase       = false;
            options.Password.RequireNonAlphanumeric = false;

            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.DefaultLockoutTimeSpan  = TimeSpan.FromMinutes(5);
            options.Lockout.AllowedForNewUsers      = true;
            options.User.RequireUniqueEmail         = true;
        })
            .AddEntityFrameworkStores<EventManagerIdentityDbContext>()
            .AddDefaultTokenProviders();

        services.AddDatabaseMigrations();
        // Registered after AddDatabaseMigrations() — hosted services start in registration order,
        // and provisioning must run after Identity migrations are applied (ADR-017).
        services.AddHostedService<SuperAdminProvisioningHostedService>();
        services.AddScoped<IIdentityService, IdentityService>();

        services.AddSingleton<IConnectionMultiplexer>(sp =>
            ConnectionMultiplexer.Connect(sp.GetRequiredService<IOptions<RedisOptions>>().Value.ConnectionString));

        services.AddSingleton<IMongoClient>(sp =>
            new MongoClient(sp.GetRequiredService<IOptions<MongoDbOptions>>().Value.ConnectionString));

        services.AddSingleton<ElasticsearchClient>(sp =>
        {
            string url = sp.GetRequiredService<IOptions<ElasticsearchOptions>>().Value.Url;
            return new ElasticsearchClient(new ElasticsearchClientSettings(new Uri(url)));
        });

        services.AddScoped<IEventRepository>(sp =>
            new CachedEventRepository(
                new SqlServerEventRepository(sp.GetRequiredService<EventManagerDbContext>()),
                sp.GetRequiredService<IConnectionMultiplexer>(),
                sp.GetRequiredService<IOptions<RedisOptions>>()));
        services.AddScoped<ICommentRepository, MongoDbCommentRepository>();
        services.AddScoped<IEventSearchService, EventSearchService>();

        return services;
    }
}