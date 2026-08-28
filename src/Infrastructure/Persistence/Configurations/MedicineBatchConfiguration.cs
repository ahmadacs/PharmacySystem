using Domain.Entities.Medicines;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class MedicineBatchConfiguration : IEntityTypeConfiguration<MedicineBatch>
{
    public void Configure(EntityTypeBuilder<MedicineBatch> builder)
    {
        builder.ToTable("MedicineBatches");
        builder.HasKey(b => b.Id);

        builder.Property(b => b.BatchNumber).IsRequired().HasMaxLength(50);
        builder.Property(b => b.SupplierName).HasMaxLength(200);

        builder.ComplexProperty(b => b.QuantityReceived, cp =>
        {
            cp.Property(q => q.Value).HasColumnName("QuantityReceived").IsRequired();
        });

        builder.ComplexProperty(b => b.QuantityAvailable, cp =>
        {
            cp.Property(q => q.Value).HasColumnName("QuantityAvailable").IsRequired();
        });

        builder.ComplexProperty(b => b.UnitCost, cp =>
        {
            cp.Property(m => m.Amount).HasColumnName("UnitCostAmount").HasColumnType("decimal(18,2)").IsRequired();
            cp.Property(m => m.Currency).HasColumnName("UnitCostCurrency").HasMaxLength(3).IsRequired();
        });

        builder.Property(b => b.RowVersion).IsRowVersion();

        builder.HasIndex(b => b.ExpiryDate);
        builder.HasIndex(b => new { b.MedicineVariantId, b.ExpiryDate });
        builder.HasIndex(b => b.MedicineVariantId);

builder.HasOne(b => b.MedicineVariant)
            .WithMany(v => v.Batches)
            .HasForeignKey(b => b.MedicineVariantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}