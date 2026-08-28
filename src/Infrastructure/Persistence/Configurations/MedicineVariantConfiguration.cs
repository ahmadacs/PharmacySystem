using Domain.Entities.Medicines;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class MedicineVariantConfiguration : IEntityTypeConfiguration<MedicineVariant>
{
    public void Configure(EntityTypeBuilder<MedicineVariant> builder)
    {
        builder.ToTable("MedicineVariants");
        builder.HasKey(v => v.Id);

        builder.Property(v => v.Form).IsRequired();
        builder.Property(v => v.Unit).IsRequired();
        builder.Property(v => v.Strength).HasPrecision(18, 2).IsRequired();

        builder.ComplexProperty(v => v.UnitOfMeasure, cp =>
        {
            cp.Property(u => u.BaseUnitName).HasColumnName("BaseUnitName").IsRequired().HasMaxLength(50);
            cp.Property(u => u.PackageUnitName).HasColumnName("PackageUnitName").IsRequired().HasMaxLength(50);
            cp.Property(u => u.UnitsPerPackage).HasColumnName("UnitsPerPackage").IsRequired();
            cp.Property(u => u.IsDivisible).HasColumnName("IsDivisible").IsRequired();
        });

        builder.HasOne(v => v.Medicine)
            .WithMany(m => m.Variants)
            .HasForeignKey(v => v.MedicineId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(v => v.MedicineId);
        builder.HasIndex(v => new { v.MedicineId, v.Form, v.Unit, v.Strength }).IsUnique();
        // Index on active flag to speed up queries that filter active variants
        builder.HasIndex(v => v.IsActive);
    }
}
