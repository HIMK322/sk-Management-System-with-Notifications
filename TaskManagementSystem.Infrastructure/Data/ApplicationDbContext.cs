using Microsoft.EntityFrameworkCore;
using TaskManagementSystem.Core.Entities;

namespace TaskManagementSystem.Infrastructure.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<TaskItem> Tasks { get; set; }
        public DbSet<Notification> Notifications { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure entities
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

            // Create indexes for performance optimization
            modelBuilder.Entity<TaskItem>()
                .HasIndex(t => t.Status)
                .HasName("IX_Tasks_Status");

            modelBuilder.Entity<TaskItem>()
                .HasIndex(t => t.AssignedToId)
                .HasName("IX_Tasks_AssignedToId");
        }
    }
}