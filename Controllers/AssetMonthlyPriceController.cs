using InvestmentManager.Services.Interfaces;
using InvestmentManager.Shared.Models;
using InvestmentManager.Shared.Models.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace InvestmentManager.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AssetMonthlyPriceController(IAssetMonthlyPriceService assetMonthlyPriceService) : ControllerBase
    {
        private readonly IAssetMonthlyPriceService _assetMonthlyPriceService = assetMonthlyPriceService;

        [HttpPost("add-range")]
        public async Task GetAllAssets([FromBody] List<AssetMonthlyPriceDto> assetsMonthlyPriceDtos)
        {
            await _assetMonthlyPriceService.AddRangeAsync(assetsMonthlyPriceDtos);
        }

        [HttpGet("get-monthly-prices")]
        public async Task<IReadOnlyList<AssetMonthlyPrice>> GetAllAssets2(AssetMonthlyPriceRequest assetMonthlyPriceRequest)
        {
            return await _assetMonthlyPriceService.GetAssetMonthlyPricesAsync(assetMonthlyPriceRequest);
        }
    }
}
