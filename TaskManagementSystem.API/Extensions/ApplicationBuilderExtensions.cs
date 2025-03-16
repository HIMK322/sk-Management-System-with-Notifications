using Hangfire;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TaskManagementSystem.API.Middlewares;
using TaskManagementSystem.Infrastructure.Data;

namespace TaskManagementSystem.API.Extensions
{
    public static class ApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseCustomExceptionHandler(this IApplicationBuilder app)
        {
            return app.UseMiddleware<ExceptionHandlingMiddleware>();
        }

        public static IApplicationBuilder UseCustomRequestLogging(this IApplicationBuilder app)
        {
            return app.UseMiddleware<RequestLoggingMiddleware>();
        }

        public static void ApplyMigrations(this IApplicationBuilder app)
        {
            using var scope = app.ApplicationServices.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            if (context.Database.GetPendingMigrations().Any())
            {
                context.Database.Migrate();
            }
        }

        public static IApplicationBuilder UseHangfireDashboardWithAuth(this IApplicationBuilder app)
        {
            return app.UseHangfireDashboard("/hangfire", new DashboardOptions
            {
                // Optional: Add authorization for the Hangfire dashboard
                // Authorization = new[] { new HangfireAuthorizationFilter() }
            });
        }
    }
}