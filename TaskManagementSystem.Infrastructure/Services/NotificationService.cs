using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hangfire;
using Microsoft.Extensions.Logging;
using TaskManagementSystem.Core.DTOs.Notification;
using TaskManagementSystem.Core.Entities;
using TaskManagementSystem.Core.Interfaces.Repositories;
using TaskManagementSystem.Core.Interfaces.Services;

namespace TaskManagementSystem.Infrastructure.Services
{
    public class NotificationService : INotificationService
    {
        private readonly INotificationRepository _notificationRepository;
        private readonly ITaskRepository _taskRepository;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(
            INotificationRepository notificationRepository,
            ITaskRepository taskRepository,
            ILogger<NotificationService> logger)
        {
            _notificationRepository = notificationRepository;
            _taskRepository = taskRepository;
            _logger = logger;
        }

        public async Task<IEnumerable<NotificationDto>> GetNotificationsAsync(Guid userId)
        {
            var notifications = await _notificationRepository.GetByUserIdAsync(userId);
            return notifications.Select(MapToNotificationDto);
        }

        public async Task<IEnumerable<NotificationDto>> GetUnreadNotificationsAsync(Guid userId)
        {
            var notifications = await _notificationRepository.GetUnreadByUserIdAsync(userId);
            return notifications.Select(MapToNotificationDto);
        }

        public async Task MarkAsReadAsync(Guid id)
        {
            await _notificationRepository.MarkAsReadAsync(id);
        }

        public async Task CreateTaskAssignmentNotificationAsync(Guid taskId, Guid userId)
        {
            // Schedule notification creation with background job to be processed asynchronously
            BackgroundJob.Enqueue(() => CreateTaskAssignmentNotificationJob(taskId, userId));
        }

        [AutomaticRetry(Attempts = 3)]
        public async Task CreateTaskAssignmentNotificationJob(Guid taskId, Guid userId)
        {
            try
            {
                var task = await _taskRepository.GetByIdAsync(taskId);
                if (task == null)
                {
                    _logger.LogWarning($"Cannot create notification: Task {taskId} not found");
                    return;
                }

                var notification = new Notification
                {
                    Id = Guid.NewGuid(),
                    Message = $"You have been assigned to task: {task.Title}",
                    Type = NotificationType.TaskAssigned,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow,
                    UserId = userId,
                    TaskId = taskId,
                    Task = task
                };

                await _notificationRepository.CreateAsync(notification);
                _logger.LogInformation($"Notification created for user {userId} about task {taskId}");

                // Here you could add additional notification methods like email, SMS, etc.
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating notification for task {taskId} and user {userId}");
                throw; // Rethrow to trigger retry mechanism
            }
        }

        private NotificationDto MapToNotificationDto(Notification notification)
        {
            return new NotificationDto
            {
                Id = notification.Id,
                Message = notification.Message,
                Type = notification.Type,
                IsRead = notification.IsRead,
                CreatedAt = notification.CreatedAt,
                TaskId = notification.TaskId,
                TaskTitle = notification.Task?.Title
            };
        }
    }
}