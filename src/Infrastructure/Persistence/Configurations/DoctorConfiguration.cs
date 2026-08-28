using Domain.Entities.Staff;
using Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class DoctorConfiguration : IEntityTypeConfiguration<Doctor>
{
    public void Configure(EntityTypeBuilder<Doctor> builder)
    {
        builder.ToTable("Doctors");
        builder.HasKey(d => d.Id);

        builder.Property(d => d.Specialization).HasMaxLength(150);
        builder.Property(d => d.PhoneNumber).HasMaxLength(30);

        builder.ComplexProperty(d => d.LicenseNumber, cp =>
        {
            cp.Property(l => l.Value).HasColumnName("LicenseNumber").HasMaxLength(20).IsRequired();
        });

        builder.HasIndex(d => d.UserId).IsUnique();

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(d => d.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(d => d.Prescriptions)
            .WithOne(p => p.Doctor)
            .HasForeignKey(p => p.DoctorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
