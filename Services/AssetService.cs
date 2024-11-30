using InvestmentManager.Data.Repositories.Interfaces;
using InvestmentManager.Models;
using InvestmentManager.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;

namespace InvestmentManager.Services
{
    /// <summary>
    /// Provides services for managing assets with caching.
    /// </summary>
    public class AssetService(IRepository<Asset> assetRepository, ILogger<AssetService> logger, IMemoryCache memoryCache) : IAssetService
    {
        private readonly IRepository<Asset> _assetRepository = assetRepository;
        private readonly ILogger<AssetService> _logger = logger;
        private readonly IMemoryCache _memoryCache = memoryCache;

        private const string CacheKey = "AssetsCache";

        public async Task<List<Asset>> GetAllAsync()
        {
            try
            {
                if (_memoryCache.TryGetValue(CacheKey, out List<Asset>? cachedAssets))
                {
                    if (!cachedAssets.IsNullOrEmpty())
                    {
                        return cachedAssets!;
                    }
                    
                    throw new Exception("Assets not found");
                }

                var assets = await _assetRepository.Query().AsNoTracking().ToListAsync();

                var cacheOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10), // Cache duration
                    SlidingExpiration = TimeSpan.FromMinutes(5) // Reset expiration on access
                };

                _memoryCache.Set(CacheKey, assets, cacheOptions);

                return assets;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving assets");
                throw;
            }
        }

        /// <summary>
        /// Clears the cache for assets.
        /// </summary>
        public void ClearCache()
        {
            _memoryCache.Remove(CacheKey);
            _logger.LogInformation("Assets cache cleared.");
        }
    }
}
