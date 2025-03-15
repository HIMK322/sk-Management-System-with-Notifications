using System;
using TaskManagementSystem.Core.Entities;

namespace TaskManagementSystem.Core.DTOs.Notification
{
    public class NotificationDto
    {
        public Guid Id { get; set; }
        public string Message { get; set; }
        public NotificationType Type { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
        public Guid TaskId { get; set; }
        public string TaskTitle { get; set; }
    }
}