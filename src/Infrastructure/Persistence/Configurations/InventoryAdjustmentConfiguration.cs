using Domain.Entities.Inventory;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class InventoryAdjustmentConfiguration : IEntityTypeConfiguration<InventoryAdjustment>
{
    public void Configure(EntityTypeBuilder<InventoryAdjustment> builder)
    {
        builder.ToTable("InventoryAdjustments");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Type).HasConversion<string>().HasMaxLength(20);
        builder.Property(a => a.Reason).IsRequired().HasMaxLength(500);

        builder.HasOne(a => a.MedicineBatch)
            .WithMany()
            .HasForeignKey(a => a.MedicineBatchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(a => a.AdjustedAt);
        builder.HasIndex(a => a.MedicineBatchId);
    }
}
