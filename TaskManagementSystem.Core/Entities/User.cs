using System;
using System.Collections.Generic;

namespace TaskManagementSystem.Core.Entities
{
    public class User
    {
        public Guid Id { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public ICollection<TaskItem> AssignedTasks { get; set; }
        public ICollection<TaskItem> CreatedTasks { get; set; }
        public ICollection<Notification> Notifications { get; set; }
    }
}