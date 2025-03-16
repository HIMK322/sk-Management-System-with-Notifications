using System;
using TaskManagementSystem.Core.Interfaces;

namespace TaskManagementSystem.Core.Entities
{
    public enum TaskStatus
    {
        Pending,
        InProgress,
        Completed
    }

    public class TaskItem : ISoftDelete
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public TaskStatus Status { get; set; }
        public DateTime DueDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        
        // Foreign keys
        public Guid CreatedById { get; set; }
        public Guid? AssignedToId { get; set; }
        
        // Navigation properties
        public User CreatedBy { get; set; }
        public User AssignedTo { get; set; }
        
        // Soft delete properties
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
    }
}