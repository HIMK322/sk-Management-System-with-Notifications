# Task Management System

A robust ASP.NET Core 8 application for managing tasks and sending notifications when tasks are assigned.

## Features

- User Authentication using JWT
- Task Management (CRUD operations)
- Task Assignment with notifications
- Optimized database queries for pending tasks
- Background job processing for notifications
- Soft delete implementation for data integrity

## Technologies

- ASP.NET Core 8
- Entity Framework Core with PostgreSQL
- Hangfire for background jobs
- Redis for caching and rate limiting
- Serilog for structured logging
- Swagger for API documentation
- Docker containerization

## Setup Instructions

### Prerequisites

- .NET 8 SDK
- PostgreSQL
- Docker (optional)

### Configuration

1. Update the connection string in `appsettings.json` to point to your PostgreSQL server:
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Server=localhost;Port=5432;Database=TaskManagementDb;User Id=postgres;Password=your_password;"
   }
   ```

2. Set a strong secret key for JWT authentication in `appsettings.json`:
   ```json
   "Jwt": {
     "Key": "your_secret_key_here_at_least_32_characters_long",
     "Issuer": "TaskManagementSystem",
     "Audience": "TaskManagementSystemUsers"
   }
   ```

### Running the Application

#### Using .NET CLI

```bash
# Clone the repository
git clone https://github.com/your-username/TaskManagementSystem.git
cd TaskManagementSystem

# Restore dependencies
dotnet restore

# Install required packages
# Redis and caching
dotnet add TaskManagementSystem.API/TaskManagementSystem.API.csproj package Microsoft.Extensions.Caching.StackExchangeRedis
dotnet add TaskManagementSystem.API/TaskManagementSystem.API.csproj package AspNetCoreRateLimit
dotnet add TaskManagementSystem.Infrastructure/TaskManagementSystem.Infrastructure.csproj package StackExchange.Redis

# Health checks
dotnet add TaskManagementSystem.API/TaskManagementSystem.API.csproj package AspNetCore.HealthChecks.NpgSql
dotnet add TaskManagementSystem.API/TaskManagementSystem.API.csproj package AspNetCore.HealthChecks.Redis
dotnet add TaskManagementSystem.API/TaskManagementSystem.API.csproj package AspNetCore.HealthChecks.Hangfire

# Hangfire Redis
dotnet add TaskManagementSystem.API/TaskManagementSystem.API.csproj package Hangfire.Redis.StackExchange

# Swagger Annotations
dotnet add TaskManagementSystem.Core/TaskManagementSystem.Core.csproj package Swashbuckle.AspNetCore.Annotations

# Apply migrations
dotnet ef database update -p TaskManagementSystem.Infrastructure -s TaskManagementSystem.API

# Run the application
dotnet run -p TaskManagementSystem.API
```

#### Using Docker

```bash
# Clone the repository
git clone https://github.com/your-username/TaskManagementSystem.git
cd TaskManagementSystem

# Start the services
docker-compose up -d

# Access services:
# - API & Swagger: http://localhost:8080/swagger
# - Redis Commander: http://localhost:8081
# - pgAdmin: http://localhost:5050 (login: admin@admin.com / password: admin)
# - Hangfire Dashboard: http://localhost:8080/hangfire
```

### Accessing the Application

### Local Development
- API: `https://localhost:7055` or `http://localhost:5055` (ports may vary)
- Swagger UI: `https://localhost:7055/swagger` or `http://localhost:5055/swagger`
- Hangfire Dashboard: `https://localhost:7055/hangfire` or `http://localhost:5055/hangfire`
- Health Status: `https://localhost:7055/health`

### Docker Environment
- API & Swagger: `http://localhost:8080/swagger`
- Hangfire Dashboard: `http://localhost:8080/hangfire`
- Redis Commander UI: `http://localhost:8081`
- pgAdmin: `http://localhost:5050` (login: admin@admin.com / password: admin)
- Health Status: `http://localhost:8080/health`

## API Endpoints

| HTTP Method | Endpoint | Description | Auth Required |
|-------------|----------|-------------|--------------|
| POST | /api/auth/register | Register a new user | No |
| POST | /api/auth/login | Authenticate and return JWT | No |
| POST | /api/tasks | Create a new task | Yes |
| PUT | /api/tasks/{id}/assign | Assign a task to another user | Yes |
| PUT | /api/tasks/{id}/complete | Mark a task as completed | Yes |
| DELETE | /api/tasks/{id} | Soft delete a task | Yes |
| GET | /api/tasks/pending | Retrieve non-completed tasks | Yes |
| GET | /api/tasks/deleted | Retrieve soft-deleted tasks | Yes (Admin) |
| POST | /api/tasks/{id}/restore | Restore a soft-deleted task | Yes (Admin) |
| GET | /api/notifications | Retrieve notifications for the logged-in user | Yes |

## Implementation Details

### Soft Delete Implementation

The application implements a soft delete mechanism for tasks to maintain data integrity and history:

1. **How It Works**:
   - Tasks are never physically removed from the database
   - When a delete operation is performed, tasks are marked with `IsDeleted = true` and `DeletedAt` timestamp
   - Global query filter automatically excludes soft-deleted tasks from regular queries
   - Special endpoints allow administrators to view and restore deleted tasks

2. **Implementation Details**:
   - `ISoftDelete` interface defines the contract for entities supporting soft delete
   - Entity Framework global query filters automatically filter out deleted items
   - DbContext `SaveChanges` methods intercept delete operations and convert them to soft deletes
   - Repository pattern encapsulates the complexity of handling soft-deleted entities

