using Hangfire;
using Hangfire.Redis.StackExchange;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace TaskManagementSystem.API.Extensions
{
    public static class HangfireExtensions
    {
        public static IServiceCollection AddHangfireWithRedis(this IServiceCollection services, IConfiguration configuration)
        {
            // Get Redis connection string from configuration
            string redisConnectionString = configuration.GetConnectionString("RedisCache") ?? "localhost:6379";
            
            // Configure Hangfire with Redis
            services.AddHangfire(config =>
            {
                var options = new RedisStorageOptions
                {
                    Prefix = "hangfire:",
                    SucceededListSize = 1000,
                    DeletedListSize = 1000,
                    InvisibilityTimeout = System.TimeSpan.FromHours(1)
                };
                
                var connectionMultiplexer = ConnectionMultiplexer.Connect(redisConnectionString);
                config.UseRedisStorage(connectionMultiplexer, options);
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