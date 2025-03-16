using Microsoft.EntityFrameworkCore;
using System;
using TaskManagementSystem.Infrastructure.Data;

namespace TaskManagementSystem.Tests.Helpers
{
    public static class DbContextFactory
    {
        public static ApplicationDbContext Create()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            var context = new ApplicationDbContext(options);
            context.Database.EnsureCreated();
            
            return context;
        }
    }
}