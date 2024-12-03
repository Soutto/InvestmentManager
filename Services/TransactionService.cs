using InvestmentManager.Data.Repositories.Interfaces;
using InvestmentManager.Models;
using InvestmentManager.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace InvestmentManager.Services
{
    /// <summary>
    /// Provides services for managing transactions.
    /// </summary>
    public class TransactionService(IRepository<Transaction> transactionRepository, ILogger<TransactionService> logger) : ITransactionService
    {
        private readonly IRepository<Transaction> _transactionRepository = transactionRepository;
        private readonly ILogger<TransactionService> _logger = logger;

        public void Add(Transaction transaction)
        {
            if (transaction == null)
            {
                _logger.LogError("Attempted to add a null transaction.");
                throw new ArgumentNullException(nameof(transaction), "Transaction cannot be null.");
            }

            try
            {
                transaction.Asset = null;
                _transactionRepository.Add(transaction);
                _transactionRepository.SaveChanges();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding transaction. ID: {TransactionId}", transaction.Id);
                throw;
            }
        }

        public async Task<List<Transaction>> GetAllByUserIdAsync(string? userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                _logger.LogWarning("User ID is null or empty. Returning empty transaction list.");
                return [];
            }

            try
            {
                var transactions = await _transactionRepository
                    .Query()
                    .Where(t => t.UserId == userId)
                    .Include(t => t.Asset)
                    .AsNoTracking()
                    .ToListAsync();

                return transactions;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving transactions for User ID: {UserId}", userId);
                throw;
            }
        }

        public async Task RemoveAsync(Guid id)
        {
            if (id == Guid.Empty)
            {
                _logger.LogError("Attempted to remove a transaction with an empty GUID.");
                throw new ArgumentException("Transaction ID cannot be empty.", nameof(id));
            }

            try
            {
                var transaction = await _transactionRepository.GetByIdAsync(id);
                
                if (transaction == null)
                {
                    _logger.LogWarning("Transaction not found. ID: {TransactionId}", id);
                    throw new KeyNotFoundException($"Transaction with ID {id} not found.");
                }

                _transactionRepository.Remove(transaction);
                
                await _transactionRepository.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing transaction. ID: {TransactionId}", id);
                throw;
            }
        }
    }
}
