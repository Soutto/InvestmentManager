using InvestmentManager.Services.Interfaces;
using InvestmentManager.Shared.Models;
using Microsoft.AspNetCore.Mvc;

namespace InvestmentManager.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AssetController : ControllerBase
    {
        private readonly IAssetService AssetService;

        public AssetController(IAssetService assetService)
        {
            AssetService = assetService;
        }

        [HttpGet("GetAll")]
        public async Task<List<Asset>> GetAllAssets()
        {
            var assets = await AssetService.GetAllAsync();
            return assets;
        }
    }
}