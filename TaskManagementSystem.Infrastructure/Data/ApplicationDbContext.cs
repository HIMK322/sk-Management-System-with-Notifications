using Microsoft.EntityFrameworkCore;
using System.Linq;
using TaskManagementSystem.Core.Entities;
using TaskManagementSystem.Core.Interfaces;

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
            
            // Add global query filter for soft delete
            modelBuilder.Entity<TaskItem>().HasQueryFilter(t => !t.IsDeleted);
        }
        
        public override int SaveChanges()
        {
            UpdateSoftDeleteStatuses();
            return base.SaveChanges();
        }
        
        public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            UpdateSoftDeleteStatuses();
            return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }
        
        private void UpdateSoftDeleteStatuses()
        {
            foreach (var entry in ChangeTracker.Entries())
            {
                if (entry.Entity is ISoftDelete softDeleteEntity && entry.State == EntityState.Deleted)
                {
                    // Change state to modified and set IsDeleted flag
                    entry.State = EntityState.Modified;
                    softDeleteEntity.IsDeleted = true;
                    softDeleteEntity.DeletedAt = DateTime.UtcNow;
                }
            }
        }
    }
}