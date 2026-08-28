using Domain.Entities.Staff;
using Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class PharmacistConfiguration : IEntityTypeConfiguration<Pharmacist>
{
    public void Configure(EntityTypeBuilder<Pharmacist> builder)
    {
        builder.ToTable("Pharmacists");
        builder.HasKey(p => p.Id);

        builder.ComplexProperty(p => p.LicenseNumber, cp =>
        {
            cp.Property(l => l.Value).HasColumnName("LicenseNumber").HasMaxLength(20).IsRequired();
        });

        builder.HasIndex(p => p.UserId).IsUnique();

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
