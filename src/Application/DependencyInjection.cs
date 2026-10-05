using System.Reflection;
using Application.Common.Behaviours;
using Application.Common.Interfaces;
using Application.Features.Files.Common;
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
            cfg.AddOpenBehavior(typeof(CacheInvalidationBehaviour<,>));
            cfg.AddOpenBehavior(typeof(PrescriptionOwnershipBehavior<,>));
        });

        services.AddScoped<DispensingDomainService>();
        services.AddScoped<IFileAccessChecker, FileAccessChecker>();

        RegisterDomainEventHandlers(services);

        services.AddLocalization();

        return services;
    }

    private static void RegisterDomainEventHandlers(IServiceCollection services)
    {
        var handlerOpenType = typeof(IDomainEventHandler<>);
        var candidates = Assembly.GetExecutingAssembly().GetTypes()
            .Where(t => !t.IsAbstract && !t.IsInterface);

        foreach (var implementation in candidates)
        {
            var handlerInterfaces = implementation.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == handlerOpenType);

            foreach (var handlerInterface in handlerInterfaces)
                services.AddScoped(handlerInterface, implementation);
        }
    }
}