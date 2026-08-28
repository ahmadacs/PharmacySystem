using Domain.Entities.Dispensing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class DispensingRecordItemConfiguration : IEntityTypeConfiguration<DispensingRecordItem>
{
    public void Configure(EntityTypeBuilder<DispensingRecordItem> builder)
    {
        builder.ToTable("DispensingRecordItems");
        builder.HasKey(i => i.Id);

        builder.ComplexProperty(i => i.Quantity, cp =>
        {
            cp.Property(q => q.Value).HasColumnName("Quantity").IsRequired();
        });

        builder.HasOne(i => i.MedicineBatch)
            .WithMany()
            .HasForeignKey(i => i.MedicineBatchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(i => i.MedicineBatchId);

        builder.Property(i => i.PrescriptionItemId).IsRequired();
    }
}
