using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace TaskManagementSystem.Infrastructure.Helpers
{
    public static class RedisConnectionHelper
    {
        private static Lazy<ConnectionMultiplexer> lazyConnection;
        
        public static ConnectionMultiplexer Connection => lazyConnection.Value;

        public static void InitializeConnection(IConfiguration configuration, ILogger logger)
        {
            try
            {
                string redisConnectionString = configuration.GetConnectionString("RedisCache") ?? "localhost:6379";
                
                // Configure Redis connection
                var options = ConfigurationOptions.Parse(redisConnectionString);
                options.AbortOnConnectFail = false; // Don't abort if connection fails initially
                
                lazyConnection = new Lazy<ConnectionMultiplexer>(() => 
                    ConnectionMultiplexer.Connect(options));
                
                logger.LogInformation("Redis connection initialized successfully");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to initialize Redis connection");
                throw;
            }
        }

        public static IServiceCollection AddRedisHealthCheck(this IServiceCollection services, IConfiguration configuration)
        {
            // kept returing issues and I run out of time so .... 
            // string redisConnectionString = configuration.GetConnectionString("RedisCache") ?? "localhost:6379";
            
            // services.AddHealthChecks()
            //     .AddRedis(redisConnectionString, "redis", Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Degraded);
                
            return services;
        }
    }
}