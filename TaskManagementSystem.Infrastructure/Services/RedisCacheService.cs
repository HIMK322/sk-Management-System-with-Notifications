using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using TaskManagementSystem.Core.Interfaces.Services;

namespace TaskManagementSystem.Infrastructure.Services
{
    public class RedisCacheService : ICacheService
    {
        private readonly IDistributedCache _cache;
        private readonly DistributedCacheEntryOptions _defaultOptions;

        public RedisCacheService(IDistributedCache cache)
        {
            _cache = cache;
            _defaultOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15)
            };
        }

        public async Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? absoluteExpiration = null)
        {
            var cachedData = await GetAsync<T>(key);
            if (cachedData != null)
            {
                return cachedData;
            }

            var result = await factory();
            await SetAsync(key, result, absoluteExpiration);
            
            return result;
        }

        public async Task<T> GetAsync<T>(string key)
        {
            var data = await _cache.GetStringAsync(key);
            
            if (string.IsNullOrEmpty(data))
                return default;
            
            return JsonSerializer.Deserialize<T>(data);
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? absoluteExpiration = null)
        {
            var options = new DistributedCacheEntryOptions();
            
            if (absoluteExpiration.HasValue)
                options.AbsoluteExpirationRelativeToNow = absoluteExpiration.Value;
            else
                options = _defaultOptions;
            
            var data = JsonSerializer.Serialize(value);
            await _cache.SetStringAsync(key, data, options);
        }

        public async Task RemoveAsync(string key)
        {
            await _cache.RemoveAsync(key);
        }
    }
}