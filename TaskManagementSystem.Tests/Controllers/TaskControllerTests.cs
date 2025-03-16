using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using TaskManagementSystem.API.Controllers;
using TaskManagementSystem.Core.DTOs.Task;
using TaskManagementSystem.Core.Entities;
using TaskManagementSystem.Core.Interfaces.Services;
using Xunit;
using FluentAssertions;
using TaskItemStatus = TaskManagementSystem.Core.Entities.TaskStatus;

namespace TaskManagementSystem.Tests.Controllers
{
    public class TaskControllerTests
    {
        private readonly Mock<ITaskService> _taskServiceMock;
        private readonly Mock<IAuthService> _authServiceMock;
        private readonly Guid _userId;
        
        public TaskControllerTests()
        {
            _taskServiceMock = new Mock<ITaskService>();
            _authServiceMock = new Mock<IAuthService>();
            _userId = Guid.NewGuid();
        }
        
        private TaskController CreateController()
        {
            var controller = new TaskController(_taskServiceMock.Object, _authServiceMock.Object);
            
            // Setup controller context with authenticated user
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, _userId.ToString())
            };
            
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var claimsPrincipal = new ClaimsPrincipal(identity);
            
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = claimsPrincipal
                }
            };
            
            return controller;
        }
        
        [Fact]
        public async Task GetPendingTasks_ShouldReturnPendingTasks()
        {
            // Arrange
            var controller = CreateController();
            
            var pendingTasks = new List<TaskDto>
            {
                new TaskDto
                {
                    Id = Guid.NewGuid(),
                    Title = "Pending Task 1",
                    Status = TaskItemStatus.Pending,
                    AssignedToId = _userId
                },
                new TaskDto
                {
                    Id = Guid.NewGuid(),
                    Title = "In Progress Task",
                    Status = TaskItemStatus.InProgress,
                    AssignedToId = _userId
                }
            };
            
            _taskServiceMock.Setup(s => s.GetPendingTasksAsync(_userId))
                .ReturnsAsync(pendingTasks);
            
            // Act
            var result = await controller.GetPendingTasks();
            
            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var returnedTasks = okResult.Value.Should().BeAssignableTo<IEnumerable<TaskDto>>().Subject;
            returnedTasks.Should().HaveCount(2);
        }
        
        [Fact]
        public async Task CreateTask_ShouldCreateAndReturnTask()
        {
            // Arrange
            var controller = CreateController();
            
            var createTaskDto = new CreateTaskDto
            {
                Title = "New Test Task",
                Description = "Test Description",
                DueDate = DateTime.UtcNow.AddDays(1)
            };
            
            var createdTask = new TaskDto
            {
                Id = Guid.NewGuid(),
                Title = createTaskDto.Title,
                Description = createTaskDto.Description,
                Status = TaskItemStatus.Pending,
                CreatedById = _userId,
                CreatedAt = DateTime.UtcNow,
                DueDate = createTaskDto.DueDate
            };
            
            _taskServiceMock.Setup(s => s.CreateTaskAsync(createTaskDto, _userId))
                .ReturnsAsync(createdTask);
            
            // Act
            var result = await controller.CreateTask(createTaskDto);
            
            // Assert
            var createdAtResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
            createdAtResult.Value.Should().BeEquivalentTo(createdTask);
            createdAtResult.ActionName.Should().Be(nameof(controller.GetTaskById));
            createdAtResult.RouteValues["id"].Should().Be(createdTask.Id);
        }
        
        [Fact]
        public async Task CompleteTask_ShouldCompleteAndReturnTask()
        {
            // Arrange
            var controller = CreateController();
            var taskId = Guid.NewGuid();
            
            var completedTask = new TaskDto
            {
                Id = taskId,
                Title = "Completed Task",
                Status = TaskItemStatus.Completed,
                AssignedToId = _userId,
                UpdatedAt = DateTime.UtcNow
            };
            
            _taskServiceMock.Setup(s => s.CompleteTaskAsync(taskId))
                .ReturnsAsync(completedTask);
            
            // Act
            var result = await controller.CompleteTask(taskId);
            
            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var returnedTask = okResult.Value.Should().BeAssignableTo<TaskDto>().Subject;
            returnedTask.Status.Should().Be(TaskItemStatus.Completed);
        }
    }
}