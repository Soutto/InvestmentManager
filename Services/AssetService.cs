using InvestmentManager.Data.Repositories.Interfaces;
using InvestmentManager.Shared.Models;
using InvestmentManager.Services.Interfaces;
using DadosDeMercadoClient.Interfaces;
using InvestmentManager.Exceptions;
using InvestmentManager.Shared.Factories;
using DadosDeMercadoClient.Interfaces.Brapi;

namespace InvestmentManager.Services
{
    /// <summary>
    /// Provides services for managing assets with caching.
    /// </summary>
    public class AssetService(IRepository<Asset> assetRepository, ILogger<AssetService> logger, ICacheService cacheService, IAssetClient assetClient, IBrapiAssetClient brapiAssetClient) : IAssetService
    {
        private readonly IRepository<Asset> _assetRepository = assetRepository;
        private readonly ILogger<AssetService> _logger = logger;
        private readonly ICacheService _cacheService = cacheService;
        private readonly IAssetClient _assetClient = assetClient;
        private readonly IBrapiAssetClient _brapiAssetClient = brapiAssetClient;

        #region Public Methods

        public async Task<List<Asset>> GetAllAsync()
        {
            try
            {
                var assets = await _cacheService.GetOrSetAsync(key: CacheKeysFactory.Assets(),
                                                               getFromSource: _assetRepository.GetAllAsync);


                if (assets is null || assets.Count == 0)
                {
                    throw new AssetsNotFoundException("Assets not found"); 
                }

                return assets;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving assets");
                throw;
            }
        }

        public async Task<Dictionary<string, Asset>> GetAllDictionaryAsync()
        {
            try
            {
                var assetsDictionary = await _cacheService.GetOrSetAsync(key: CacheKeysFactory.AssetsDictionary(),
                                                                         getFromSource: GetAssetsDictionaryAsync);

                if (assetsDictionary is null || assetsDictionary.Count == 0)
                {
                    throw new AssetsNotFoundException("Assets dictionary not found");
                }

                return assetsDictionary;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving assets");
                throw;
            }

            async Task<Dictionary<string, Asset>> GetAssetsDictionaryAsync()
            {
                var assets = await GetAllAsync();
                return assets.ToDictionary(a => a.IsinCode, StringComparer.OrdinalIgnoreCase);
            }
        }

        public async Task<List<string>> GetAllAssetsTickersAsync()
        {
            try
            {
                var assetsTickers = await _cacheService.GetOrSetAsync(key: CacheKeysFactory.AssetsTickers(),
                                                                      getFromSource: GetAssetsTickersAsync);

                if (assetsTickers is null || assetsTickers.Count == 0)
                {
                    throw new AssetsNotFoundException("Assets dictionary not found");
                }

                return assetsTickers;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving assets");
                throw;
            }

            async Task<List<string>> GetAssetsTickersAsync()
            {
                var assets = await GetAllAsync();
                return assets.Select(a => a.Ticker).ToList();
            }
        }

        public async Task<Asset> GetByIdAsync(string isinCode)
        {
            ArgumentException.ThrowIfNullOrEmpty(isinCode, nameof(isinCode));

            try
            {
                var asset = await _assetRepository.GetByIdAsync(isinCode);

                return asset is null ? throw new KeyNotFoundException($"Asset with IsinCode {isinCode} not found.") : asset;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting asset. IsinCode: {isinCode}", isinCode);
                throw;
            }
        }

        //TODO - Por enquanto só está atualizando as ações para ver como vai ficando, depois precisa atualizar para pegar os outros tipos de ativos.
        public async Task SyncApiAssetsWithDatabaseAsync()
        {
            try
            {
                var updatedStocks = await _assetClient.GetAllStocksAsync();
                var databaseStocks = await _assetRepository.GetAllTrackedAsync();

                foreach (var updatedStock in updatedStocks)
                {
                    ProcessStock(updatedStock, databaseStocks);
                }

                await SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error when synchronizing api assets with database");
                throw;
            }
        }
        #endregion

        #region Private Methods
        private async Task SaveChangesAsync()
        {
            await _assetRepository.SaveChangesAsync();
        }

        private void ProcessStock(Asset updatedStock, List<Asset> databaseStocks)
        {
            var matchingAsset = FindMatchingAsset(databaseStocks, updatedStock);

            if (matchingAsset != null)
            {
                UpdateExistingAsset(matchingAsset, updatedStock);
            }
            else
            {
                AddNewAsset(updatedStock);
            }
        }

        private void AddNewAsset(Asset updatedStock)
        {
            _assetRepository.Add(updatedStock);
        }

        private static void UpdateExistingAsset(Asset updatedStock, Asset existingAsset)
        {
            existingAsset.CurrentPrice = updatedStock.CurrentPrice;

            if (!existingAsset.Ticker.Equals(updatedStock.Ticker, StringComparison.OrdinalIgnoreCase))
            {
                existingAsset.Ticker = updatedStock.Ticker;
            }
        }

        private static Asset? FindMatchingAsset(List<Asset> databaseStocks, Asset updatedStock)
        {
            return databaseStocks.FirstOrDefault(s => s.IsinCode.Equals(updatedStock.IsinCode, StringComparison.OrdinalIgnoreCase));
        }
        #endregion
    }
}
