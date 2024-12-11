using InvestmentManager.Services.Interfaces;
using InvestmentManager.Shared.Models;
using Microsoft.AspNetCore.Mvc;

namespace InvestmentManager.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AssetController(IAssetService assetService) : ControllerBase
    {
        private readonly IAssetService AssetService = assetService;

        [HttpGet("GetAll")]
        public async Task<List<Asset>> GetAllAssets()
        {
            var assets = await AssetService.GetAllAsync();
            return assets;
        }
    }
}