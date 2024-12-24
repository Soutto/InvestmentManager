using InvestmentManager.Shared.Models.Application;

namespace InvestmentManager.Services.Interfaces
{
    public interface INewsService
    {
        Task<IEnumerable<News>> GetAllAsync();
    }
}
