using InvestmentManager.Data.Repositories.Interfaces;
using InvestmentManager.Shared.Models;
using InvestmentManager.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using InvestmentManager.Shared.Models.DTOs;
using InvestmentManager.Shared.Models.Application;
using InvestmentManager.Shared.Factories;
using InvestmentManager.Exceptions;
using InvestmentManager.Utils;
using System.Collections.ObjectModel;

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
                await _cacheService.RefreshKeyAsync(CacheKeysFactory.UserTransactions(userId), _cacheService.DefaultExpiration());
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

        public async Task<MonthlyHeritageEvolution> GetMonthlyHeritageEvolutionAsync (string? userId)
        {
            ArgumentException.ThrowIfNullOrEmpty(userId);

            var transactions = await GetUserTransactionsDtoAsync(userId);

            if (transactions is null || transactions.Count == 0)
            {
                return new MonthlyHeritageEvolution([]);
            }

            // Identify the first and last month we need to cover
            var firstTransactionDate = transactions.First().TransactionDate;
            // We'll compute up to the currentDate's month
            var startMonth = new DateTime(firstTransactionDate.Year, firstTransactionDate.Month, 1);
            var endMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);

            // Group transactions by (Year, Month)
            var monthlyTransactions = transactions
                .OrderBy(t => t.TransactionDate)
                .GroupBy(t => new { t.TransactionDate.Year, t.TransactionDate.Month })
                .ToDictionary(
                    g => new DateTime(g.Key.Year, g.Key.Month, 1),
                    g => g.OrderBy(t => t.TransactionDate).ToList() // ensure monthly internal order
                );

            // Step 1: Sequentially compute cumulative holdings at the end of each month
            var monthlyHoldings = new List<(DateTime MonthEnd, Dictionary<string, decimal> Holdings)>();
            var currentHoldings = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

            // Iterate from startMonth to endMonth month by month
            for (var monthStart = startMonth; monthStart <= endMonth; monthStart = monthStart.AddMonths(1))
            {
                // Month-end = last calendar day of the month
                var daysInMonth = DateTime.DaysInMonth(monthStart.Year, monthStart.Month);
                var monthEnd = new DateTime(monthStart.Year, monthStart.Month, daysInMonth);

                // If we have transactions for this month, apply them
                if (monthlyTransactions.TryGetValue(monthStart, out var thisMonthTx))
                {
                    foreach (var t in thisMonthTx)
                    {
                        UpdateHoldings(currentHoldings, t.IsBuy, t.AssetIsinCode, t.Quantity);
                    }
                }
                // If no transactions, holdings remain as last month

                // Store a snapshot of holdings for this month-end
                monthlyHoldings.Add((monthEnd, new Dictionary<string, decimal>(currentHoldings, StringComparer.OrdinalIgnoreCase)));
            }

            // Step 2: Parallelize price retrieval and monthly value calculation
            //var tasks = monthlyHoldings.Select(async mh =>
            //{
            //    var (monthEnd, holdings) = mh;
            //    if (holdings.Count == 0)
            //    {
            //        // No assets held this month
            //        return new MonthlyHeritageRecord(
            //            monthEndDate: monthEnd,
            //            totalHeritage: 0m,
            //            assetValues: new ReadOnlyDictionary<string, decimal>(new Dictionary<string, decimal>())
            //        );
            //    }

            //    var assetIsins = holdings.Keys;
            //    //var prices = await getClosingPricesForMonthEnd(assetIsins, monthEnd).ConfigureAwait(false); TODO, CRIAR ESSE METODO

            //    // Calculate asset values for this month
            //    var assetValues = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
            //    foreach (var kvp in holdings)
            //    {
            //        var isin = kvp.Key;
            //        var quantity = kvp.Value;
            //        var price = prices.TryGetValue(isin, out var p) ? p : 0m;
            //        assetValues[isin] = quantity * price;
            //    }

            //    var totalValue = assetValues.Values.Sum();

            //    return new MonthlyHeritageRecord(
            //        monthEndDate: monthEnd,
            //        totalHeritage: totalValue,
            //        assetValues: new ReadOnlyDictionary<string, decimal>(assetValues)
            //    );
            //}).ToArray();

            //var records = await Task.WhenAll(tasks).ConfigureAwait(false);
            //return new MonthlyHeritageEvolution(records);
            return new MonthlyHeritageEvolution([]);
        }

        #region Private Methods

        private static void UpdateHoldings(Dictionary<string, decimal> holdings, bool isBuy, string isin, decimal quantity)
        {
            if (!holdings.TryGetValue(isin, out var currentQuantity))
                currentQuantity = 0m;

            var newQuantity = isBuy ? currentQuantity + quantity : currentQuantity - quantity;

            if (newQuantity <= 0)
                holdings.Remove(isin);
            else
                holdings[isin] = newQuantity;
        }

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
