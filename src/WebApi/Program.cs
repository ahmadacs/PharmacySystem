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
using WebApi.Middleware;
using WebApi.Exceptions;
using WebApi.OpenApi;
using WebApi.Services;

var builder = WebApplication.CreateBuilder(args);

// Validate the whole service graph (including MediatR handlers) at startup so
// missing registrations like ICurrentUserService fail fast instead of at runtime.
builder.Host.UseDefaultServiceProvider(options =>
{
    options.ValidateOnBuild = true;
    options.ValidateScopes = true;
});

// ---------------------------------------------------------------------------
// Serilog structured logging (console + rolling file).
// ---------------------------------------------------------------------------
builder.Host.UseSerilog((context, services, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

var services = builder.Services;

services.AddHttpContextAccessor();
services.AddScoped<ICurrentUserService, CurrentUserService>();

// Read-heavy GETs only (medicines, inventory, low-stock). Named policies skip the
// default "no Authorization header" rule so JWT calls can be cached. Redis is used
// when ConnectionStrings:Redis is set; otherwise the in-memory store is used.
services.AddPharmacyOutputCache(builder.Configuration);

// ---------------------------------------------------------------------------
// MVC. Model validation failures flow through the standard error envelope.
// ---------------------------------------------------------------------------
// ---------------------------------------------------------------------------
// MVC. Model validation failures flow through the standard error envelope.
// ---------------------------------------------------------------------------
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

// ---------------------------------------------------------------------------
// API versioning: /api/v{version:apiVersion}/... with the version reported in
// responses (api-supported-versions) and each version grouped in the docs.
// ---------------------------------------------------------------------------
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

// ---------------------------------------------------------------------------
// Application + Infrastructure layers.
// ---------------------------------------------------------------------------
services.AddApplicationServices();
services.AddInfrastructureServices(builder.Configuration);

// ---------------------------------------------------------------------------
// CORS for the Angular origin. Never AllowAnyOrigin with credentials.
// ---------------------------------------------------------------------------
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

// ---------------------------------------------------------------------------
// Authentication: JWT bearer using the same issuer/audience/signing key the
// token service encodes tokens with.
// ---------------------------------------------------------------------------
var jwt = builder.Configuration.GetSection(Infrastructure.Identity.JwtOptions.SectionName)
    .Get<Infrastructure.Identity.JwtOptions>() ?? new Infrastructure.Identity.JwtOptions();

services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        // Keep the JWT claim names as issued (sub, role, email, ...). With the
        // default inbound mapping .NET rewrites `sub` to ClaimTypes.NameIdentifier,
        // which breaks CurrentUserService.UserId (null -> 403 on /auth/me).
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

        // WebSockets cannot send Authorization headers, so SignalR clients pass the
        // JWT as the "access_token" query string. Only honor it for /hubs paths.
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                    context.Token = accessToken;
                return Task.CompletedTask;
            }
        };
    });

services.AddAuthorization(options =>
    {
        // Combined policy: anyone who may read prescriptions at all (View) OR may
        // manage their own (ManageOwn) can reach the prescription endpoints. The
        // fine-grained "own records only" rule is enforced per-resource by the
        // PrescriptionResourceAuthorizationHandler (see Infrastructure.Services).
        options.AddPolicy("Prescriptions.ViewOrOwn", policy =>
            policy
                .RequireAuthenticatedUser()
                .RequireAssertion(ctx => ctx.User.Claims.Any(c =>
                    c.Type == CurrentUserService.PermissionClaimType &&
                    (c.Value == Permissions.Prescriptions.View ||
                     c.Value == Permissions.Prescriptions.ManageOwn))));
    });
services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();

// ---------------------------------------------------------------------------
// Rate limiting on authentication endpoints.
// ---------------------------------------------------------------------------
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

// ---------------------------------------------------------------------------
// Centralized exception handling -> standard error envelope.
// ---------------------------------------------------------------------------
services.AddExceptionHandler<GlobalExceptionHandler>();
services.AddProblemDetails();

services.AddHealthChecks();

var app = builder.Build();

app.UseSerilogRequestLogging();

// Request timing middleware: logs slow requests (threshold configurable).
app.UseMiddleware<RequestTimingMiddleware>();

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

// Scalar API docs UI (served at /scalar, reads the OpenAPI document).
app.MapScalarApiReference(options =>
{
    options.WithTitle("Pharmacy Inventory & Dispensing API")
        .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
});

// ---------------------------------------------------------------------------
// Apply pending migrations and seed idempotent reference data on startup.
// ---------------------------------------------------------------------------
await app.Services.InitializeDatabaseAsync();

app.Run();
