using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Text.Json;
using System.Threading.Tasks;

namespace TaskManagementSystem.API.Extensions
{
    public static class HealthChecksExtensions
    {
        public static IServiceCollection AddApplicationHealthChecks(this IServiceCollection services, IConfiguration configuration)
        {
            var healthChecks = services.AddHealthChecks();
            
            // Add PostgreSQL health check
            healthChecks.AddNpgSql(
                configuration.GetConnectionString("DefaultConnection"),
                name: "postgres",
                failureStatus: HealthStatus.Degraded);
            
            // Add Redis health check
            healthChecks.AddRedis(
                configuration.GetConnectionString("RedisCache"),
                name: "redis",
                failureStatus: HealthStatus.Degraded);
            
            // Add Hangfire health check
            healthChecks.AddHangfire(
                options => options.MinimumAvailableServers = 1, 
                name: "hangfire",
                failureStatus: HealthStatus.Degraded);
            
            return services;
        }
        
        public static IEndpointRouteBuilder MapHealthChecks(this IEndpointRouteBuilder endpoints)
        {
            endpoints.MapHealthChecks("/health", new HealthCheckOptions
            {
                ResponseWriter = WriteHealthCheckResponse
            });
            
            endpoints.MapHealthChecks("/health/ready", new HealthCheckOptions
            {
                Predicate = registration => registration.Tags.Contains("ready"),
                ResponseWriter = WriteHealthCheckResponse
            });
            
            endpoints.MapHealthChecks("/health/live", new HealthCheckOptions
            {
                Predicate = _ => false,
                ResponseWriter = WriteHealthCheckResponse
            });
            
            return endpoints;
        }
        
        private static Task WriteHealthCheckResponse(HttpContext context, HealthReport report)
        {
            context.Response.ContentType = "application/json";
            
            var response = new
            {
                status = report.Status.ToString(),
                components = report.Entries.Select(e => new
                {
                    component = e.Key,
                    status = e.Value.Status.ToString(),
                    description = e.Value.Description,
                    duration = e.Value.Duration.TotalMilliseconds
                })
            };
            
            return context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }
    }
}