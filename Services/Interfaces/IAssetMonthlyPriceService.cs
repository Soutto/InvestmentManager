using InvestmentManager.Shared.Models;
using InvestmentManager.Shared.Models.DTOs;

namespace InvestmentManager.Services.Interfaces
{
    public interface IAssetMonthlyPriceService
    {
        Task AddRangeAsync(List<AssetMonthlyPriceDto> assetsMonthlyPriceDtos);
        Task<IReadOnlyList<AssetMonthlyPrice>> GetAssetMonthlyPricesAsync(AssetMonthlyPriceRequest assetMonthlyPriceRequest);
    }
}
