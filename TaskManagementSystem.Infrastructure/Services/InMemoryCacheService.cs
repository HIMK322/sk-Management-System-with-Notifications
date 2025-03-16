using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using TaskManagementSystem.Core.Interfaces.Services;

namespace TaskManagementSystem.Infrastructure.Services
{
    public class InMemoryCacheService : ICacheService
    {
        private readonly IMemoryCache _memoryCache;
        private readonly ILogger<InMemoryCacheService> _logger;

        public InMemoryCacheService(IMemoryCache memoryCache, ILogger<InMemoryCacheService> logger)
        {
            _memoryCache = memoryCache;
            _logger = logger;
        }

        public async Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? absoluteExpiration = null)
        {
            if (_memoryCache.TryGetValue(key, out T cachedValue))
            {
                return cachedValue;
            }

            var result = await factory();
            
            var options = new MemoryCacheEntryOptions();
            if (absoluteExpiration.HasValue)
            {
                options.AbsoluteExpirationRelativeToNow = absoluteExpiration.Value;
            }
            else
            {
                options.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15);
            }
            
            _memoryCache.Set(key, result, options);
            return result;
        }

        public Task<T> GetAsync<T>(string key)
        {
            if (_memoryCache.TryGetValue(key, out T value))
            {
                return Task.FromResult(value);
            }

            return Task.FromResult(default(T));
        }

        public Task SetAsync<T>(string key, T value, TimeSpan? absoluteExpiration = null)
        {
            var options = new MemoryCacheEntryOptions();
            if (absoluteExpiration.HasValue)
            {
                options.AbsoluteExpirationRelativeToNow = absoluteExpiration.Value;
            }
            else
            {
                options.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15);
            }
            
            _memoryCache.Set(key, value, options);
            return Task.CompletedTask;
        }

        public Task RemoveAsync(string key)
        {
            _memoryCache.Remove(key);
            return Task.CompletedTask;
        }
    }
}