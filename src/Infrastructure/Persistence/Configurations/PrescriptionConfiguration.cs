using Domain.Entities.Prescriptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class PrescriptionConfiguration : IEntityTypeConfiguration<Prescription>
{
    public void Configure(EntityTypeBuilder<Prescription> builder)
    {
        builder.ToTable("Prescriptions");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Diagnosis).HasMaxLength(500);
        builder.Property(p => p.Status).HasConversion<string>().HasMaxLength(30);

        builder.Property(p => p.RowVersion).IsRowVersion();

        builder.HasOne(p => p.Patient)
            .WithMany(pa => pa.Prescriptions)
            .HasForeignKey(p => p.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(p => p.Items)
            .WithOne()
            .HasForeignKey(i => i.PrescriptionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(p => p.DoctorId);
        builder.HasIndex(p => p.PatientId);
        builder.HasIndex(p => p.Status);
    }
}
