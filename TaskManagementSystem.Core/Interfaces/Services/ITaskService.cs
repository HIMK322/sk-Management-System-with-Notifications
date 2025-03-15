using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TaskManagementSystem.Core.DTOs.Task;

namespace TaskManagementSystem.Core.Interfaces.Services
{
    public interface ITaskService
    {
        Task<IEnumerable<TaskDto>> GetAllTasksAsync();
        Task<TaskDto> GetTaskByIdAsync(Guid id);
        Task<IEnumerable<TaskDto>> GetPendingTasksAsync(Guid userId);
        Task<IEnumerable<TaskDto>> GetTasksByUserIdAsync(Guid userId);
        Task<TaskDto> CreateTaskAsync(CreateTaskDto createTaskDto, Guid currentUserId);
        Task<TaskDto> UpdateTaskAsync(Guid id, UpdateTaskDto updateTaskDto);
        Task<TaskDto> AssignTaskAsync(Guid id, AssignTaskDto assignTaskDto);
        Task<TaskDto> CompleteTaskAsync(Guid id);
        Task DeleteTaskAsync(Guid id);
    }
}