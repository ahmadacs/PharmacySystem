using Domain.Entities.Medicines;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class GenericNameConfiguration : IEntityTypeConfiguration<GenericName>
{
    public void Configure(EntityTypeBuilder<GenericName> builder)
    {
        builder.ToTable("GenericNames");
        builder.HasKey(g => g.Id);

        builder.Property(g => g.Name).IsRequired().HasMaxLength(200);
        builder.Property(g => g.NameAr).HasMaxLength(200);
        builder.HasIndex(g => g.Name).IsUnique();
    }
}