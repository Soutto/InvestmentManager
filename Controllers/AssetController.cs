using InvestmentManager.Services.Interfaces;
using InvestmentManager.Shared.Factories;
using InvestmentManager.Shared.Models;
using InvestmentManager.Shared.Models.Application;
using Microsoft.AspNetCore.Mvc;

namespace InvestmentManager.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AssetController(IAssetService assetService, ITransactionService transactionService, ICacheService cacheService) : ControllerBase
    {
        private readonly IAssetService _assetService = assetService;
        private readonly ITransactionService _transactionService = transactionService;
        private readonly ICacheService _cacheService = cacheService;
        [HttpGet("GetAll")]
        public async Task<List<Asset>> GetAllAssets()
        {
            var assets = await _assetService.GetAllAsync();
            return assets;
        }

        [HttpGet("SyncApiAssetsWithDatabase")]
        public async Task SyncApiAssetsWithDatabaseAsync()
        {
            await _assetService.SyncApiAssetsWithDatabaseAsync();
        }

        [HttpGet("teste")]
        public async Task<Portfolio> Teste()
        {
            return await _transactionService.GetPortfolioAsync("39359fce-5291-4119-91a7-a8831c1aa55d");
        }
        [HttpGet("teste2")]
        public async Task Teste2()
        {
            await _cacheService.DeleteAllKeysAsync();
        }
    }
}