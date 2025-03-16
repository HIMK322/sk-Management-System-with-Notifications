using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using TaskManagementSystem.Core.Entities;
using TaskManagementSystem.Infrastructure.Data.Repositories;
using TaskManagementSystem.Infrastructure.Services;
using TaskManagementSystem.Tests.Helpers;
using Xunit;
using FluentAssertions;

namespace TaskManagementSystem.Tests.Services
{
    public class NotificationServiceTests
    {
        private readonly Mock<ILogger<NotificationService>> _loggerMock;
        
        public NotificationServiceTests()
        {
            _loggerMock = new Mock<ILogger<NotificationService>>();
        }
        
        [Fact]
        public async Task GetNotificationsAsync_ShouldReturnUserNotifications()
        {
            // Arrange
            var context = DbContextFactory.Create();
            var notificationRepository = new NotificationRepository(context);
            var taskRepository = new TaskRepository(context);
            
            var userId = Guid.NewGuid();
            var otherUserId = Guid.NewGuid();
            
            // Create test tasks
            var task = new TaskItem
            {
                Id = Guid.NewGuid(),
                Title = "Notification Test Task",
                CreatedById = otherUserId,
                CreatedAt = DateTime.UtcNow,
                DueDate = DateTime.UtcNow.AddDays(1)
            };
            
            await context.Tasks.AddAsync(task);
            
            // Create notifications for two different users
            var notifications = new[]
            {
                new Notification
                {
                    Id = Guid.NewGuid(),
                    Message = "Test notification 1",
                    Type = NotificationType.TaskAssigned,
                    IsRead = false,
                    UserId = userId,
                    TaskId = task.Id,
                    Task = task,
                    CreatedAt = DateTime.UtcNow
                },
                new Notification
                {
                    Id = Guid.NewGuid(),
                    Message = "Test notification 2",
                    Type = NotificationType.TaskAssigned,
                    IsRead = true,
                    UserId = userId,
                    TaskId = task.Id,
                    Task = task,
                    CreatedAt = DateTime.UtcNow
                },
                new Notification
                {
                    Id = Guid.NewGuid(),
                    Message = "Other user notification",
                    Type = NotificationType.TaskAssigned,
                    IsRead = false,
                    UserId = otherUserId,
                    TaskId = task.Id,
                    Task = task,
                    CreatedAt = DateTime.UtcNow
                }
            };
            
            await context.Notifications.AddRangeAsync(notifications);
            await context.SaveChangesAsync();
            
            var notificationService = new NotificationService(
                notificationRepository,
                taskRepository,
                _loggerMock.Object);
            
            // Act
            var result = await notificationService.GetNotificationsAsync(userId);
            
            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2); // Only notifications for userId
        }
        
        [Fact]
        public async Task GetUnreadNotificationsAsync_ShouldReturnOnlyUnreadNotifications()
        {
            // Arrange
            var context = DbContextFactory.Create();
            var notificationRepository = new NotificationRepository(context);
            var taskRepository = new TaskRepository(context);
            
            var userId = Guid.NewGuid();
            
            // Create test task
            var task = new TaskItem
            {
                Id = Guid.NewGuid(),
                Title = "Unread Notification Test Task",
                CreatedById = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow,
                DueDate = DateTime.UtcNow.AddDays(1)
            };
            
            await context.Tasks.AddAsync(task);
            
            // Create read and unread notifications
            var notifications = new[]
            {
                new Notification
                {
                    Id = Guid.NewGuid(),
                    Message = "Unread notification 1",
                    Type = NotificationType.TaskAssigned,
                    IsRead = false,
                    UserId = userId,
                    TaskId = task.Id,
                    Task = task,
                    CreatedAt = DateTime.UtcNow
                },
                new Notification
                {
                    Id = Guid.NewGuid(),
                    Message = "Read notification",
                    Type = NotificationType.TaskAssigned,
                    IsRead = true,
                    UserId = userId,
                    TaskId = task.Id,
                    Task = task,
                    CreatedAt = DateTime.UtcNow
                },
                new Notification
                {
                    Id = Guid.NewGuid(),
                    Message = "Unread notification 2",
                    Type = NotificationType.TaskAssigned,
                    IsRead = false,
                    UserId = userId,
                    TaskId = task.Id,
                    Task = task,
                    CreatedAt = DateTime.UtcNow
                }
            };
            
            await context.Notifications.AddRangeAsync(notifications);
            await context.SaveChangesAsync();
            
            var notificationService = new NotificationService(
                notificationRepository,
                taskRepository,
                _loggerMock.Object);
            
            // Act
            var result = await notificationService.GetUnreadNotificationsAsync(userId);
            
            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2); // Only unread notifications
            result.All(n => !n.IsRead).Should().BeTrue();
        }
        
        [Fact]
        public async Task MarkAsReadAsync_ShouldMarkNotificationAsRead()
        {
            // Arrange
            var context = DbContextFactory.Create();
            var notificationRepository = new NotificationRepository(context);
            var taskRepository = new TaskRepository(context);
            
            // Create unread notification
            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                Message = "Test notification to mark as read",
                Type = NotificationType.TaskAssigned,
                IsRead = false,
                UserId = Guid.NewGuid(),
                TaskId = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow
            };
            
            await context.Notifications.AddAsync(notification);
            await context.SaveChangesAsync();
            
            var notificationService = new NotificationService(
                notificationRepository,
                taskRepository,
                _loggerMock.Object);
            
            // Act
            await notificationService.MarkAsReadAsync(notification.Id);
            
            // Assert
            var updatedNotification = await context.Notifications.FindAsync(notification.Id);
            updatedNotification.IsRead.Should().BeTrue();
        }
    }
}