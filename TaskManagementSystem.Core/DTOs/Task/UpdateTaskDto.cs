    public class UpdateTaskDto
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public TaskManagementSystem.Core.Entities.TaskStatus Status { get; set; }
        public DateTime DueDate { get; set; }
    }