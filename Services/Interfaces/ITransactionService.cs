using InvestmentManager.Shared.Models;
using InvestmentManager.Shared.Models.Application;
using InvestmentManager.Shared.Models.DTOs;

namespace InvestmentManager.Services.Interfaces
{
    public interface ITransactionService
    {
        Task<List<Transaction>> GetUserTransactionsAsync(string? userId);
        Task<List<TransactionDto>> GetUserTransactionsDtoAsync(string? userId);
        Task AddAsync(Transaction transaction);
        Task RemoveAsync(Guid id);
        Task<Portfolio> GetPortfolioAsync(string? userId);
    }
}
