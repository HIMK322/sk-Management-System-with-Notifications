using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskManagementSystem.Core.Entities;

namespace TaskManagementSystem.Infrastructure.Data.Configurations
{
    public class TaskConfiguration : IEntityTypeConfiguration<TaskItem>
    {
        public void Configure(EntityTypeBuilder<TaskItem> builder)
        {
            builder.HasKey(t => t.Id);

            builder.Property(t => t.Title)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(t => t.Description)
                .HasMaxLength(1000);

            builder.Property(t => t.Status)
                .IsRequired()
                .HasConversion<string>();

            builder.Property(t => t.DueDate)
                .IsRequired();

            // Indexes for performance optimization
            builder.HasIndex(t => t.Status);
            builder.HasIndex(t => t.AssignedToId);
            builder.HasIndex(t => t.CreatedById);
        }
    }
}