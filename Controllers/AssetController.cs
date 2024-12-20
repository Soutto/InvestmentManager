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

        [HttpGet("get-all")]
        public async Task<List<Asset>> GetAllAssets()
        {
            var assets = await _assetService.GetAllAsync();
            return assets;
        }

        [HttpGet("sync")]
        public async Task SyncApiAssetsWithDatabaseAsync()
        {
            await _assetService.SyncApiAssetsWithDatabaseAsync();
        }

        [HttpGet("teste")]
        public async Task<Portfolio> Teste()
        {
            return await _transactionService.GetPortfolioAsync("be43a447-a0c0-47d2-927d-4eb72e246478");
        }
        [HttpGet("teste2")]
        public async Task Teste2()
        {
            await _cacheService.DeleteAllKeysAsync();
        }
        [HttpGet("teste3")]
        public async Task<MonthlyHeritageEvolution> Teste3()
        {
            return await _transactionService.GetMonthlyHeritageEvolutionAsync("be43a447-a0c0-47d2-927d-4eb72e246478", 132);
        }
    }
}