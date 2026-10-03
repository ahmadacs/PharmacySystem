using System.Globalization;
using System.Text.Json;
using Application.Common.Interfaces;
using Domain.Common;
using Domain.Entities.Audit;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Infrastructure.Persistence.Interceptors;

public sealed class AuditableEntitySaveChangesInterceptor : SaveChangesInterceptor
{
    private readonly ICurrentUserService _currentUser;

    public AuditableEntitySaveChangesInterceptor(ICurrentUserService currentUser)
    {
        _currentUser = currentUser;
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        if (eventData.Context is not null)
            Process(eventData.Context);

        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
            Process(eventData.Context);

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    // Order is load-bearing and enforced here by construction:
    //   1. ApplySoftDeleteLifecycle (Deleted -> Modified + IsDeleted = true;
    //      IsDeleted true -> false detected as restore, DeletedBy/At cleared).
    //   2. ApplyAuditMetadata (converted/restored rows get ModifiedBy/ModifiedAt).
    //   3. CreateAuditEntries (sees final states, classifies Deleted vs Updated).
    // Do not reorder these calls.
    private void Process(DbContext context)
    {
        var userId = _currentUser.UserId;
        var now = DateTime.UtcNow;

        ApplySoftDeleteLifecycle(context, userId, now);
        ApplyAuditMetadata(context, userId, now);

        var auditEntries = CreateAuditEntries(context, userId, now);

        if (auditEntries.Count > 0)
            context.Set<AuditEntry>().AddRange(auditEntries);
    }

    private static void ApplySoftDeleteLifecycle(DbContext context, Guid? userId, DateTime now)
    {
        foreach (var entry in context.ChangeTracker.Entries<ISoftDelete>())
        {
            if (entry.State == EntityState.Deleted)
            {
                entry.State = EntityState.Modified;
                entry.Entity.IsDeleted = true;
                entry.Entity.DeletedBy = userId;
                entry.Entity.DeletedAt = now;
                continue;
            }

            if (entry.State == EntityState.Modified && IsRestoreTransition(entry, entry.Entity))
            {
                entry.Entity.DeletedBy = null;
                entry.Entity.DeletedAt = null;
            }
        }
    }

    private static void ApplyAuditMetadata(DbContext context, Guid? userId, DateTime now)
    {
        foreach (var entry in context.ChangeTracker.Entries<IAuditable>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedBy = userId;
                    entry.Entity.CreatedAt = now;
                    break;

                case EntityState.Modified:
                    entry.Entity.ModifiedBy = userId;
                    entry.Entity.ModifiedAt = now;
                    break;
            }
        }
    }

