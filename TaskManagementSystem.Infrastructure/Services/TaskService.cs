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
        private readonly ILogger<TaskService> _logger;

        public TaskService(
            ITaskRepository taskRepository,
            IUserRepository userRepository,
            INotificationService notificationService,
            ILogger<TaskService> logger)
        {
            _taskRepository = taskRepository;
            _userRepository = userRepository;
            _notificationService = notificationService;
            _logger = logger;
        }

        public async Task<IEnumerable<TaskDto>> GetAllTasksAsync()
        {
            var tasks = await _taskRepository.GetAllAsync();
            return tasks.Select(MapToTaskDto);
        }

        public async Task<TaskDto> GetTaskByIdAsync(Guid id)
        {
            var task = await _taskRepository.GetByIdAsync(id);
            if (task == null)
            {
                throw new NotFoundException("Task not found");
            }
            return MapToTaskDto(task);
        }

        public async Task<IEnumerable<TaskDto>> GetPendingTasksAsync(Guid userId)
        {
            var tasks = await _taskRepository.GetPendingTasksAsync(userId);
            return tasks.Select(MapToTaskDto);
        }

        public async Task<IEnumerable<TaskDto>> GetTasksByUserIdAsync(Guid userId)
        {
            var tasks = await _taskRepository.GetTasksByUserIdAsync(userId);
            return tasks.Select(MapToTaskDto);
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
            task.Status = updateTaskDto.Status;
            task.DueDate = updateTaskDto.DueDate;

            await _taskRepository.UpdateAsync(task);
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
            }

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
    }
}