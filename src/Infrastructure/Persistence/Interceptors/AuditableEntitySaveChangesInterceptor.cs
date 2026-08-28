using System.Text.Json;
using Application.Common.Interfaces;
using Domain.Common;
using Domain.Entities.Audit;
using Domain.Entities.Notifications;
using Domain.Enums;
using Infrastructure.Identity;
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
        {
            ApplyAuditRulesAndSoftDelete(eventData.Context);
            RecordAuditEntries(eventData.Context);
        }

        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
        {
            ApplyAuditRulesAndSoftDelete(eventData.Context);
            RecordAuditEntries(eventData.Context);
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void ApplyAuditRulesAndSoftDelete(DbContext context)
    {
        var userId = _currentUser.UserId;
        var now = DateTime.UtcNow;

        foreach (var entry in context.ChangeTracker.Entries<BaseEntity>())
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

                case EntityState.Deleted:
                    entry.State = EntityState.Modified;
                    entry.Entity.IsDeleted = true;
                    entry.Entity.DeletedBy = userId;
                    entry.Entity.DeletedAt = now;
                    break;
            }
        }
    }

    /// <summary>
    /// Captures an audit row for every created/updated/soft-deleted domain entity.
    /// Runs AFTER ApplyAuditRulesAndSoftDelete so soft deletes (which arrive here
    /// as Modified with IsDeleted=true) are labelled "Deleted". Old/new values are
    /// recorded for scalar and EF complex-type properties.
    /// </summary>
    private void RecordAuditEntries(DbContext context)
    {
        var userId = _currentUser.UserId;
        var now = DateTime.UtcNow;
        var auditEntries = new List<AuditEntry>();

        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry.Entity is AuditEntry or RefreshToken or Notification or not BaseEntity)
                continue;

            var action = entry.State switch
            {
                EntityState.Added => AuditAction.Created,
                EntityState.Deleted => AuditAction.Deleted,
                EntityState.Modified when ((BaseEntity)entry.Entity).IsDeleted => AuditAction.Deleted,
                EntityState.Modified => AuditAction.Updated,
                _ => (AuditAction?)null
            };

            if (action is null)
                continue;

            var changesJson = action is AuditAction.Updated or AuditAction.Created ? CaptureChanges(entry, action.Value) : null;

            auditEntries.Add(new AuditEntry(
                entry.Entity.GetType().Name,
                ((BaseEntity)entry.Entity).Id,
                action.Value,
                userId,
                now,
                changesJson));
        }

        if (auditEntries.Count > 0)
            context.Set<AuditEntry>().AddRange(auditEntries);
    }

    private static string? CaptureChanges(EntityEntry entry, AuditAction action)
    {
        var changes = new List<AuditChangeRecord>();

        foreach (var property in entry.Properties)
        {
            var name = property.Metadata.Name;
            if (IsSkippedProperty(name, property.Metadata.ClrType))
                continue;

            var oldValue = property.OriginalValue;
            var newValue = property.CurrentValue;

            if (action == AuditAction.Created)
            {
                if (newValue is null)
                    continue;
                changes.Add(new AuditChangeRecord(name, null, FormatValue(newValue)));
                continue;
            }

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

        return changes.Count == 0 ? null : JsonSerializer.Serialize(changes, JsonOptions);
    }

    private static bool IsSkippedProperty(string name, Type clrType)
        => clrType == typeof(byte[])
            || name is nameof(BaseEntity.CreatedBy) or nameof(BaseEntity.CreatedAt)
                or nameof(BaseEntity.ModifiedBy) or nameof(BaseEntity.ModifiedAt)
                or nameof(BaseEntity.DeletedBy) or nameof(BaseEntity.DeletedAt)
                or nameof(BaseEntity.IsDeleted);

    private static string? FormatValue(object? value)
        => value switch
        {
            null => null,
            DateOnly date => date.ToString("yyyy-MM-dd"),
            DateTime dateTime => dateTime.ToString("yyyy-MM-dd HH:mm:ss"),
            bool boolean => boolean.ToString().ToLowerInvariant(),
            byte[] => "[binary]",
            _ => value.ToString()
        };

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private sealed record AuditChangeRecord(string Property, string? OldValue, string? NewValue);
}