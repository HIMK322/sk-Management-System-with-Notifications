using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using TaskManagementSystem.Core.DTOs.Task;
using TaskManagementSystem.Core.Entities;
using TaskManagementSystem.Core.Exceptions;
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
        private readonly Mock<ILogger<TaskService>> _loggerMock;
        
        public TaskServiceTests()
        {
            _notificationServiceMock = new Mock<INotificationService>();
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
                _loggerMock.Object);
            
            var createTaskDto = new CreateTaskDto
            {
                Title = "Test Task",
                Description = "Test Description",
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
            
            // Create test tasks with different statuses
            var tasks = new[]
            {
                new TaskItem
                {
                    Id = Guid.NewGuid(),
                    Title = "Pending Task",
                    Status = TaskItemStatus.Pending,
                    CreatedById = userId,
                    AssignedToId = userId,
                    CreatedAt = DateTime.UtcNow,
                    DueDate = DateTime.UtcNow.AddDays(1)
                },
                new TaskItem
                {
                    Id = Guid.NewGuid(),
                    Title = "In Progress Task",
                    Status = TaskItemStatus.InProgress,
                    CreatedById = userId,
                    AssignedToId = userId,
                    CreatedAt = DateTime.UtcNow,
                    DueDate = DateTime.UtcNow.AddDays(1)
                },
                new TaskItem
                {
                    Id = Guid.NewGuid(),
                    Title = "Completed Task",
                    Status = TaskItemStatus.Completed,
                    CreatedById = userId,
                    AssignedToId = userId,
                    CreatedAt = DateTime.UtcNow,
                    DueDate = DateTime.UtcNow.AddDays(1)
                }
            };
            
            await context.Tasks.AddRangeAsync(tasks);
            await context.SaveChangesAsync();
            
            var taskService = new TaskService(
                taskRepository, 
                userRepository, 
                _notificationServiceMock.Object,
                _cacheServiceMock.Object, 
                _loggerMock.Object
            );
            
            // Act
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
                Description = "Task to be assigned",
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
            // Arrange
            var context = DbContextFactory.Create();
            var taskRepository = new TaskRepository(context);
            var userRepository = new UserRepository(context);
            
            var userId = Guid.NewGuid();
            
            // Create a task
            var task = new TaskItem
            {
                Id = Guid.NewGuid(),
                Title = "Task to Complete",
                Status = TaskItemStatus.InProgress,
                CreatedById = userId,
                CreatedAt = DateTime.UtcNow,
                DueDate = DateTime.UtcNow.AddDays(1)
            };
            
            await context.Tasks.AddAsync(task);
            await context.SaveChangesAsync();
            
            var taskService = new TaskService(
                taskRepository, 
                userRepository, 
                _notificationServiceMock.Object, 
                _loggerMock.Object);
            
            // Act
            var result = await taskService.CompleteTaskAsync(task.Id);
            
            // Assert
            result.Should().NotBeNull();
            result.Status.Should().Be(TaskItemStatus.Completed);
        }
    }
}