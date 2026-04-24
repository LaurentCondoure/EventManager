
using EventManager.Api.ExceptionHandlers;
using EventManager.Api.Validators;
using EventManager.Domain.Interfaces;
using EventManager.Domain.Services;
using EventManager.Infrastructure.Factories;
using EventManager.Infrastructure.Options;
using EventManager.Infrastructure.Repositories;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.Extensions.Options;
using Serilog;
using StackExchange.Redis;

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

var redisConnectionString = builder.Configuration.GetConnectionString("Redis")
    ?? "localhost:6379";
builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
    ConnectionMultiplexer.Connect(redisConnectionString));

builder.Services.AddScoped<IDbConnectionFactory>(sp =>
                                                    new DbConnectionFactory(
                                                        sp.GetRequiredService<IOptions<DatabaseOptions>>(),
                                                        DbProvider.SqlServer
                                                    )
                                                );
builder.Services.AddScoped<IEventRepository, EventRepository>();
builder.Services.Decorate<IEventRepository, CachedEventRepository>();

builder.Services.AddScoped<IEventService, EventService>();

WebApplication app = builder.Build();

app.UseExceptionHandler();
app.UseSwagger();
app.UseSwaggerUI(options => options.SwaggerEndpoint("/swagger/v1/swagger.json", "Events API v1"));
app.UseHttpsRedirection();
app.MapControllers();

app.Run();