3. **Code Example**:
   ```csharp
   // In ApplicationDbContext
   protected override void OnModelCreating(ModelBuilder modelBuilder)
   {
       // Add global query filter for soft delete
       modelBuilder.Entity<TaskItem>().HasQueryFilter(t => !t.IsDeleted);
   }

   // In SaveChanges method
   private void UpdateSoftDeleteStatuses()
   {
       foreach (var entry in ChangeTracker.Entries())
       {
           if (entry.Entity is ISoftDelete softDeleteEntity && entry.State == EntityState.Deleted)
           {
               // Change state to modified and set IsDeleted flag
               entry.State = EntityState.Modified;
               softDeleteEntity.IsDeleted = true;
               softDeleteEntity.DeletedAt = DateTime.UtcNow;
           }
       }
   }
   ```

4. **Benefits**:
   - Maintains historical data for audit and compliance purposes
   - Protects against accidental data loss
   - Allows data recovery through restore operations
   - Preserves referential integrity in the database
   - Enables "trash bin" functionality for administrative users

### Redis Caching and Rate Limiting

The application implements Redis for two primary purposes:

1. **Performance Optimization with Caching**:
   - Frequently accessed data like pending tasks and user-specific tasks are cached in Redis
   - The cache service implements a generic pattern for fetching or creating cached items:
     ```csharp
     // Example usage in TaskService
     return await _cacheService.GetOrCreateAsync(
         $"pending_tasks_{userId}",
         async () => {
             // This expensive database query only runs on cache miss
             var tasks = await _taskRepository.GetPendingTasksAsync(userId);
             return tasks.Select(MapToTaskDto).ToList();
         },
         TimeSpan.FromMinutes(10));
     ```
   - Intelligent cache invalidation occurs when tasks are created, updated, or deleted
   - Cache entries have configurable expiration times to ensure data freshness
   - Strategy pattern allows for swapping cache implementations without changing business logic

2. **Rate Limiting for API Protection**:
   - IP-based rate limiting prevents abuse of the API endpoints
   - Different rate limits are applied to different endpoint groups:
     * Authentication endpoints: 10 requests per minute
     * Task endpoints: 30 requests per minute
     * Notification endpoints: 20 requests per minute
     * Global limit: 5 requests per second
   - Rate limit counters are stored in Redis for persistence across application instances
   - Exceeded rate limits return 429 (Too Many Requests) responses

### Docker Support

The application includes Docker support for containerized deployment:

1. **Multi-stage Docker build**:
   - Optimized for smaller final image size
   - Separate build and runtime environments

2. **Docker Compose configuration**:
   - Complete environment with PostgreSQL, Redis and the API
   - Pre-configured environment variables
   - Volume persistence for database data
   - Health checks for all services
   - Administration tools:
     * Redis Commander for Redis monitoring at http://localhost:8081
     * pgAdmin for PostgreSQL management at http://localhost:5050
   - Service dependencies with proper startup ordering

Task assignment is implemented using an asynchronous notification system:

1. **Task Assignment Process**:
   - When a task is assigned to a user (either during creation or via the assign endpoint), the system captures the assignment action.
   - The `TaskService` calls `NotificationService.CreateTaskAssignmentNotificationAsync()`, which schedules a background job.
   - The background job is handled by Hangfire, ensuring the notification creation doesn't block the API response.

2. **Background Processing with Hangfire**:
   - Hangfire manages the queue of notification jobs, ensuring reliable processing even if the application restarts.
   - The `CreateTaskAssignmentNotificationJob` method is decorated with `[AutomaticRetry]` to ensure delivery even in case of temporary failures.
   - This approach ensures notification handling scales well with increasing user and task volumes.

3. **Notification Storage**:
   - Notifications are stored in the database for persistence and retrieval.
   - Each notification includes metadata like type, creation time, read status, and references to related entities.
   - Users can query their notifications and mark them as read independently of task operations.

This decoupled approach provides several benefits:
- Better user experience with faster API responses
- Improved system resilience with retry capabilities 
- Ability to extend with additional notification channels (email, push) without affecting the core assignment process

### Query Optimization for Non-Completed Tasks

The query for retrieving non-completed tasks is optimized through several techniques:

1. **Database Indexing**:
   - An index is created on the `Status` column in the Tasks table:
     ```csharp
     modelBuilder.Entity<TaskItem>()
         .HasIndex(t => t.Status)
         .HasName("IX_Tasks_Status");
     ```
   - Additional indexes on `AssignedToId` and `CreatedById` enhance related queries.

2. **Efficient Query Construction**:
   - The query specifically filters for non-completed tasks using the indexed Status column:
     ```csharp
     return await _context.Tasks
         .Where(t => t.AssignedToId == userId && 
                    (t.Status == TaskStatus.Pending || t.Status == TaskStatus.InProgress))
         .ToListAsync();
     ```
   - This query leverages the index to quickly eliminate completed tasks without scanning the entire table.

3. **Eager Loading Optimization**:
   - Related entities are loaded using strategic eager loading with `Include()` to avoid N+1 query problems.
   - Only necessary related data is loaded to minimize memory usage.

4. **Entity Framework Query Optimization**:
   - The query is designed to translate efficiently to SQL, avoiding client-side evaluation.
   - EF Core's parameterized queries prevent SQL injection and enable query plan caching.

These optimizations ensure the system performs well even with millions of tasks in the database, by:
- Minimizing database reads through proper indexing
- Reducing data transfer by filtering at the database level
- Avoiding unnecessary round-trips to the database

## Testing

Run the tests using the .NET CLI:

```bash
dotnet test TaskManagementSystem.Tests
```

## License

MIT