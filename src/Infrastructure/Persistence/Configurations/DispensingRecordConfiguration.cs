using Domain.Entities.Dispensing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class DispensingRecordConfiguration : IEntityTypeConfiguration<DispensingRecord>
{
    public void Configure(EntityTypeBuilder<DispensingRecord> builder)
    {
        builder.ToTable("DispensingRecords");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Notes).HasMaxLength(500);

        builder.HasOne(r => r.Prescription)
            .WithMany()
            .HasForeignKey(r => r.PrescriptionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Pharmacist)
            .WithMany()
            .HasForeignKey(r => r.PharmacistId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(r => r.Items)
            .WithOne()
            .HasForeignKey(i => i.DispensingRecordId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(r => r.DispensedAt);
        // Index pharmacist for fast lookups by pharmacist
        builder.HasIndex(r => r.PharmacistId);
    }
}