    private static List<AuditEntry> CreateAuditEntries(DbContext context, Guid? userId, DateTime now)
    {
        var auditEntries = new List<AuditEntry>();

        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified or EntityState.Deleted))
                continue;

            var entity = entry.Entity;

            if (entity is not IAuditable)
                continue;

            if (entity is not IEntity trackable)
                continue;

            var action = entry.State switch
            {
                EntityState.Added => AuditAction.Created,
                EntityState.Deleted => AuditAction.Deleted,
                EntityState.Modified when IsSoftDeleteTransition(entry, entity) => AuditAction.Deleted,
                EntityState.Modified => AuditAction.Updated,
                _ => (AuditAction?)null
            };

            if (action is null)
                continue;

            string? changesJson;
            if (action == AuditAction.Updated)
            {
                var isRestore = IsRestoreTransition(entry, entity);
                changesJson = CaptureChanges(entry, action.Value, isRestore);
                if (changesJson is null)
                    continue;
            }
            else if (action == AuditAction.Created)
            {
                changesJson = CaptureChanges(entry, action.Value);
            }
            else
            {
                changesJson = null;
            }

            auditEntries.Add(new AuditEntry(
                entity.GetType().Name,
                trackable.Id,
                action.Value,
                userId,
                now,
                changesJson));
        }

        return auditEntries;
    }

    private static bool IsSoftDeleteTransition(EntityEntry entry, object entity)
    {
        if (entity is not ISoftDelete softDelete || softDelete.IsDeleted is not true)
            return false;

        const string propertyName = nameof(ISoftDelete.IsDeleted);
        if (entry.Metadata.FindProperty(propertyName) is null)
            return false;

        var property = entry.Property(propertyName);
        return property.IsModified
            && property.CurrentValue is true
            && (property.OriginalValue is false || property.OriginalValue is null);
    }

    private static bool IsRestoreTransition(EntityEntry entry, object entity)
    {
        if (entity is not ISoftDelete softDelete || softDelete.IsDeleted is not false)
            return false;

        const string propertyName = nameof(ISoftDelete.IsDeleted);
        if (entry.Metadata.FindProperty(propertyName) is null)
            return false;

        var property = entry.Property(propertyName);
        return property.IsModified
            && property.CurrentValue is false
            && property.OriginalValue is true;
    }

    private static string? CaptureChanges(EntityEntry entry, AuditAction action, bool includeRestoreFlip = false)
    {
        var changes = new List<AuditChangeRecord>();

        foreach (var propertyMeta in entry.Metadata.GetProperties())
        {
            if (IsSkippedProperty(propertyMeta.Name, propertyMeta.ClrType))
                continue;

            var property = entry.Property(propertyMeta.Name);
            var name = propertyMeta.Name;

            if (action == AuditAction.Created)
            {
                var createdValue = property.CurrentValue;
                if (createdValue is null)
                    continue;
                changes.Add(new AuditChangeRecord(name, null, FormatValue(createdValue)));
                continue;
            }

            if (!property.IsModified)
                continue;

            var oldValue = property.OriginalValue;
            var newValue = property.CurrentValue;

            if (Equals(oldValue, newValue))
                continue;

            changes.Add(new AuditChangeRecord(name, FormatValue(oldValue), FormatValue(newValue)));
        }

        foreach (var complex in entry.ComplexProperties)
        {
            foreach (var property in complex.Properties)
            {
                if (property.Metadata.ClrType == typeof(byte[]))
                    continue;

                var oldValue = property.OriginalValue;
                var newValue = property.CurrentValue;

                if (action == AuditAction.Created)
                {
                    if (newValue is null)
                        continue;
                    changes.Add(new AuditChangeRecord(
                        $"{complex.Metadata.Name}.{property.Metadata.Name}", null, FormatValue(newValue)));
                    continue;
                }

                if (Equals(oldValue, newValue))
                    continue;

                changes.Add(new AuditChangeRecord(
                    $"{complex.Metadata.Name}.{property.Metadata.Name}",
                    FormatValue(oldValue),
                    FormatValue(newValue)));
            }
        }

        if (includeRestoreFlip)
            changes.Add(new AuditChangeRecord(nameof(ISoftDelete.IsDeleted), FormatValue(true), FormatValue(false)));

        return changes.Count == 0 ? null : JsonSerializer.Serialize(changes, JsonOptions);
    }

    private static bool IsSkippedProperty(string name, Type clrType)
        => clrType == typeof(byte[])
            || name is nameof(IAuditable.CreatedBy) or nameof(IAuditable.CreatedAt)
                or nameof(IAuditable.ModifiedBy) or nameof(IAuditable.ModifiedAt)
                or nameof(ISoftDelete.DeletedBy) or nameof(ISoftDelete.DeletedAt)
                or nameof(ISoftDelete.IsDeleted);

    private static string? FormatValue(object? value)
        => value switch
        {
            null => null,
            DateOnly date => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            DateTime dateTime => dateTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
            bool boolean => boolean.ToString().ToLowerInvariant(),
            byte[] => "[binary]",
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString()
        };

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private sealed record AuditChangeRecord(string Property, string? OldValue, string? NewValue);
}