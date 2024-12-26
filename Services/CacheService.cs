using InvestmentManager.Services.Interfaces;
using StackExchange.Redis;
using System.Text.Json;

namespace InvestmentManager.Services
{
    public class CacheService(IConnectionMultiplexer connectionMultiplexer, ILogger<CacheService> logger) : ICacheService
    {
        private readonly IDatabase _database = connectionMultiplexer.GetDatabase();
        private readonly ILogger<CacheService> _logger = logger;

        public TimeSpan DefaultExpiration() => new(0, 10, 0);

        #region Public Methods

        public async Task<T?> GetOrSetAsync<T>(string key, Func<Task<T>> getFromSource, TimeSpan? expiration = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(key, nameof(key));

            if (await _database.KeyExistsAsync(key))
            {
                var cacheValue = await _database.StringGetAsync(key);
                return cacheValue.IsNullOrEmpty ? default : JsonSerializer.Deserialize<T>(cacheValue!);
            }
            else
            {
                var freshValue = await getFromSource();
                await SetAsync(key, freshValue, expiration);
                return freshValue;
            }
        }

        public async Task RefreshKeyAsync(string key, TimeSpan slidingExpiration)
        {
            await _database.KeyExpireAsync(key, slidingExpiration);
        }

        public async Task KeyDeleteAsync(string key)
        {
            await _database.KeyDeleteAsync(key);
        }

        public async Task DeleteAllKeysAsync()
        {
            await _database.ExecuteAsync("FLUSHDB");
        }

        #endregion

        #region Private Methods 

        private async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
        {
            try
            {
                string valueJson = JsonSerializer.Serialize(value);
                await _database.StringSetAsync(key, valueJson, expiration);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting cache for key '{key}'", key);
                throw;
            }
        }

        #endregion
    }
}
