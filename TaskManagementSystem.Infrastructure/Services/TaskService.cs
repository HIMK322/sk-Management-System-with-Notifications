using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TaskManagementSystem.Core.DTOs.Task;
using TaskManagementSystem.Core.Entities;
using TaskManagementSystem.Core.Exceptions;
using TaskManagementSystem.Core.Interfaces.Repositories;
using TaskManagementSystem.Core.Interfaces.Services;
using TaskStatus = TaskManagementSystem.Core.Entities.TaskStatus;

namespace TaskManagementSystem.Infrastructure.Services
{
    public class TaskService : ITaskService
    {
        private readonly ITaskRepository _taskRepository;
        private readonly IUserRepository _userRepository;
        private readonly INotificationService _notificationService;
        private readonly ICacheService _cacheService;
        private readonly ILogger<TaskService> _logger;

        private readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(10);

        public TaskService(
            ITaskRepository taskRepository,
            IUserRepository userRepository,
            INotificationService notificationService,
            ICacheService cacheService,
            ILogger<TaskService> logger)
        {
            _taskRepository = taskRepository;
            _userRepository = userRepository;
            _notificationService = notificationService;
            _cacheService = cacheService;
            _logger = logger;
        }

        public async Task<IEnumerable<TaskDto>> GetAllTasksAsync()
        {
            return await _cacheService.GetOrCreateAsync(
                "tasks_all",
                async () => {
                    var tasks = await _taskRepository.GetAllAsync();
                    return tasks.Select(MapToTaskDto).ToList();
                },
                _cacheDuration);
        }

        public async Task<TaskDto> GetTaskByIdAsync(Guid id)
        {
            return await _cacheService.GetOrCreateAsync(
                $"task_{id}",
                async () => {
                    var task = await _taskRepository.GetByIdAsync(id);
                    if (task == null)
                    {
                        throw new NotFoundException("Task not found");
                    }
                    return MapToTaskDto(task);
                },
                _cacheDuration);
        }

        public async Task<IEnumerable<TaskDto>> GetPendingTasksAsync(Guid userId)
        {
            return await _cacheService.GetOrCreateAsync(
                $"pending_tasks_{userId}",
                async () => {
                    var tasks = await _taskRepository.GetPendingTasksAsync(userId);
                    return tasks.Select(MapToTaskDto).ToList();
                },
                _cacheDuration);
        }

        public async Task<IEnumerable<TaskDto>> GetTasksByUserIdAsync(Guid userId)
        {
            return await _cacheService.GetOrCreateAsync(
                $"user_tasks_{userId}",
                async () => {
                    var tasks = await _taskRepository.GetTasksByUserIdAsync(userId);
                    return tasks.Select(MapToTaskDto).ToList();
                },
                _cacheDuration);
        }

        public async Task<TaskDto> CreateTaskAsync(CreateTaskDto createTaskDto, Guid currentUserId)
        {
            var createdBy = await _userRepository.GetByIdAsync(currentUserId);
            if (createdBy == null)
            {
                throw new NotFoundException("User not found");
            }

            User assignedTo = null;
            if (createTaskDto.AssignedToId.HasValue)
            {
                assignedTo = await _userRepository.GetByIdAsync(createTaskDto.AssignedToId.Value);
                if (assignedTo == null)
                {
                    throw new NotFoundException("Assigned user not found");
                }
            }

            var task = new TaskItem
            {
                Id = Guid.NewGuid(),
                Title = createTaskDto.Title,
                Description = createTaskDto.Description,
                Status = TaskStatus.Pending,
                DueDate = createTaskDto.DueDate,
                CreatedAt = DateTime.UtcNow,
                CreatedById = currentUserId,
                CreatedBy = createdBy,
                AssignedToId = createTaskDto.AssignedToId,
                AssignedTo = assignedTo
            };

            var createdTask = await _taskRepository.CreateAsync(task);

            // Create notification if task is assigned to someone
            if (createdTask.AssignedToId.HasValue && createdTask.AssignedToId != currentUserId)
            {
                await _notificationService.CreateTaskAssignmentNotificationAsync(
                    createdTask.Id, createdTask.AssignedToId.Value);
            }

            await InvalidateTaskCaches(createdTask);

            return MapToTaskDto(createdTask);
        }

