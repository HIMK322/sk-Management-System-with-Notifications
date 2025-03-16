using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using TaskManagementSystem.Core.DTOs.Task;
using TaskManagementSystem.Core.Entities;
using TaskManagementSystem.Core.Exceptions;
using TaskManagementSystem.Core.Interfaces.Repositories;
using TaskManagementSystem.Core.Interfaces.Services;
using TaskManagementSystem.Infrastructure.Data.Repositories;
using TaskManagementSystem.Infrastructure.Services;
using TaskManagementSystem.Tests.Helpers;
using Xunit;
using FluentAssertions;
using TaskItemStatus = TaskManagementSystem.Core.Entities.TaskStatus;

namespace TaskManagementSystem.Tests.Services
{
    public class TaskServiceTests
    {
        private readonly Mock<INotificationService> _notificationServiceMock;
        private readonly Mock<ICacheService> _cacheServiceMock;
        private readonly Mock<ILogger<TaskService>> _loggerMock;
        
        public TaskServiceTests()
        {
            _notificationServiceMock = new Mock<INotificationService>();
            _cacheServiceMock = new Mock<ICacheService>();
            _loggerMock = new Mock<ILogger<TaskService>>();
        }
        
        [Fact]
        public async Task CreateTaskAsync_ShouldCreateNewTask()
        {
            // Arrange
            var context = DbContextFactory.Create();
            var taskRepository = new TaskRepository(context);
            var userRepository = new UserRepository(context);
            
            // Create a test user
            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = "taskuser",
                Email = "task@example.com",
                PasswordHash = "hashedpassword",
                FirstName = "Task",
                LastName = "User",
                CreatedAt = DateTime.UtcNow
            };
            
            await context.Users.AddAsync(user);
            await context.SaveChangesAsync();
            
            var taskService = new TaskService(
                taskRepository, 
                userRepository, 
                _notificationServiceMock.Object,
                _cacheServiceMock.Object,
                _loggerMock.Object);
            
            var createTaskDto = new CreateTaskDto
            {
                Title = "Test Task",
                Description = "Test Description", // Make sure Description is included
                DueDate = DateTime.UtcNow.AddDays(7)
            };
            
            // Act
            var result = await taskService.CreateTaskAsync(createTaskDto, user.Id);
            
            // Assert
            result.Should().NotBeNull();
            result.Title.Should().Be(createTaskDto.Title);
            result.Description.Should().Be(createTaskDto.Description);
            result.Status.Should().Be(TaskItemStatus.Pending);
            result.CreatedById.Should().Be(user.Id);
        }

