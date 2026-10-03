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

        services.AddDbContext<ApplicationDbContext>((sp, options) =>
        {

            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"), sql =>
                sql.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(10),
                    errorNumbersToAdd: null));
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

        services.AddScoped<IUserManager, UserManagerService>();
        services.AddScoped<ITokenService, JwtTokenService>();
        services.AddScoped<IEmailService, MockEmailService>();
        services.AddScoped<IStaffService, StaffService>();
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped(typeof(IRepositoryWithSoftDelete<>), typeof(RepositoryWithSoftDelete<>));
        services.AddScoped<IPrescriptionRepository, PrescriptionRepository>();
        services.AddScoped(typeof(IRepositoryWithHardDelete<>), typeof(RepositoryWithHardDelete<>));
        services.AddScoped<IMedicineVariantRepository, MedicineVariantRepository>();
        services.AddScoped<IDashboardRepository, DashboardRepository>();
        services.AddScoped<IExportDataProvider, ExportDataProvider>();
        services.AddScoped<IExportService, ExportService>();
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<ApplicationDbContext>());

        services.AddScoped<IAuthorizationHandler, PrescriptionResourceAuthorizationHandler>();
        services.AddScoped<IResourceAuthorizationService, ResourceAuthorizationService>();

        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();

        return services;
    }
}
