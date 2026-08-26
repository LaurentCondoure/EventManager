using EventManager.Api;
using EventManager.Api.Auth;
using EventManager.Api.ExceptionHandlers;
using EventManager.Api.Validators;
using EventManager.Domain.Events.Interfaces;
using EventManager.Domain.Events.Services;
using EventManager.Infrastructure.DependencyInjection;
using EventManager.Infrastructure.Options;
using AppRateLimiterOptions = EventManager.Infrastructure.Options.RateLimiterOptions;

using System.Text;
using System.Threading.RateLimiting;

using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Serilog;

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

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
builder.Services.AddInfrastructure(builder.Configuration);

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