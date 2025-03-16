using System;
using System.Threading.Tasks;

namespace TaskManagementSystem.Core.Interfaces.Services
{
    public interface ICacheService
    {
        Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? absoluteExpiration = null);
        Task RemoveAsync(string key);
        Task<T> GetAsync<T>(string key);
        Task SetAsync<T>(string key, T value, TimeSpan? absoluteExpiration = null);
    }
}