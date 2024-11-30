using InvestmentManager.Models;

namespace InvestmentManager.Services.Interfaces
{
    public interface IAssetService
    {
        Task<List<Asset>> GetAllAsync();
    }
}