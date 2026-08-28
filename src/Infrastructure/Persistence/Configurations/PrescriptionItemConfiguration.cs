using Domain.Entities.Prescriptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class PrescriptionItemConfiguration : IEntityTypeConfiguration<PrescriptionItem>
{
    public void Configure(EntityTypeBuilder<PrescriptionItem> builder)
    {
        builder.ToTable("PrescriptionItems");
        builder.HasKey(i => i.Id);

        builder.Property(i => i.DosageInstructions).HasMaxLength(300);

        builder.ComplexProperty(i => i.PrescribedQuantity, cp =>
        {
            cp.Property(q => q.Value).HasColumnName("PrescribedQuantity").IsRequired();
        });

        builder.ComplexProperty(i => i.DispensedQuantity, cp =>
        {
            cp.Property(q => q.Value).HasColumnName("DispensedQuantity").IsRequired();
        });

        builder.HasOne(i => i.MedicineVariant)
            .WithMany()
            .HasForeignKey(i => i.MedicineVariantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(i => i.MedicineVariantId);
    }
}