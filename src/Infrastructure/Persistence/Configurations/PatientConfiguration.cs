using Domain.Entities.Patients;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class PatientConfiguration : IEntityTypeConfiguration<Patient>
{
    public void Configure(EntityTypeBuilder<Patient> builder)
    {
        builder.ToTable("Patients");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.FirstName).IsRequired().HasMaxLength(100);
        builder.Property(p => p.LastName).IsRequired().HasMaxLength(100);
        builder.Property(p => p.DateOfBirth).IsRequired();
        builder.Property(p => p.PhoneNumber).IsRequired().HasMaxLength(30);

        builder.HasIndex(p => p.PhoneNumber).IsUnique();
        builder.HasIndex(p => p.FirstName);
        builder.HasIndex(p => p.LastName);
    }
}