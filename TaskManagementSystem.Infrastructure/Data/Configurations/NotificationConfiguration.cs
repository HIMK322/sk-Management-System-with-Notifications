using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskManagementSystem.Core.Entities;

namespace TaskManagementSystem.Infrastructure.Data.Configurations
{
    public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
    {
        public void Configure(EntityTypeBuilder<Notification> builder)
        {
            builder.HasKey(n => n.Id);

            builder.Property(n => n.Message)
                .IsRequired()
                .HasMaxLength(500);

            builder.Property(n => n.Type)
                .IsRequired()
                .HasConversion<string>();

            builder.Property(n => n.IsRead)
                .IsRequired()
                .HasDefaultValue(false);

            // Indexes for performance optimization
            builder.HasIndex(n => n.UserId);
            builder.HasIndex(n => n.IsRead);
            builder.HasIndex(n => n.CreatedAt);
        }
    }
}