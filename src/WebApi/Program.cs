using System.Security.Claims;
using System.Text;
using Application;
using Application.Common.Interfaces;
using Application.Common.Security;
using Asp.Versioning;
using Infrastructure;
using Infrastructure.Notifications;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using Serilog;
using WebApi.Authorization;
using WebApi.Caching;
using WebApi.Common;
using WebApi.Exceptions;
using WebApi.OpenApi;
using WebApi.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseDefaultServiceProvider(options =>
{
    options.ValidateOnBuild = true;
    options.ValidateScopes = true;
});

builder.Host.UseSerilog((context, services, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

var services = builder.Services;

services.AddHttpContextAccessor();
services.AddScoped<ICurrentUserService, CurrentUserService>();

services.AddPharmacyOutputCache(builder.Configuration);

services
    .AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()))
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState
                .Where(kv => kv.Value?.Errors.Count > 0)
                .ToDictionary(
                    kv => kv.Key,
                    kv => kv.Value!.Errors.Select(e => e.ErrorMessage).ToArray());

            var response = new ErrorResponse
            {
                Message = "Validation failed",
                Errors = errors
            };

            return new BadRequestObjectResult(response);
        };
    });

services.AddEndpointsApiExplorer();
services.AddOpenApi(options =>
{
    options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
    options.AddOperationTransformer<XmlCommentsOperationTransformer>();
});

services.AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new ApiVersion(1, 0);
        options.ReportApiVersions = true;
        options.ApiVersionReader = ApiVersionReader.Combine(new UrlSegmentApiVersionReader());
    })
    .AddMvc()
    .AddApiExplorer(options =>
    {
        options.GroupNameFormat = "'v'VVV";
        options.SubstituteApiVersionInUrl = true;
    });

services.AddApplicationServices();
services.AddInfrastructureServices(builder.Configuration);

services.AddCors(options =>
{
    options.AddPolicy("Angular", policy =>
    {
        var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? ["http://localhost:4200"];

        policy.WithOrigins(origins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

var jwt = builder.Configuration.GetSection(Infrastructure.Identity.JwtOptions.SectionName)
    .Get<Infrastructure.Identity.JwtOptions>() ?? new Infrastructure.Identity.JwtOptions();

services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {

        options.MapInboundClaims = false;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
            RoleClaimType = WebApi.Services.CurrentUserService.RoleClaimType,
            NameClaimType = System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Name
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                    context.Token = accessToken;
                return Task.CompletedTask;
            },
            OnTokenValidated = async context =>
            {
                var subject = context.Principal?
                    .FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;
                if (!Guid.TryParse(subject, out var userId))
                {
                    context.Fail("The token subject is missing or invalid.");
                    return;
                }

                if (context.Principal?.Identity is not ClaimsIdentity identity)
                    return;

                var users = context.HttpContext.RequestServices.GetRequiredService<IUserManager>();
                var account = await users.FindAsync(userId, context.HttpContext.RequestAborted);
                if (account is null || !account.IsActive)
                {
                    context.Fail("The account is no longer active.");
                    return;
                }

                foreach (var permission in account.Permissions)
                    identity.AddClaim(new Claim(JwtClaimTypes.Permission, permission));
            }
        };
    });

services.AddAuthorization();
services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();

services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddFixedWindowLimiter("auth", limiter =>
    {
        limiter.PermitLimit = 10;
        limiter.Window = TimeSpan.FromMinutes(1);
        limiter.QueueLimit = 0;
    });
});

services.AddExceptionHandler<GlobalExceptionHandler>();
services.AddProblemDetails();

services.AddHealthChecks().AddCheck<WebApi.HealthChecks.RedisHealthCheck>("redis");

var app = builder.Build();

app.UseSerilogRequestLogging();

var supportedCultures = new[] { "en", "ar" };
app.UseRequestLocalization(new RequestLocalizationOptions()
    .SetDefaultCulture(supportedCultures[0])
    .AddSupportedCultures(supportedCultures)
    .AddSupportedUICultures(supportedCultures));

app.UseExceptionHandler();

app.UseRouting();

app.UseCors("Angular");

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.UseOutputCache();

app.MapOpenApi();
app.MapControllers();
app.MapHealthChecks("/health");
app.MapHub<NotificationsHub>("/hubs/notifications");

app.MapScalarApiReference(options =>
{
    options.WithTitle("Pharmacy Inventory & Dispensing API")
        .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
});

await app.Services.InitializeDatabaseAsync();

app.Run();