        [Fact]
        public async Task GetPendingTasksAsync_ShouldReturnOnlyPendingTasks()
        {
            // Arrange
            var context = DbContextFactory.Create();
            var taskRepository = new TaskRepository(context);
            var userRepository = new UserRepository(context);
            
            var userId = Guid.NewGuid();
            
            // Create a user
            var user = new User
            {
                Id = userId,
                Username = "testuser",
                Email = "test@example.com",
                PasswordHash = "hashedpassword",
                FirstName = "Test",
                LastName = "User",
                CreatedAt = DateTime.UtcNow
            };
            
            await context.Users.AddAsync(user);
            await context.SaveChangesAsync();
            
            // Create test tasks with different statuses
            var pendingTask = new TaskItem
            {
                Id = Guid.NewGuid(),
                Title = "Pending Task",
                Description = "This is a pending task",
                Status = TaskItemStatus.Pending,
                CreatedById = userId,
                AssignedToId = userId,
                CreatedAt = DateTime.UtcNow,
                DueDate = DateTime.UtcNow.AddDays(1)
            };
            
            var inProgressTask = new TaskItem
            {
                Id = Guid.NewGuid(),
                Title = "In Progress Task",
                Description = "This is an in-progress task",
                Status = TaskItemStatus.InProgress,
                CreatedById = userId,
                AssignedToId = userId,
                CreatedAt = DateTime.UtcNow,
                DueDate = DateTime.UtcNow.AddDays(1)
            };
            
            var completedTask = new TaskItem
            {
                Id = Guid.NewGuid(),
                Title = "Completed Task",
                Description = "This is a completed task",
                Status = TaskItemStatus.Completed,
                CreatedById = userId,
                AssignedToId = userId,
                CreatedAt = DateTime.UtcNow,
                DueDate = DateTime.UtcNow.AddDays(1)
            };
            
            await context.Tasks.AddRangeAsync(pendingTask, inProgressTask, completedTask);
            await context.SaveChangesAsync();
            
            var cacheService = new TestCacheService();
            
            var taskService = new TaskService(
                taskRepository,
                userRepository,
                _notificationServiceMock.Object,
                cacheService,
                _loggerMock.Object
            );
            
            // Act
            var tasks = await taskRepository.GetPendingTasksAsync(userId);
            Console.WriteLine($"Repository returned {tasks.Count()} tasks"); // Debug output
            
            var result = await taskService.GetPendingTasksAsync(userId);
            
            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2); // Only Pending and InProgress
            result.All(t => t.Status != TaskItemStatus.Completed).Should().BeTrue();
        }
        
        [Fact]
        public async Task AssignTaskAsync_ShouldAssignTaskToUser()
        {
            // Arrange
            var context = DbContextFactory.Create();
            var taskRepository = new TaskRepository(context);
            var userRepository = new UserRepository(context);
            
            // Create users
            var creator = new User
            {
                Id = Guid.NewGuid(),
                Username = "creator",
                Email = "creator@example.com",
                PasswordHash = "hashedpassword",
                FirstName = "Creator",
                LastName = "User",
                CreatedAt = DateTime.UtcNow
            };
            
            var assignee = new User
            {
                Id = Guid.NewGuid(),
                Username = "assignee",
                Email = "assignee@example.com",
                PasswordHash = "hashedpassword",
                FirstName = "Assignee",
                LastName = "User",
                CreatedAt = DateTime.UtcNow
            };
            
            await context.Users.AddRangeAsync(creator, assignee);
            
            // Create a task
            var task = new TaskItem
            {
                Id = Guid.NewGuid(),
                Title = "Test Assignment",
                Description = "Task to be assigned", // Added Description property
                Status = TaskItemStatus.Pending,
                CreatedById = creator.Id,
                CreatedBy = creator,
                CreatedAt = DateTime.UtcNow,
                DueDate = DateTime.UtcNow.AddDays(1)
            };
            
            await context.Tasks.AddAsync(task);
            await context.SaveChangesAsync();
            
            var taskService = new TaskService(
                taskRepository, 
                userRepository, 
                _notificationServiceMock.Object,
                _cacheServiceMock.Object,
                _loggerMock.Object);
            
            var assignTaskDto = new AssignTaskDto
            {
                AssignedToId = assignee.Id
            };
            
            // Act
            var result = await taskService.AssignTaskAsync(task.Id, assignTaskDto);
            
            // Assert
            result.Should().NotBeNull();
            result.AssignedToId.Should().Be(assignee.Id);
            result.AssignedToUsername.Should().Be(assignee.Username);
            
            // Verify notification service was called
            _notificationServiceMock.Verify(
                x => x.CreateTaskAssignmentNotificationAsync(task.Id, assignee.Id),
                Times.Once);
        }
        
        [Fact]
        public async Task CompleteTaskAsync_ShouldChangeTaskStatusToCompleted()
        {
            // Arrange - Use mocks instead of in-memory database
            var taskId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            
            // Setup task repository mock
            var taskRepositoryMock = new Mock<ITaskRepository>();
            var userRepositoryMock = new Mock<IUserRepository>();
            
            // Create a task
            var task = new TaskItem
            {
                Id = taskId,
                Title = "Task to Complete",
                Description = "This task will be completed",
                Status = TaskItemStatus.InProgress,
                CreatedById = userId,
                CreatedAt = DateTime.UtcNow,
                DueDate = DateTime.UtcNow.AddDays(1)
            };
            
            // Setup repository to return the task
            taskRepositoryMock
                .Setup(repo => repo.GetByIdAsync(taskId))
                .ReturnsAsync(task);
            
            // Setup cache service
            _cacheServiceMock
                .Setup(cache => cache.GetOrCreateAsync(
                    $"task_{taskId}",
                    It.IsAny<Func<Task<TaskDto>>>(),
                    It.IsAny<TimeSpan?>()))
                .Returns<string, Func<Task<TaskDto>>, TimeSpan?>((key, factory, expiry) => factory());
            
            var taskService = new TaskService(
                taskRepositoryMock.Object,
                userRepositoryMock.Object,
                _notificationServiceMock.Object,
                _cacheServiceMock.Object,
                _loggerMock.Object);
            
            // Act
            var result = await taskService.CompleteTaskAsync(taskId);
            
            // Assert
            result.Should().NotBeNull();
            result.Status.Should().Be(TaskItemStatus.Completed);
            
            // Verify UpdateAsync was called
            taskRepositoryMock.Verify(
                repo => repo.UpdateAsync(It.Is<TaskItem>(t => 
                    t.Id == taskId && 
                    t.Status == TaskItemStatus.Completed)), 
                Times.Once);
        }
    
        private class TestCacheService : ICacheService
        {
            public async Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? absoluteExpiration = null)
            {
                return await factory();
            }
            
            public Task<T> GetAsync<T>(string key)
            {
                return Task.FromResult<T>(default);
            }
            
            public Task SetAsync<T>(string key, T value, TimeSpan? absoluteExpiration = null)
            {
                return Task.CompletedTask;
            }
            
            public Task RemoveAsync(string key)
            {
                return Task.CompletedTask;
            }
        }
    }
}