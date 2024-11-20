using InvestmentManager.Models;

namespace InvestmentManager.Services.Interfaces
{
    public interface ITransactionService
    {
        Task<Transaction?> GetByIdAsync(Guid id);
        Task<IEnumerable<Transaction>> GetAllByUserIdAsync(string? userId);
        Task AddAsync(Transaction transaction);
        void Update(Transaction transaction);
        void Remove(Transaction transaction);
    }
}
