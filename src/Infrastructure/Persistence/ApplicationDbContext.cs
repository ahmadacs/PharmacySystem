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

namespace Infrastructure.Persistence;

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

    private List<IDomainEvent> CollectDomainEvents()
    {
        var entities = ChangeTracker.Entries<AggregateRoot>().Select(e => e.Entity).ToList();
        var domainEvents = entities.SelectMany(e => e.DomainEvents).ToList();

        foreach (var entity in entities)
            entity.ClearDomainEvents();

        return domainEvents;
    }

    private Task DispatchDomainEventsAsync(List<IDomainEvent> domainEvents, CancellationToken cancellationToken)
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