using Application.Common.Caching;
using Application.Common.Interfaces;
using Microsoft.AspNetCore.OutputCaching;

namespace WebApi.Caching;

public static class OutputCacheServiceCollectionExtensions
{
    public static IServiceCollection AddPharmacyOutputCache(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Redis-backed distributed output cache when ConnectionStrings:Redis is set (Docker/production).
        // Falls back to in-memory when not configured (local dev without Redis).
        var redisConnection = configuration.GetConnectionString("Redis");
        if (!string.IsNullOrWhiteSpace(redisConnection))
        {
            services.AddStackExchangeRedisOutputCache(options =>
            {
                options.Configuration = redisConnection;
            });
        }

        services.AddOutputCache(options =>
        {
            options.AddPolicy(OutputCachePolicies.Medicines, builder =>
            {
                builder.AddPolicy<AllowAuthenticatedGetCachePolicy>();
                builder.Expire(TimeSpan.FromMinutes(5));
                builder.Tag(CacheTags.Medicines);
                builder.SetVaryByQuery(
                    "page", "pageSize", "search", "sortBy", "sortDir",
                    "categoryId", "form", "isActive");
            }, excludeDefaultPolicy: true);

            options.AddPolicy(OutputCachePolicies.Inventory, builder =>
            {
                builder.AddPolicy<AllowAuthenticatedGetCachePolicy>();
                builder.Expire(TimeSpan.FromSeconds(60));
                builder.Tag(CacheTags.Inventory);
                builder.SetVaryByQuery(
                    "page", "pageSize", "search", "sortBy", "sortDir",
                    "medicineId", "expiryStatus", "withinDays", "stockStatus", "status");
            }, excludeDefaultPolicy: true);
        });

        services.AddSingleton<ICacheInvalidator, OutputCacheInvalidator>();

        return services;
    }
}
