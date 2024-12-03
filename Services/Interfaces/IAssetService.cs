using InvestmentManager.Models;

namespace InvestmentManager.Services.Interfaces
{
    public interface IAssetService
    {
        Task<List<Asset>> GetAllAsync();
        Task<List<string>> GetAllTickersTextAsync();
        Task<Asset> GetByIdAsync(string isinCode);
    }
}