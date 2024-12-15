using InvestmentManager.Shared.Models;

namespace InvestmentManager.Services.Interfaces
{
    public interface IAssetService
    {
        Task<List<Asset>> GetAllAsync();
        Task<List<string>> GetAllAssetsTickersAsync();
        Task<Dictionary<string, Asset>> GetAllDictionaryAsync();
        Task<Asset> GetByIdAsync(string isinCode);
        Task SyncApiAssetsWithDatabaseAsync();
    }
}