using Domain.Entities.Files;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class FileAttachmentConfiguration : IEntityTypeConfiguration<FileAttachment>
{
    public void Configure(EntityTypeBuilder<FileAttachment> builder)
    {
        builder.ToTable("FileAttachments");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.EntityType).IsRequired().HasConversion<int>();
        builder.Property(x => x.EntityId).IsRequired();
        builder.Property(x => x.FileName).IsRequired().HasMaxLength(260);
        builder.Property(x => x.ContentType).IsRequired().HasMaxLength(100);
        builder.Property(x => x.SizeBytes).IsRequired();
        builder.Property(x => x.BlobPath).IsRequired().HasMaxLength(500);
        builder.HasIndex(x => new { x.EntityType, x.EntityId });
    }
}
