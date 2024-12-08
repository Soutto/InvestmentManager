using InvestmentManager.Data.Repositories.Interfaces;
using InvestmentManager.Shared.Models;
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

        private const string GetAllKey = "AssetsCache";
        private const string GetAllAssetsTickersKey = "TickersTextCache";

        public async Task<List<Asset>> GetAllAsync()
        {
            try
            {
                if (_memoryCache.TryGetValue(GetAllKey, out List<Asset>? cachedAssets))
                {
                    if (!cachedAssets.IsNullOrEmpty())
                    {
                        return cachedAssets!;
                    }
                    
                    throw new Exception("Assets not found");
                }

                var assets = await _assetRepository.Query().OrderBy(a => a.Ticker).AsNoTracking().ToListAsync();

                var cacheOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10), // Cache duration
                    SlidingExpiration = TimeSpan.FromMinutes(5) // Reset expiration on access
                };

                _memoryCache.Set(GetAllKey, assets, cacheOptions);

                return assets;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving assets");
                throw;
            }
        }

        public async Task<List<string>> GetAllAssetsTickersAsync()
        {
            try
            {
                if (_memoryCache.TryGetValue(GetAllAssetsTickersKey, out List<string>? cachedAssetsTickers))
                {
                    if (!cachedAssetsTickers.IsNullOrEmpty())
                    {
                        return cachedAssetsTickers!;
                    }
                    
                    throw new Exception("Assets tickers text not found");
                }

                var assetsTickers = await _assetRepository
                                .Query()
                                .OrderBy(a => a.Ticker)
                                .Select(a => a.Ticker)
                                .AsNoTracking()
                                .ToListAsync();

                var cacheOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10), 
                    SlidingExpiration = TimeSpan.FromMinutes(5) 
                };

                _memoryCache.Set(GetAllAssetsTickersKey, assetsTickers, cacheOptions);

                return assetsTickers;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving assets tickers");
                throw;
            }
        }
        
       public async Task<Asset> GetByIdAsync(string isinCode)
        {
            if (string.IsNullOrEmpty(isinCode))
            {
                _logger.LogError("Attempted to find a asset");
                throw new ArgumentNullException(nameof(isinCode), "IsinCode cannot be null.");
            }

            try
            {
                var asset = await _assetRepository.GetByIdAsync(isinCode);

                if (asset is null)
                {
                    _logger.LogWarning("Asset not found. IsinCode: {isinCode}", isinCode);
                    throw new KeyNotFoundException($"Asset with IsinCode {isinCode} not found.");
                }
                
                return asset;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting asset. IsinCode: {isinCode}", isinCode);
                throw;
            }
        }
        /// <summary>
        /// Clears the cache for assets.
        /// </summary>
        public void ClearCache()
        {
            _memoryCache.Remove(GetAllKey);
            _logger.LogInformation("Assets cache cleared.");
        }
    }
}
