using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManagementSystem.Core.DTOs.Task;
using TaskManagementSystem.Core.Interfaces.Services;

namespace TaskManagementSystem.API.Controllers
{
    [ApiController]
    [Route("api/tasks")]
    [Authorize]
    public class TaskController : ControllerBase
    {
        private readonly ITaskService _taskService;
        private readonly IAuthService _authService;

        public TaskController(ITaskService taskService, IAuthService authService)
        {
            _taskService = taskService;
            _authService = authService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<TaskDto>>> GetAllTasks()
        {
            var tasks = await _taskService.GetAllTasksAsync();
            return Ok(tasks);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<TaskDto>> GetTaskById(Guid id)
        {
            var task = await _taskService.GetTaskByIdAsync(id);
            return Ok(task);
        }

        [HttpGet("pending")]
        public async Task<ActionResult<IEnumerable<TaskDto>>> GetPendingTasks()
        {
            var userId = GetCurrentUserId();
            var tasks = await _taskService.GetPendingTasksAsync(userId);
            return Ok(tasks);
        }

        [HttpGet("my-tasks")]
        public async Task<ActionResult<IEnumerable<TaskDto>>> GetMyTasks()
        {
            var userId = GetCurrentUserId();
            var tasks = await _taskService.GetTasksByUserIdAsync(userId);
            return Ok(tasks);
        }

        [HttpPost]
        public async Task<ActionResult<TaskDto>> CreateTask([FromBody] CreateTaskDto createTaskDto)
        {
            var userId = GetCurrentUserId();
            var task = await _taskService.CreateTaskAsync(createTaskDto, userId);
            return CreatedAtAction(nameof(GetTaskById), new { id = task.Id }, task);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<TaskDto>> UpdateTask(Guid id, [FromBody] UpdateTaskDto updateTaskDto)
        {
            var task = await _taskService.UpdateTaskAsync(id, updateTaskDto);
            return Ok(task);
        }

        [HttpPut("{id}/assign")]
        public async Task<ActionResult<TaskDto>> AssignTask(Guid id, [FromBody] AssignTaskDto assignTaskDto)
        {
            var task = await _taskService.AssignTaskAsync(id, assignTaskDto);
            return Ok(task);
        }

        [HttpPut("{id}/complete")]
        public async Task<ActionResult<TaskDto>> CompleteTask(Guid id)
        {
            var task = await _taskService.CompleteTaskAsync(id);
            return Ok(task);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTask(Guid id)
        {
            await _taskService.DeleteTaskAsync(id);
            return NoContent();
        }

        private Guid GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                throw new UnauthorizedAccessException("User is not authenticated");
            }
            return Guid.Parse(userIdClaim.Value);
        }
    }
}