        public async Task<TaskDto> UpdateTaskAsync(Guid id, UpdateTaskDto updateTaskDto)
        {
            var task = await _taskRepository.GetByIdAsync(id);
            if (task == null)
            {
                throw new NotFoundException("Task not found");
            }

            task.Title = updateTaskDto.Title;
            task.Description = updateTaskDto.Description;
            task.Status = TaskManagementSystem.Core.Entities.TaskStatus.Completed;
            task.DueDate = updateTaskDto.DueDate;
            task.UpdatedAt = DateTime.UtcNow;

            await _taskRepository.UpdateAsync(task);
            await InvalidateTaskCaches(task);

            return MapToTaskDto(task);
        }

        public async Task<TaskDto> AssignTaskAsync(Guid id, AssignTaskDto assignTaskDto)
        {
            var task = await _taskRepository.GetByIdAsync(id);
            if (task == null)
            {
                throw new NotFoundException("Task not found");
            }

            var assignedTo = await _userRepository.GetByIdAsync(assignTaskDto.AssignedToId);
            if (assignedTo == null)
            {
                throw new NotFoundException("Assigned user not found");
            }

            // Store previous assignee to check if it changed
            var previousAssigneeId = task.AssignedToId;

            task.AssignedToId = assignTaskDto.AssignedToId;
            task.AssignedTo = assignedTo;
            task.UpdatedAt = DateTime.UtcNow;

            await _taskRepository.UpdateAsync(task);

            // Create notification if the task is newly assigned or reassigned
            if (task.AssignedToId != previousAssigneeId)
            {
                await _notificationService.CreateTaskAssignmentNotificationAsync(
                    task.Id, task.AssignedToId.Value);
                
                if (previousAssigneeId.HasValue)
                {
                    await InvalidateUserTaskCaches(previousAssigneeId.Value);
                }
            }

            await InvalidateTaskCaches(task);

            return MapToTaskDto(task);
        }

        public async Task<TaskDto> CompleteTaskAsync(Guid id)
        {
            var task = await _taskRepository.GetByIdAsync(id);
            if (task == null)
            {
                throw new NotFoundException("Task not found");
            }

            task.Status = TaskStatus.Completed;
            task.UpdatedAt = DateTime.UtcNow;

            await _taskRepository.UpdateAsync(task);
            await InvalidateTaskCaches(task);

            return MapToTaskDto(task);
        }

        public async Task DeleteTaskAsync(Guid id)
        {
            var task = await _taskRepository.GetByIdAsync(id);
            if (task == null)
            {
                throw new NotFoundException("Task not found");
            }

            await _taskRepository.DeleteAsync(id);
            await InvalidateTaskCaches(task);
        }

        private TaskDto MapToTaskDto(TaskItem task)
        {
            return new TaskDto
            {
                Id = task.Id,
                Title = task.Title,
                Description = task.Description,
                Status = task.Status,
                DueDate = task.DueDate,
                CreatedAt = task.CreatedAt,
                UpdatedAt = task.UpdatedAt,
                CreatedById = task.CreatedById,
                CreatedByUsername = task.CreatedBy?.Username,
                AssignedToId = task.AssignedToId,
                AssignedToUsername = task.AssignedTo?.Username
            };
        }

        private async Task InvalidateTaskCaches(TaskItem task)
        {
            // Invalidate specific task cache
            await _cacheService.RemoveAsync($"task_{task.Id}");
            
            // Invalidate all tasks cache
            await _cacheService.RemoveAsync("tasks_all");
            
            // Invalidate user-specific caches
            if (task.CreatedById != Guid.Empty)
            {
                await InvalidateUserTaskCaches(task.CreatedById);
            }
            
            if (task.AssignedToId.HasValue)
            {
                await InvalidateUserTaskCaches(task.AssignedToId.Value);
            }
        }

        private async Task InvalidateUserTaskCaches(Guid userId)
        {
            await _cacheService.RemoveAsync($"user_tasks_{userId}");
            await _cacheService.RemoveAsync($"pending_tasks_{userId}");
        }
        
        public async Task<IEnumerable<TaskDto>> GetDeletedTasksAsync()
        {
            var tasks = await _taskRepository.GetDeletedTasksAsync();
            return tasks.Select(MapToTaskDto);
        }

        public async Task<TaskDto> RestoreTaskAsync(Guid id)
        {
            await _taskRepository.RestoreAsync(id);
            
            var task = await _taskRepository.GetByIdAsync(id);
            if (task == null)
            {
                throw new NotFoundException("Task could not be restored");
            }
            
            // Invalidate relevant caches
            await InvalidateTaskCaches(task);
            
            _logger.LogInformation($"Task {id} has been restored");
            
            return MapToTaskDto(task);
        }
    }
}
