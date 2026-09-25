using Infrastructure.Persistence;
using Infrastructure.Seeding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure;

public static class DatabaseInitializer
{
    /// <summary>
    /// Creates the schema then applies the idempotent seed data.
    /// Retries because the db container may not accept connections yet
    /// when the api starts; without this, one failed attempt crashes
    /// the process before Kestrel serves.
    /// </summary>
    public static async Task InitializeDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        const int maxAttempts = 10;
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                if (db.Database.GetMigrations().Any())
                    await db.Database.MigrateAsync(cancellationToken);
                else
                    await db.Database.EnsureCreatedAsync(cancellationToken);

                await DbSeeder.SeedAsync(services, cancellationToken);
                return;
            }
            catch when (attempt < maxAttempts)
            {
                // Transient startup failure (db not reachable yet) — wait and retry.
                await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
            }
        }
    }
}