using Application.Common.Interfaces;
using Application.Common.Options;
using Infrastructure.Identity;
using Infrastructure.Notifications;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Interceptors;
using Infrastructure.Repositories;
using Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<AuditableEntitySaveChangesInterceptor>();

        // Use DbContext pooling to reduce expensive context creation and
        // help with connection acquisition under load. The interceptor is
        // still registered through the factory overload.
        services.AddDbContext<ApplicationDbContext>((sp, options) =>
        {
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"));
            options.AddInterceptors(sp.GetRequiredService<AuditableEntitySaveChangesInterceptor>());
        });

        services
            .AddIdentityCore<ApplicationUser>(options =>
            {
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                options.Password.RequiredLength = 8;
            })
            .AddRoles<ApplicationRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<FileStorageOptions>(configuration.GetSection(FileStorageOptions.SectionName));
        services.AddScoped<IFileStorageService, FileSystemBlobStorageService>();

        services.AddSignalR();
        services.AddSingleton(_ => configuration.GetSection(NotificationOptions.SectionName)
            .Get<NotificationOptions>() ?? new NotificationOptions());

        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<IAuditRepository, AuditRepository>();

        services.AddScoped<IUserManager, UserManagerService>();
        services.AddScoped<ITokenService, JwtTokenService>();
        services.AddScoped<IEmailService, MockEmailService>();
        services.AddScoped<IStaffService, StaffService>();
        services.AddScoped<IAsyncQueryExecutor, EfQueryExecutor>();
        services.AddScoped<IMedicineRepository, MedicineRepository>();
        services.AddScoped<IPrescriptionRepository, PrescriptionRepository>();
        services.AddScoped<IPatientRepository, PatientRepository>();
        services.AddScoped<IFileAttachmentRepository, FileAttachmentRepository>();
        services.AddScoped<IExportDataProvider, ExportDataProvider>();
        services.AddScoped<IExportService, ExportService>();
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<ApplicationDbContext>());

        // Resource-based authorization: the implementation (and the ASP.NET Core
        // authorization handler it drives) live in Infrastructure so the Application
        // layer only sees the IResourceAuthorizationService abstraction.
        services.AddScoped<IAuthorizationHandler, PrescriptionResourceAuthorizationHandler>();
        services.AddScoped<IResourceAuthorizationService, ResourceAuthorizationService>();

        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();

        return services;
    }
}