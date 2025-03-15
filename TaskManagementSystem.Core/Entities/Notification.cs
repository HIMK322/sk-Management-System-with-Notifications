using System;

namespace TaskManagementSystem.Core.Entities
{
    public enum NotificationType
    {
        TaskAssigned,
        TaskUpdated,
        TaskCompleted
    }

    public class Notification
    {
        public Guid Id { get; set; }
        public string Message { get; set; }
        public NotificationType Type { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
        
        // Foreign keys
        public Guid UserId { get; set; }
        public Guid TaskId { get; set; }
        
        // Navigation properties
        public User User { get; set; }
        public TaskItem Task { get; set; }
    }
}