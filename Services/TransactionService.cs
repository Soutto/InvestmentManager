using InvestmentManager.Data.Repositories.Interfaces;
using InvestmentManager.Shared.Models;
using InvestmentManager.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using InvestmentManager.Shared.Models.DTOs;
using InvestmentManager.Shared.Models.Application;
using InvestmentManager.Shared.Factories;
using InvestmentManager.Exceptions;
using System.Collections.ObjectModel;
using Microsoft.CodeAnalysis;

namespace InvestmentManager.Services
{
    /// <summary>
    /// Provides services for managing transactions.
    /// </summary>
    public class TransactionService(IRepository<Transaction> transactionRepository, ILogger<TransactionService> logger, ICacheService cacheService, IAssetService assetService, IAssetMonthlyPriceService assetMonthlyPriceService) : ITransactionService
    {
        private readonly IRepository<Transaction> _transactionRepository = transactionRepository;
        private readonly ILogger<TransactionService> _logger = logger;
        private readonly ICacheService _cacheService = cacheService;
        private readonly IAssetService _assetService = assetService;
        private readonly IAssetMonthlyPriceService _assetMonthlyPriceService = assetMonthlyPriceService;

        private static readonly HashSet<int> AllowedMonths = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 60, 360];

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
                        Quantity = group.Sum(t => t.IsBuy ? t.Quantity : -t.Quantity),
                        TotalPurchaseValue = group.Where(t => t.IsBuy).Sum(t => t.Quantity * t.UnitPrice),
                        TotalQuantityPurchased = group.Where(t => t.IsBuy).Sum(t => t.Quantity)
                    };
                })
                .ToList();

            var monthlyInvestment = await GetMonthlyInvestmentEvolutionAsync(userId, 1);
            decimal totalPurchase = monthlyInvestment.Investments[monthlyInvestment.Investments.Count - 1].TotalInvestment;
            return CreatePortfolio(portfolioAssets, totalPurchase);
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

        /// <summary>
        /// Calculates the monthly evolution of a user's financial holdings (heritage) based on their transactions 
        /// and a specified reporting period. The method processes transactions, computes cumulative holdings 
        /// for each month, retrieves corresponding asset prices, and generates a monthly heritage evolution report.
        /// </summary>
        /// <param name="userId">The unique identifier of the user whose transactions are to be processed.</param>
        /// <param name="monthsToInclude">
        /// The number of months to include in the heritage evolution report. If the user has fewer transactions 
        /// than the specified months, all available data will be included.
        /// </param>
        /// <returns>
        /// A task representing the asynchronous operation, returning a <see cref="MonthlyHeritageEvolution"/> 
        /// object containing a list of monthly heritage records for the specified period.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="userId"/> is null or empty, or if <paramref name="monthsToInclude"/> is less than or equal to zero.
        /// </exception>
        /// <remarks>
        /// - Transactions are grouped by month to compute cumulative holdings.
        /// - Asset prices for each holding are retrieved using the corresponding month and ISIN codes.
        /// - The report filters holdings based on the specified period (`monthsToInclude`) and adjusts 
        ///   intervals to reduce the data volume for larger periods.
        /// - The final report includes the total heritage value and asset-level breakdown for each month.
        /// - If the user has no transactions, an empty report is returned.
        /// </remarks>
        public async Task<MonthlyHeritageEvolution> GetMonthlyHeritageEvolutionAsync(string? userId, int monthsToInclude)
        {
            ArgumentException.ThrowIfNullOrEmpty(userId);
            ValidateMonthsToInclude(monthsToInclude);

            var transactions = await GetUserTransactionsOrderedAsync(userId);
            if (transactions.Count == 0)
            {
                return new MonthlyHeritageEvolution([]);
            }

            var monthlyTransactions = GroupTransactionsByMonth(transactions);
            var (startMonth, endMonth) = GetFirstAndLastTransactionDate(transactions);
            var monthlyHoldings = CalculateOrderedMonthlyHoldings(startMonth, endMonth, monthlyTransactions);
            var lastHoldings = TakeLastHoldings(monthlyHoldings, monthsToInclude);
            var intervalHoldings = TakeIntervalHoldings(lastHoldings);
            var prices = await FetchAssetPricesAsync(intervalHoldings);
            var records = BuildMonthlyHeritageRecords(intervalHoldings, prices);

            return new MonthlyHeritageEvolution(records);
        }

        public async Task<MonthlyInvestmentEvolution> GetMonthlyInvestmentEvolutionAsync(string? userId, int monthsToInclude)
        {
            ArgumentException.ThrowIfNullOrEmpty(userId);
            ValidateMonthsToInclude(monthsToInclude);

            var transactions = await GetUserTransactionsOrderedAsync(userId);
            if (transactions.Count == 0)
            {
                return new MonthlyInvestmentEvolution([]);
            }

            var monthlyTransactions = GroupTransactionsByMonth(transactions);
            var buysTransactions = transactions.Where(t => t.IsBuy).OrderBy(t => t.TransactionDate).ToList();

            var buysTransactionsDictionary = GroupSellsTransactionsByAsset(buysTransactions);

            var (startMonth, endMonth) = GetFirstAndLastTransactionDate(transactions);

            var monthlyInvestments = CalculateOrderedMonthlyInvestments(startMonth, endMonth, monthlyTransactions, buysTransactionsDictionary);
            var lastInvestments = TakeLastInvestments(monthlyInvestments, monthsToInclude);
            var intervalInvestments = TakeIntervalInvestments(lastInvestments);


            return new MonthlyInvestmentEvolution(intervalInvestments);
        }

        #region Private Methods

        private static void ValidateMonthsToInclude(int monthsToInclude)
        {
            if (!AllowedMonths.Contains(monthsToInclude))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(monthsToInclude),
                    $"The value of {nameof(monthsToInclude)} must be one of the following: {string.Join(", ", AllowedMonths)}."
                );
            }
        }

        /// <summary>
        /// Filters a list of monthly holdings based on an interval, retaining every nth holding 
        /// and always including the last holding in the list.
        /// </summary>
        /// <param name="monthlyHoldings">The list of <see cref="MonthlyHolding"/> objects to process.</param>
        /// <returns>
        /// A filtered list of <see cref="MonthlyHolding"/> objects:
        /// - If the input list has 12 or fewer items, the original list is returned.
        /// - If the list has more than 12 items, every 2nd or 3rd holding is retained based on the total count, 
        ///   with the last holding always included.
        /// </returns>
        /// <remarks>
        /// - An interval of 2 is used for lists with fewer than 60 items but more than 12.
        /// - An interval of 3 is used for lists with 60 or more items.
        /// </remarks>
        public static IEnumerable<MonthlyHolding> TakeIntervalHoldings(IEnumerable<MonthlyHolding> monthlyHoldings)
        {
            if (monthlyHoldings.Count() <= 12) return monthlyHoldings;

            int interval = monthlyHoldings.Count() >= 60 ? 3 : 2;

            return monthlyHoldings.Where((date, index) => index % interval == interval - 1 || index == monthlyHoldings.Count() - 1);
        }

        public static IEnumerable<MonthlyInvestment> TakeIntervalInvestments(IEnumerable<MonthlyInvestment> monthlyInvestments)
        {
            if (monthlyInvestments.Count() <= 12) return monthlyInvestments;

            int interval = monthlyInvestments.Count() >= 60 ? 3 : 2;

            return monthlyInvestments.Where((date, index) => index % interval == interval - 1 || index == monthlyInvestments.Count() - 1);
        }

        private static IEnumerable<MonthlyHolding> TakeLastHoldings(IEnumerable<MonthlyHolding> monthlyHoldings, int monthsToInclude)
        {
            return monthlyHoldings.TakeLast(monthsToInclude);
        }
        private static IEnumerable<MonthlyInvestment> TakeLastInvestments(IEnumerable<MonthlyInvestment> monthlyInvestments, int monthsToInclude)
        {
            return monthlyInvestments.TakeLast(monthsToInclude);
        }
        private async Task<List<TransactionDto>> GetUserTransactionsOrderedAsync(string userId)
        {
            var transactions = await GetUserTransactionsDtoAsync(userId);
            return [.. transactions.OrderBy(t => t.TransactionDate)];
        }

        private static (DateTime startMonth, DateTime endMonth) GetFirstAndLastTransactionDate(List<TransactionDto> transactions)
        {
            var firstTransactionDate = transactions.First().TransactionDate;
            var startMonth = new DateTime(firstTransactionDate.Year, firstTransactionDate.Month, 1);
            var endMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            return (startMonth, endMonth);
        }

        private static Dictionary<DateTime, List<TransactionDto>> GroupTransactionsByMonth(List<TransactionDto> transactions)
        {
            return transactions.GroupBy(t => new DateTime(t.TransactionDate.Year, t.TransactionDate.Month, 1))
                               .ToDictionary(g => g.Key, g => g.OrderBy(t => t.TransactionDate).ToList());
        }

        private static Dictionary<string, Dictionary<TransactionDto, decimal>> GroupSellsTransactionsByAsset(List<TransactionDto> transactions)
        {
            //TODO Pensar no cenario onde existe 2 transactions de venda no mesmo dia.
            return transactions
                .GroupBy(t => t.AssetIsinCode)
                .ToDictionary(
                    g => g.Key, 
                    g => g.OrderBy(t => t.TransactionDate)
                          .ToDictionary(
                              t => t, 
                              t => 0m   
                          )
                );
        }

        private static IEnumerable<MonthlyHolding> CalculateOrderedMonthlyHoldings(DateTime startMonth, DateTime endMonth, Dictionary<DateTime, List<TransactionDto>> monthlyTransactions)
        {
            List<MonthlyHolding> monthlyHoldings = [];
            var currentHoldings = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

            for (var monthStart = startMonth; monthStart <= endMonth; monthStart = monthStart.AddMonths(1))
            {
                var monthEnd = new DateTime(monthStart.Year, monthStart.Month, DateTime.DaysInMonth(monthStart.Year, monthStart.Month));

                if (monthlyTransactions.TryGetValue(monthStart, out var transactions))
                {
                    foreach (var transaction in transactions)
                    {
                        UpdateHoldings(currentHoldings, transaction.IsBuy, transaction.AssetIsinCode, transaction.Quantity);
                    }
                }

                monthlyHoldings.Add(new MonthlyHolding 
                { 
                    MonthEnd = monthEnd,
                    Holdings = new Dictionary<string, decimal>(currentHoldings, StringComparer.OrdinalIgnoreCase) 
                });
            }
            return monthlyHoldings.OrderBy(mh => mh.MonthEnd);
        }

        private static List<MonthlyInvestment> CalculateOrderedMonthlyInvestments(
                DateTime startMonth,
                DateTime endMonth,
                Dictionary<DateTime, List<TransactionDto>> monthlyTransactions,
                Dictionary<string, Dictionary<TransactionDto, decimal>> buyTransactions)
        {
            var monthlyInvestments = new List<MonthlyInvestment>();
            var currentInvestments = new MonthlyInvestment();

            for (var monthStart = startMonth; monthStart <= endMonth; monthStart = monthStart.AddMonths(1))
            {
                ProcessMonthlyTransactions(monthStart, monthlyTransactions, buyTransactions, currentInvestments);

                var monthEnd = GetMonthEnd(monthStart);
                currentInvestments.MonthEnd = monthEnd;
                monthlyInvestments.Add(new MonthlyInvestment
                {
                    MonthEnd = monthEnd,
                    TotalInvestment = currentInvestments.TotalInvestment
                });
            }

            return monthlyInvestments;
        }

        private static void ProcessMonthlyTransactions(
            DateTime monthStart,
            Dictionary<DateTime, List<TransactionDto>> monthlyTransactions,
            Dictionary<string, Dictionary<TransactionDto, decimal>> buyTransactions,
            MonthlyInvestment currentInvestments)
        {
            if (!monthlyTransactions.TryGetValue(monthStart, out var transactions))
                return;

            foreach (var transaction in transactions)
            {
                if (transaction.IsBuy)
                {
                    HandleBuyTransaction(transaction, currentInvestments);
                }
                else
                {
                    HandleSellTransaction(transaction, buyTransactions, currentInvestments);
                }
            }
        }

        private static void HandleBuyTransaction(TransactionDto transaction, MonthlyInvestment currentInvestments)
        {
            currentInvestments.TotalInvestment += transaction.TotalValue;
        }

        private static void HandleSellTransaction(
            TransactionDto transaction,
            Dictionary<string, Dictionary<TransactionDto, decimal>> buyTransactions,
            MonthlyInvestment currentInvestments)
        {
            if (!buyTransactions.TryGetValue(transaction.AssetIsinCode, out var transactions))
                return;

            var quantityToSell = transaction.Quantity;

            foreach (var (buyTransaction, soldQuantity) in transactions.ToList())
            {
                quantityToSell = ProcessSellQuantity(buyTransaction, soldQuantity, transactions, quantityToSell, currentInvestments);

                if (quantityToSell == 0)
                    break;
            }
        }

        private static decimal ProcessSellQuantity(
            TransactionDto buyTransaction,
            decimal soldQuantity,
            Dictionary<TransactionDto, decimal> transactions,
            decimal quantityToSell,
            MonthlyInvestment currentInvestments)
        {
            var remainingBuyQuantity = buyTransaction.Quantity - soldQuantity;

            if (remainingBuyQuantity <= 0)
                return quantityToSell;

            var unitsToSell = Math.Min(remainingBuyQuantity, quantityToSell);

            var decrement = unitsToSell * buyTransaction.UnitPrice;
            currentInvestments.TotalInvestment -= decrement;
            transactions[buyTransaction] += unitsToSell;
            quantityToSell -= unitsToSell;

            return quantityToSell;
        }

        private static DateTime GetMonthEnd(DateTime monthStart)
        {
            return new DateTime(monthStart.Year, monthStart.Month, DateTime.DaysInMonth(monthStart.Year, monthStart.Month));
        }


        private async Task<IReadOnlyList<AssetMonthlyPrice>> FetchAssetPricesAsync(IEnumerable<MonthlyHolding> monthlyHoldings)
        {
            var request = new AssetMonthlyPriceRequest
            {
                AssetsIsinCode = monthlyHoldings.Select(mh => mh.Holdings.Keys.ToArray()).ToArray(),
                Dates = monthlyHoldings.Select(mh => mh.MonthEnd).ToArray()
            };

            return await _assetMonthlyPriceService.GetAssetMonthlyPricesAsync(request);
        }

        /// <summary>
        /// Builds a list of monthly heritage records based on asset holdings and their corresponding prices.
        /// </summary>
        /// <param name="monthlyHoldings">A list of month-end dates with associated asset holdings.</param>
        /// <param name="prices">A list of asset prices grouped by year and month.</param>
        /// <returns>A list of <see cref="MonthlyHeritageRecord"/> representing the heritage evolution.</returns>
        private static IEnumerable<MonthlyHeritageRecord> BuildMonthlyHeritageRecords(
            IEnumerable<MonthlyHolding> monthlyHoldings,
            IReadOnlyList<AssetMonthlyPrice> prices)
        {
            var priceDictionary = GroupPricesByYearAndMonth(prices);
            return monthlyHoldings.Select(holding => CreateMonthlyHeritageRecord(holding, priceDictionary));
        }

        /// <summary>
        /// Groups asset prices by year and month for efficient lookup.
        /// </summary>
        private static Dictionary<(int Year, int Month), List<AssetMonthlyPrice>> GroupPricesByYearAndMonth(
            IReadOnlyList<AssetMonthlyPrice> prices)
        {
            return prices
                .GroupBy(price => (price.Year, price.Month))
                .ToDictionary(group => group.Key, group => group.ToList());
        }

        /// <summary>
        /// Creates a heritage record for a specific month-end based on holdings and available prices.
        /// </summary>
        private static MonthlyHeritageRecord CreateMonthlyHeritageRecord(
            MonthlyHolding holding,
            Dictionary<(int Year, int Month), List<AssetMonthlyPrice>> priceDictionary)
        {
            var key = (holding.MonthEnd.Year, holding.MonthEnd.Month);

            var monthlyPrices = priceDictionary.GetValueOrDefault(key, []);
            var totalHeritage = CalculateTotalHeritage(holding.Holdings, monthlyPrices);
            var assetValues = CreateAssetValuesDictionary(monthlyPrices);

            return new MonthlyHeritageRecord(
                monthEnd: holding.MonthEnd,
                totalHeritage: totalHeritage,
                assetValues: new ReadOnlyDictionary<string, decimal>(assetValues)
            );
        }

        /// <summary>
        /// Calculates the total heritage value for the given holdings and asset prices.
        /// </summary>
        private static decimal CalculateTotalHeritage(
            Dictionary<string, decimal> holdings,
            List<AssetMonthlyPrice> prices)
        {
            return prices.Sum(price =>
            {
                var quantity = holdings.GetValueOrDefault(price.AssetIsinCode, 0);
                return price.Price * quantity;
            });
        }

        /// <summary>
        /// Creates a dictionary mapping asset ISIN codes to their respective prices.
        /// </summary>
        private static Dictionary<string, decimal> CreateAssetValuesDictionary(
            List<AssetMonthlyPrice> prices)
        {
            return prices.ToDictionary(price => price.AssetIsinCode, price => price.Price);
        }


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

        private static Portfolio CreatePortfolio(List<PortfolioAsset> assets, decimal totalPurchase)
        {
            return new Portfolio
            {
                PortfolioAssets = assets,
                TotalPurchaseValue = totalPurchase
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
