using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TaskManagementSystem.Core.Entities;

namespace TaskManagementSystem.Core.Interfaces.Repositories
{
    public interface ITaskRepository
    {
        Task<TaskItem> GetByIdAsync(Guid id);
        Task<IEnumerable<TaskItem>> GetAllAsync();
        Task<IEnumerable<TaskItem>> GetPendingTasksAsync(Guid userId);
        Task<IEnumerable<TaskItem>> GetTasksByUserIdAsync(Guid userId);
        Task<TaskItem> CreateAsync(TaskItem task);
        Task UpdateAsync(TaskItem task);
        Task DeleteAsync(Guid id);
    }
}
