using DadosDeMercadoClient.Clients;
using DadosDeMercadoClient.Interfaces;
using InvestmentManager.Data.Repositories.Interfaces;
using InvestmentManager.Services.Interfaces;
using InvestmentManager.Shared.Models;
using Microsoft.Extensions.Logging;

namespace InvestmentManager.Services
{
    public class AssetMonthlyPriceService(IRepository<AssetMonthlyPrice> assetMonthlyPriceServiceRepository, ILogger<AssetMonthlyPriceService> logger, ICacheService cacheService, IAssetClient assetClient) : IAssetMonthlyPriceService
    {
        private readonly IRepository<AssetMonthlyPrice> _assetMonthlyPriceServiceRepository = assetMonthlyPriceServiceRepository;
        private readonly ILogger<AssetMonthlyPriceService> _logger = logger;
        private readonly ICacheService _cacheService = cacheService;
        private readonly IAssetClient _assetClient = assetClient;

        #region Public Methods
        #endregion

        #region Private Methods
        #endregion
    }
}
