using EventManager.Api;
using EventManager.Api.Auth;
using EventManager.Api.ExceptionHandlers;
using EventManager.Api.Validators;
using EventManager.Domain.Events.Interfaces;
using EventManager.Domain.Events.Services;
using EventManager.Domain.Identity.Interfaces;
using EventManager.Infrastructure.Identity;
using EventManager.Infrastructure.Mappings;
using EventManager.Infrastructure.Options;
using EventManager.Infrastructure.Repositories;
using EventManager.Infrastructure.Search;
using AppRateLimiterOptions = EventManager.Infrastructure.Options.RateLimiterOptions;

using System.Text;
using System.Threading.RateLimiting;

using Elastic.Clients.Elasticsearch;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
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
builder.Services.AddCors(options =>
{
    var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
    options.AddPolicy("Frontend", policy => policy
        .WithOrigins(origins)
        .AllowAnyHeader()
        .AllowAnyMethod());
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
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));

// Options are resolved from the built container (not read eagerly off builder.Configuration) so that
// test hosts overriding configuration via WithWebHostBuilder(...) are honoured — an eager read here
// would capture configuration as it stood before those overrides are spliced in.
builder.Services.AddDbContext<EventManagerIdentityDbContext>((sp, options) =>
    options.UseSqlServer(sp.GetRequiredService<IOptions<DatabaseOptions>>().Value.IdentityConnection));

// TECH-002 adds the RefreshTokens/PasswordHistory tables and the initial migration on this context.
builder.Services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
{
    // Password policy — minimal complexity per scoping-v1-user-management.md.
    options.Password.RequiredLength         = 8;
    options.Password.RequireDigit           = true;
    options.Password.RequireLowercase       = true;
    options.Password.RequireUppercase       = false;
    options.Password.RequireNonAlphanumeric = false;

    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan  = TimeSpan.FromMinutes(5);
    options.Lockout.AllowedForNewUsers      = true;

    options.User.RequireUniqueEmail = true;
})
    .AddEntityFrameworkStores<EventManagerIdentityDbContext>()
    .AddDefaultTokenProviders();

// AddIdentity() defaults the authentication scheme to its own cookie scheme (IdentityConstants.ApplicationScheme).
// ADR-014 uses JWT in httpOnly cookies instead, so JWT Bearer must be forced as the default scheme here.
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme    = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme             = JwtBearerDefaults.AuthenticationScheme;
})
    .AddJwtBearer()
    // Static system key (ADR-021) — lets an automated caller authenticate without the human
    // login/cookie flow. Not the default scheme: routes opt in explicitly alongside JWT Bearer.
    .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(
        ApiKeyAuthenticationDefaults.AuthenticationScheme, _ => { });

builder.Services.Configure<ApiKeyOptions>(builder.Configuration.GetSection(ApiKeyOptions.SectionName));

// Post-configure (rather than the AddJwtBearer(options => ...) delegate) so JwtOptions is resolved
// from the built container, honouring test-host configuration overrides — see the comment above
// AddDbContext<EventManagerIdentityDbContext> for why an eager builder.Configuration read is avoided.
builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<IOptions<JwtOptions>>((options, jwtOptions) =>
    {
        var jwtConfig = jwtOptions.Value;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidIssuer              = jwtConfig.Issuer,
            ValidateAudience         = true,
            ValidAudience            = jwtConfig.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtConfig.Secret)),
            ValidateLifetime         = true,
            ClockSkew                = TimeSpan.Zero
        };

        // The access token travels in an httpOnly cookie, never in the Authorization header (ADR-014).
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                if (context.Request.Cookies.TryGetValue(AuthCookieNames.AccessToken, out var accessToken))
                    context.Token = accessToken;

                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddScoped<IIdentityService, IdentityService>();

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

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

app.UseExceptionHandler();
app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/swagger/v1/swagger.json", "Events API v1"));
}

app.UseHttpsRedirection();
app.MapControllers();
app.MapMinimalApiEndpoints();

app.Run();

// Needed for integration tests
public partial class Program { }