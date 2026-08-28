using System.Linq.Expressions;
using Application.Common.Interfaces;
using Domain.Common;
using Domain.Entities.Audit;
using Domain.Entities.Dispensing;
using Domain.Entities.Inventory;
using Domain.Entities.Files;
using Domain.Entities.Medicines;
using Domain.Entities.Notifications;
using Domain.Entities.Patients;
using Domain.Entities.Prescriptions;
using Domain.Entities.Staff;
using Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Infrastructure.Persistence;

/// <summary>
/// The DbContext acts as the Unit of Work: all entities changed inside one
/// request are committed atomically with a single SaveChangesAsync call. EF Core
/// wraps that call in an implicit database transaction, so no explicit
/// BeginTransaction/Commit/Rollback management is needed in handlers.
/// </summary>
public class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>, IUnitOfWork
{
    private readonly IDomainEventDispatcher _domainEventDispatcher;

    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        IDomainEventDispatcher domainEventDispatcher) : base(options)
    {
        _domainEventDispatcher = domainEventDispatcher;
    }

    public DbSet<Medicine> Medicines => Set<Medicine>();
    public DbSet<MedicineBatch> MedicineBatches => Set<MedicineBatch>();
    public DbSet<MedicineVariant> MedicineVariants => Set<MedicineVariant>();
    public DbSet<GenericName> GenericNames => Set<GenericName>();
    public DbSet<Doctor> Doctors => Set<Doctor>();
    public DbSet<Pharmacist> Pharmacists => Set<Pharmacist>();
    public DbSet<Prescription> Prescriptions => Set<Prescription>();
    public DbSet<PrescriptionItem> PrescriptionItems => Set<PrescriptionItem>();
    public DbSet<DispensingRecord> DispensingRecords => Set<DispensingRecord>();
    public DbSet<DispensingRecordItem> DispensingRecordItems => Set<DispensingRecordItem>();
    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<InventoryAdjustment> InventoryAdjustments => Set<InventoryAdjustment>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<FileAttachment> FileAttachments => Set<FileAttachment>();
    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        ApplySoftDeleteFilters(builder);
    }

    /// <summary>
    /// All DateTime values are stored and read as UTC: EF Core reads SQL Server
    /// datetime2 as DateTimeKind.Unspecified, which makes System.Text.Json omit the
    /// trailing "Z" and lets clients misparse the instant. These converters force
    /// every DateTime (and DateTime?) column to Kind=Utc on read and normalize to
    /// UTC on write, so the API always returns ISO-8601 with "Z". Display timezone
    /// conversion (Asia/Riyadh) happens only in the frontend.
    /// </summary>
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
        configurationBuilder.Properties<DateTime?>().HaveConversion<UtcDateTimeNullableConverter>();
    }

    private sealed class UtcDateTimeConverter : ValueConverter<DateTime, DateTime>
    {
        public UtcDateTimeConverter() : base(
            v => v.Kind == DateTimeKind.Utc
                ? v
                : v.Kind == DateTimeKind.Local
                    ? v.ToUniversalTime()
                    : DateTime.SpecifyKind(v, DateTimeKind.Utc),
            v => DateTime.SpecifyKind(v, DateTimeKind.Utc))
        { }
    }

    private sealed class UtcDateTimeNullableConverter : ValueConverter<DateTime?, DateTime?>
    {
        public UtcDateTimeNullableConverter() : base(
            v => v.HasValue
                ? v.Value.Kind == DateTimeKind.Utc
                    ? v
                    : v.Value.Kind == DateTimeKind.Local
                        ? v.Value.ToUniversalTime()
                        : DateTime.SpecifyKind(v.Value, DateTimeKind.Utc)
                : v,
            v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v)
        { }
    }

    /// <summary>
    /// Domain events raised by entities are dispatched only after the transaction
    /// commits, so a failed save never fires side effects. Note: if a notification
    /// handler itself fails, the commit has already completed (see README §15).
    /// </summary>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var domainEvents = CollectDomainEvents();
        var result = await base.SaveChangesAsync(cancellationToken);
        await DispatchDomainEventsAsync(domainEvents, cancellationToken);
        return result;
    }

    public override int SaveChanges()
    {
        var domainEvents = CollectDomainEvents();
        var result = base.SaveChanges();
        DispatchDomainEventsAsync(domainEvents, CancellationToken.None).GetAwaiter().GetResult();
        return result;
    }

    private List<object> CollectDomainEvents()
    {
        var entities = ChangeTracker.Entries<BaseEntity>().Select(e => e.Entity).ToList();
        var domainEvents = entities.SelectMany(e => e.DomainEvents).ToList();

        foreach (var entity in entities)
            entity.ClearDomainEvents();

        return domainEvents;
    }

    private Task DispatchDomainEventsAsync(List<object> domainEvents, CancellationToken cancellationToken)
        => domainEvents.Count == 0
            ? Task.CompletedTask
            : _domainEventDispatcher.DispatchAsync(domainEvents, cancellationToken);

    private static void ApplySoftDeleteFilters(ModelBuilder builder)
    {
        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            if (!typeof(ISoftDelete).IsAssignableFrom(entityType.ClrType))
                continue;

            var parameter = Expression.Parameter(entityType.ClrType, "e");
            var isDeletedProperty = Expression.Property(parameter, nameof(ISoftDelete.IsDeleted));
            var notDeleted = Expression.Lambda(Expression.Not(isDeletedProperty), parameter);

            builder.Entity(entityType.ClrType).HasQueryFilter(notDeleted);
        }
    }
}