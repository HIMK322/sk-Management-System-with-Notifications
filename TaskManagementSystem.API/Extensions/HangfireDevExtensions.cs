using Hangfire;
using Hangfire.MemoryStorage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace TaskManagementSystem.API.Extensions
{
    public static class HangfireDevExtensions
    {
        public static IServiceCollection AddHangfireWithMemoryStorage(this IServiceCollection services)
        {
            // Configure Hangfire with in-memory storage
            services.AddHangfire(config =>
            {
                config.UseMemoryStorage();
            });
            
            services.AddHangfireServer(options =>
            {
                options.WorkerCount = 4;
                options.Queues = new[] { "default", "notifications", "critical" };
            });
            
            return services;
        }
    }
}