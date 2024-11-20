using InvestmentManager.Data.Repositories.Interfaces;
using InvestmentManager.Models;
using InvestmentManager.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace InvestmentManager.Services
{
    public class TransactionService : ITransactionService
    {
        private readonly IRepository<Transaction> _transactionRepository;

        //private readonly ILogger _logger;

        public TransactionService(IRepository<Transaction> repository)
        {
             _transactionRepository = repository;
        }

        public Task AddAsync(Transaction transaction)
        {
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<Transaction>> GetAllByUserIdAsync(string? userId)
        {
            return await _transactionRepository.Query().Where(transaction => transaction.UserId == userId).ToListAsync();
        }

        public async Task<Transaction?> GetByIdAsync(Guid id) 
        {
            return await _transactionRepository.GetByIdAsync(id);
        }

        public void Remove(Transaction transaction)
        {
            throw new NotImplementedException();
        }

        public void Update(Transaction transaction)
        {
            throw new NotImplementedException();
        }
    }
}
