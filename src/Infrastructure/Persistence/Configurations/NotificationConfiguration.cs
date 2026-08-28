using Domain.Entities.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("Notifications");
        builder.HasKey(n => n.Id);
        builder.Property(n => n.Title).HasMaxLength(200).IsRequired();
        builder.Property(n => n.Message).HasMaxLength(500).IsRequired();
        builder.Property(n => n.Data).HasMaxLength(500);
        builder.Property(n => n.LocalizationKey).HasMaxLength(100);
        builder.Property(n => n.LocalizationParamsJson).HasMaxLength(1000);
        builder.Property(n => n.Type).HasConversion<string>().HasMaxLength(30);
        builder.HasIndex(n => new { n.UserId, n.IsRead });
    }
}