using System;
using TaskStatus = TaskManagementSystem.Core.Entities.TaskStatus;

namespace TaskManagementSystem.Core.DTOs.Task
{
    public class TaskDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public TaskStatus Status { get; set; }
        public DateTime DueDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public Guid CreatedById { get; set; }
        public string CreatedByUsername { get; set; }
        public Guid? AssignedToId { get; set; }
        public string AssignedToUsername { get; set; }
    }
}