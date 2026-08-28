using System.Reflection;
using Application.Common.Behaviours;
using Domain.Services;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
            cfg.AddOpenBehavior(typeof(LoggingBehaviour<,>));
            cfg.AddOpenBehavior(typeof(CacheInvalidationBehaviour<,>));
            cfg.AddOpenBehavior(typeof(PrescriptionOwnershipBehavior<,>));
        });

        services.AddScoped<DispensingDomainService>();

        return services;
    }
}