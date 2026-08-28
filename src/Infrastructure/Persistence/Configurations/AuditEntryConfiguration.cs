using Domain.Entities.Audit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public sealed class AuditEntryConfiguration : IEntityTypeConfiguration<AuditEntry>
{
    public void Configure(EntityTypeBuilder<AuditEntry> builder)
    {
        builder.ToTable("AuditEntries");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.EntityName).HasMaxLength(200).IsRequired();
        builder.Property(a => a.Action).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(a => a.ChangesJson).HasColumnType("nvarchar(max)");
        builder.HasIndex(a => a.ChangedAt);
        builder.HasIndex(a => new { a.EntityName, a.EntityId });
    }
}