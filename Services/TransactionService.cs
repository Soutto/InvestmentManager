using InvestmentManager.Data.Repositories.Interfaces;
using InvestmentManager.Shared.Models;
using InvestmentManager.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using InvestmentManager.Shared.Models.DTOs;
using InvestmentManager.Shared.Models.Application;
using InvestmentManager.Shared.Factories;
using InvestmentManager.Exceptions;

namespace InvestmentManager.Services
{
    /// <summary>
    /// Provides services for managing transactions.
    /// </summary>
    public class TransactionService(IRepository<Transaction> transactionRepository, ILogger<TransactionService> logger, ICacheService cacheService, IAssetService assetService) : ITransactionService
    {
        private readonly IRepository<Transaction> _transactionRepository = transactionRepository;
        private readonly ILogger<TransactionService> _logger = logger;
        private readonly ICacheService _cacheService = cacheService;
        private readonly IAssetService _assetService = assetService;

        public async Task AddAsync(Transaction transaction)
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
                await _transactionRepository.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding transaction. ID: {TransactionId}", transaction.Id);
                throw;
            }
        }

        public async Task<List<Transaction>> GetUserTransactionsAsync(string? userId)
        {
            ArgumentException.ThrowIfNullOrEmpty(userId, nameof(userId));

            try
            {
                var transactions = await _cacheService.GetOrSetAsync(key: CacheKeysFactory.UserTransactions(userId),
                                                                    getFromSource: async () =>
                                                                    {
                                                                        return await GetFromDbAsync(userId);
                                                                    },
                                                                    expiration: _cacheService.DefaultExpiration());
                await SetTransactionsAssetsAsync(transactions);
                return transactions ?? [];
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving transactions for User ID: {UserId}", userId);
                throw;
            }

            async Task<List<Transaction>> GetFromDbAsync(string userId)
            {
                return await _transactionRepository
                    .Query()
                    .Where(t => t.UserId == userId)
                    .AsNoTracking()
                    .ToListAsync();
            }
        }

        public async Task<List<TransactionDto>> GetUserTransactionsDtoAsync(string? userId)
        {
            ArgumentException.ThrowIfNullOrEmpty(userId, nameof(userId));

            try
            {
                var transactions = await _cacheService.GetOrSetAsync(key: CacheKeysFactory.UserTransactionsDto(userId),
                                                    getFromSource: async () =>
                                                    {
                                                        return await GetFromSourceAsync(userId);
                                                    },
                                                    expiration: _cacheService.DefaultExpiration());

                await SetTransactionsDtoAssetsAsync(transactions);
                await _cacheService.RefreshKeyAsync(CacheKeysFactory.UserTransactionsDto(userId), _cacheService.DefaultExpiration());

                return transactions ?? [];
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving transactionsdto for User ID: {UserId}", userId);
                throw;
            }

            async Task<List<TransactionDto>> GetFromSourceAsync(string? userId)
            {
                var transactions = await GetUserTransactionsAsync(userId);
                return transactions.Select(t => new TransactionDto
                {
                    IsBuy = t.IsBuy,
                    TransactionDate = t.TransactionDate,
                    Quantity = t.Quantity,
                    UnitPrice = t.UnitPrice,
                    OtherCosts = t.OtherCosts,
                    AssetIsinCode = t.AssetIsinCode!
                }).ToList();
            }
        }

        public async Task<Portfolio> GetPortfolioAsync(string? userId)
        {
            ArgumentException.ThrowIfNullOrEmpty(userId, nameof(userId));

            var transactions = await GetUserTransactionsDtoAsync(userId);
            var assets = await _assetService.GetAllAsync();

            var portfolioAssets = transactions
                .GroupBy(t => t.AssetIsinCode)
                .Select(group =>
                {

                    return new PortfolioAsset
                    {
                        Asset = assets.First(a => a.IsinCode.Equals(group.Key, StringComparison.OrdinalIgnoreCase)),
                        Quantity = group.Sum(t => t.Quantity),
                        TotalPurchaseValue = group.Where(t => t.IsBuy).Sum(t => t.Quantity * t.UnitPrice),
                        TotalQuantityPurchased = group.Where(t => t.IsBuy).Sum(t => t.Quantity)
                    };
                })
                .ToList();

            return CreatePortfolio(portfolioAssets);
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

        #region Private Methods

        private static Portfolio CreatePortfolio(List<PortfolioAsset> assets)
        {
            return new Portfolio
            {
                PortfolioAssets = assets
            };
        }

        private async Task SetTransactionsDtoAssetsAsync(List<TransactionDto>? transactions)
        {
            if (transactions is null || transactions.Count == 0) return;

            var assetDictionary = await _assetService.GetAllDictionaryAsync();

            var missingAssets = transactions
                .Select(t => t.AssetIsinCode)
                .Where(isinCode => !assetDictionary.ContainsKey(isinCode))
                .Distinct()
                .ToList();

            if (missingAssets.Count > 0)
            {
                var message = $"The following assets were not found: {string.Join(", ", missingAssets)}";
                throw new AssetsNotFoundException(message);
            }

            foreach (var transaction in transactions)
            {
                transaction.Asset = assetDictionary[transaction.AssetIsinCode];
            }
        }

        private async Task SetTransactionsAssetsAsync(List<Transaction>? transactions)
        {
            if (transactions is null || transactions.Count == 0) return;

            var assetDictionary = await _assetService.GetAllDictionaryAsync();

            var missingAssets = transactions
                .Select(t => t.AssetIsinCode)
                .Where(isinCode => !assetDictionary.ContainsKey(isinCode!))
                .Distinct()
                .ToList();

            if (missingAssets.Count > 0)
            {
                var message = $"The following assets were not found: {string.Join(", ", missingAssets)}";
                throw new AssetsNotFoundException(message);
            }

            foreach (var transaction in transactions)
            {
                transaction.Asset = assetDictionary[transaction.AssetIsinCode!];
            }
        }

        #endregion
    }
}
