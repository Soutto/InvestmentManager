using InvestmentManager.Models;

namespace InvestmentManager.Services.Interfaces
{
    public interface ITransactionService
    {
        Task<List<Transaction>> GetAllByUserIdAsync(string? userId);
        Task AddAsync(Transaction transaction);
        Task RemoveAsync(Guid id);
    }
}
