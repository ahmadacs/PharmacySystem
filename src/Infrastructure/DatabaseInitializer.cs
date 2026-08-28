using Infrastructure.Persistence;
using Infrastructure.Seeding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure;

public static class DatabaseInitializer
{
    /// <summary>
    /// Creates the schema (via migrations once they exist, otherwise the model),
    /// then applies the idempotent seed data. Called once at startup.
    /// </summary>
    public static async Task InitializeDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        if (db.Database.GetMigrations().Any())
            await db.Database.MigrateAsync(cancellationToken);
        else
            await db.Database.EnsureCreatedAsync(cancellationToken);

        await DbSeeder.SeedAsync(services, cancellationToken);
    }
}