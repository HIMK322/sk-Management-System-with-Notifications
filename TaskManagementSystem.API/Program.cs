using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Text;
using Hangfire;
using TaskManagementSystem.API.Extensions;
using TaskManagementSystem.API.Middlewares;
using TaskManagementSystem.Core.Interfaces.Repositories;
using TaskManagementSystem.Core.Interfaces.Services;
using TaskManagementSystem.Infrastructure.Data;
using TaskManagementSystem.Infrastructure.Data.Repositories;
using TaskManagementSystem.Infrastructure.Helpers;
using TaskManagementSystem.Infrastructure.Services;
using AspNetCoreRateLimit;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Threading.RateLimiting;
using System;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
});

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", policy =>
    {
        policy.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

// Configure Redis Cache
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("RedisCache") ?? "localhost:6379";
    options.InstanceName = "TaskManagement_";
});

// Configure Rate Limiting with Redis
builder.Services.AddMemoryCache();
builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
builder.Services.Configure<IpRateLimitPolicies>(builder.Configuration.GetSection("IpRateLimitPolicies"));
builder.Services.AddInMemoryRateLimiting();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

// Configure DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configure JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? 
                    throw new ArgumentNullException("Jwt:Key", "JWT key is not configured")))
        };
    });

// Register repositories
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ITaskRepository, TaskRepository>();
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();

// Register services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddSingleton<ICacheService, RedisCacheService>();
builder.Services.AddScoped<INotificationService, NotificationService>();

// Register TaskService with cache dependency
builder.Services.AddScoped<ITaskService>(provider => new TaskService(
    provider.GetRequiredService<ITaskRepository>(),
    provider.GetRequiredService<IUserRepository>(),
    provider.GetRequiredService<INotificationService>(),
    provider.GetRequiredService<ICacheService>(),
    provider.GetRequiredService<ILogger<TaskService>>()
));

// Configure Hangfire with Redis
builder.Services.AddHangfireWithRedis(builder.Configuration);

// Add Health Checks
builder.Services.AddApplicationHealthChecks(builder.Configuration);

// Configure Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { 
        Title = "Task Management System API", 
        Version = "v1",
        Description = "API for managing tasks and notifications"
    });
    
    // Add JWT Authentication to Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
    c.CustomSchemaIds(type => type.FullName);
});

var app = builder.Build();

// Initialize Redis connection
var loggerFactory = app.Services.GetRequiredService<ILoggerFactory>();
var redisLogger = loggerFactory.CreateLogger<Program>();
RedisConnectionHelper.InitializeConnection(builder.Configuration, redisLogger);

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Task Management System API v1"));
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
}

// Use rate limiting middleware
app.UseIpRateLimiting();

// Custom middleware for exception handling
app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseHttpsRedirection();
app.UseCors("CorsPolicy");
app.UseRouting();

// Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

// Configure Hangfire dashboard
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    // Set to true in production to require authentication
    IsReadOnlyFunc = context => app.Environment.IsProduction()
});

// Map controllers and health checks
app.MapControllers();
app.MapHealthChecks();
app.MapHangfireDashboard();

// Ensure database is created and apply migrations
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        context.Database.Migrate();
        // Seed data if needed
        // await SeedData.InitializeAsync(services);
    }
    catch (Exception ex)
    {
        var dbLogger = services.GetRequiredService<ILogger<Program>>();
        dbLogger.LogError(ex, "An error occurred while migrating or initializing the database.");
    }
}

app.Run();