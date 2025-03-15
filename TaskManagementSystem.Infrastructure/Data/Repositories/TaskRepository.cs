using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TaskManagementSystem.Core.Entities;
using TaskManagementSystem.Core.Interfaces.Repositories;
using TaskStatus = TaskManagementSystem.Core.Entities.TaskStatus;

namespace TaskManagementSystem.Infrastructure.Data.Repositories
{
    public class TaskRepository : ITaskRepository
    {
        private readonly ApplicationDbContext _context;

        public TaskRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<TaskItem> GetByIdAsync(Guid id)
        {
            return await _context.Tasks
                .Include(t => t.CreatedBy)
                .Include(t => t.AssignedTo)
                .FirstOrDefaultAsync(t => t.Id == id);
        }

        public async Task<IEnumerable<TaskItem>> GetAllAsync()
        {
            return await _context.Tasks
                .Include(t => t.CreatedBy)
                .Include(t => t.AssignedTo)
                .ToListAsync();
        }

        public async Task<IEnumerable<TaskItem>> GetPendingTasksAsync(Guid userId)
        {
            return await _context.Tasks
                .Include(t => t.CreatedBy)
                .Include(t => t.AssignedTo)
                .Where(t => t.AssignedToId == userId && 
                           (t.Status == TaskStatus.Pending || t.Status == TaskStatus.InProgress))
                .ToListAsync();
        }

        public async Task<IEnumerable<TaskItem>> GetTasksByUserIdAsync(Guid userId)
        {
            return await _context.Tasks
                .Include(t => t.CreatedBy)
                .Include(t => t.AssignedTo)
                .Where(t => t.AssignedToId == userId || t.CreatedById == userId)
                .ToListAsync();
        }

        public async Task<TaskItem> CreateAsync(TaskItem task)
        {
            await _context.Tasks.AddAsync(task);
            await _context.SaveChangesAsync();
            return task;
        }

        public async Task UpdateAsync(TaskItem task)
        {
            task.UpdatedAt = DateTime.UtcNow;
            _context.Entry(task).State = EntityState.Modified;
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid id)
        {
            var task = await _context.Tasks.FindAsync(id);
            if (task != null)
            {
                _context.Tasks.Remove(task);
                await _context.SaveChangesAsync();
            }
        }
    }
}
