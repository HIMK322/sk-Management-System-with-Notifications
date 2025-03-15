using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TaskManagementSystem.Core.Entities;

namespace TaskManagementSystem.Core.Interfaces.Repositories
{
    public interface IUserRepository
    {
        Task<User> GetByIdAsync(Guid id);
        Task<User> GetByEmailAsync(string email);
        Task<User> GetByUsernameAsync(string username);
        Task<IEnumerable<User>> GetAllAsync();
        Task<User> CreateAsync(User user);
        Task UpdateAsync(User user);
        Task DeleteAsync(Guid id);
    }
}