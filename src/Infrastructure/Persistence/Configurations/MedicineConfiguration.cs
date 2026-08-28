using Domain.Entities.Medicines;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class MedicineConfiguration : IEntityTypeConfiguration<Medicine>
{
    public void Configure(EntityTypeBuilder<Medicine> builder)
    {
        builder.ToTable("Medicines");
        builder.HasKey(m => m.Id);

        builder.Property(m => m.Name).IsRequired().HasMaxLength(200);
        builder.Property(m => m.NameAr).HasMaxLength(200);

        builder.ComplexProperty(m => m.ReorderLevel, cp =>
        {
            cp.Property(q => q.Value).HasColumnName("ReorderLevel").IsRequired();
        });

        builder.Property(m => m.CategoryEnum)
            .IsRequired()
            .HasConversion<int>();

        builder.HasOne(m => m.GenericName)
            .WithMany(g => g.Medicines)
            .HasForeignKey(m => m.GenericNameId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(m => m.GenericNameId).IsRequired();

        builder.HasIndex(m => m.Name);
        builder.HasIndex(m => m.CategoryEnum);
        builder.HasIndex(m => m.GenericNameId);
        // Frequently queried flag - index to speed up active/inactive filters
        builder.HasIndex(m => m.IsActive);
    }
}