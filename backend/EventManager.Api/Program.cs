using EventManager.Api;
using EventManager.Api.ExceptionHandlers;
using EventManager.Api.Validators;
using EventManager.Domain.Interfaces;
using EventManager.Domain.Services;
using EventManager.Infrastructure.Factories;
using EventManager.Infrastructure.Options;
using EventManager.Infrastructure.Repositories;
using EventManager.Infrastructure.Search;
using AppRateLimiterOptions = EventManager.Infrastructure.Options.RateLimiterOptions;

using System.Threading.RateLimiting;

using Elastic.Clients.Elasticsearch;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using EventManager.Infrastructure.Mappings;

using MongoDB.Driver;
using Serilog;
using StackExchange.Redis;

MongoDbMappings.Register();

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .WriteTo.Console());

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title       = "Events API",
        Version     = "v1",
        Description = "API de gestion d'événements culturels"
    });
});
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<CreateEventInputValidator>();
builder.Services.AddExceptionHandler<BadRequestExceptionHandler>();
builder.Services.AddExceptionHandler<NotFoundExceptionHandler>();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.Configure<DatabaseOptions>(builder.Configuration.GetSection(DatabaseOptions.SectionName));
builder.Services.Configure<RedisOptions>(builder.Configuration.GetSection(RedisOptions.SectionName));
builder.Services.Configure<MongoDbOptions>(builder.Configuration.GetSection(MongoDbOptions.SectionName));
builder.Services.Configure<ElasticsearchOptions>(builder.Configuration.GetSection(ElasticsearchOptions.SectionName));

builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
    ConnectionMultiplexer.Connect(sp.GetRequiredService<IOptions<RedisOptions>>().Value.ConnectionString));

builder.Services.AddSingleton<IMongoClient>(sp =>
    new MongoClient(sp.GetRequiredService<IOptions<MongoDbOptions>>().Value.ConnectionString));

builder.Services.AddSingleton<ElasticsearchClient>(sp =>
{
    var url = sp.GetRequiredService<IOptions<ElasticsearchOptions>>().Value.Url;
    var settings = new ElasticsearchClientSettings(new Uri(url));
    return new ElasticsearchClient(settings);
});

builder.Services.AddScoped<IDbConnectionFactory>(sp =>
    new DbConnectionFactory(sp.GetRequiredService<IOptions<DatabaseOptions>>()));
builder.Services.AddScoped<IEventRepository, SqlServerEventRepository>();
builder.Services.Decorate<IEventRepository, CachedEventRepository>();

//AddRateLimiter does not accept a factory with IServiceProvider.
//You need to read the configuration directly from builder.Configuration before building the container
AppRateLimiterOptions rateLimiterConfig = builder.Configuration
    .GetSection(AppRateLimiterOptions.SectionName)
    .Get<AppRateLimiterOptions>() ?? new AppRateLimiterOptions();

builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("fixed", policy =>
    {
        policy.PermitLimit          = rateLimiterConfig.PermitLimit;
        policy.Window               = TimeSpan.FromMinutes(rateLimiterConfig.WindowMinutes);
        policy.QueueLimit           = rateLimiterConfig.QueueLimit;
        policy.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
    });
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

builder.Services.AddScoped<ICommentRepository, MongoDbCommentRepository>();
builder.Services.AddScoped<IEventSearchService, EventSearchService>();
builder.Services.AddScoped<IEventService, EventService>();

WebApplication app = builder.Build();

app.UseExceptionHandler();
app.UseRateLimiter();
app.UseSwagger();
app.UseSwaggerUI(options => options.SwaggerEndpoint("/swagger/v1/swagger.json", "Events API v1"));
app.UseHttpsRedirection();
app.MapControllers();
app.MapMinimalApiEndpoints();

app.Run();



// Needed for integration tests
public partial class Program { }