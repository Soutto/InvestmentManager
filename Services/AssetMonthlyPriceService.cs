using Azure;
using DadosDeMercadoClient.Interfaces;
using InvestmentManager.Data.Repositories.Interfaces;
using InvestmentManager.Services.Interfaces;
using InvestmentManager.Shared.Models;
using InvestmentManager.Shared.Models.DTOs;
using Microsoft.EntityFrameworkCore;
using Z.EntityFramework.Plus;

namespace InvestmentManager.Services
{
    public class AssetMonthlyPriceService(IRepository<AssetMonthlyPrice> assetMonthlyPriceRepository, IAssetService assetService, IAssetClient assetClient) : IAssetMonthlyPriceService
    {
        private readonly IRepository<AssetMonthlyPrice> _assetMonthlyPriceRepository = assetMonthlyPriceRepository;
        private readonly IAssetService _assetService = assetService;
        private readonly IAssetClient _assetClient = assetClient;
        #region Public Methods

        /// <summary>
        /// Adds a range of AssetMonthlyPrice records to the database asynchronously.
        /// </summary>
        /// <param name="assetsMonthlyPriceDtos">List of AssetMonthlyPriceDto objects to add.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        public async Task AddRangeAsync(List<AssetMonthlyPriceDto> assetsMonthlyPriceDtos)
        {
            ////Teste para inserir varios no banco
            assetsMonthlyPriceDtos = [];
            int year = 2014;
            int month = 1;
            decimal priceAALR3 = 10m; // Starting price for AALR3
            decimal priceABCB4 = 15m; // Starting price for ABCB4
            Random random = new();

            while (!(month == 12 && year == 2024))
            {
                // Simulate price fluctuations with a random factor
                decimal priceChangeAALR3 = (decimal)(random.NextDouble() - 0.5) * 2; // -1 to +1
                decimal priceChangeABCB4 = (decimal)(random.NextDouble() - 0.5) * 2; // -1 to +1

                // Apply the change, but ensure it doesn't go below zero
                priceAALR3 += priceChangeAALR3;
                priceABCB4 += priceChangeABCB4;
                if (priceAALR3 < 0) priceAALR3 = 0;
                if (priceABCB4 < 0) priceABCB4 = 0;

                // Add a slight upward trend over time
                priceAALR3 += 0.05m;
                priceABCB4 += 0.08m;

                assetsMonthlyPriceDtos.Add(new AssetMonthlyPriceDto
                {
                    Ticker = "AALR3",
                    Year = year,
                    Month = month,
                    Price = priceAALR3
                });
                assetsMonthlyPriceDtos.Add(new AssetMonthlyPriceDto
                {
                    Ticker = "ABCB4",
                    Year = year,
                    Month = month,
                    Price = priceABCB4
                });
                month++;
                if (month == 13)
                {
                    month = 1;
                    year++;
                }
            }
            /////
            if (assetsMonthlyPriceDtos.Count == 0)
            {
                return;
            }

            var assetsDictionary = await GetAssetsDictionaryAsync();
            List<AssetMonthlyPrice> assetsMonthlyPrices = [];

            foreach (var dto in assetsMonthlyPriceDtos)
            {
                if (!assetsDictionary.TryGetValue(dto.Ticker, out var asset))
                {
                    continue;
                }

                assetsMonthlyPrices.Add(new AssetMonthlyPrice
                {
                    AssetIsinCode = asset.IsinCode,
                    Month = dto.Month,
                    Year = dto.Year,
                    Price = dto.Price
                });
            }

            _assetMonthlyPriceRepository.AddRange(assetsMonthlyPrices);
            await _assetMonthlyPriceRepository.SaveChangesAsync();
        }

        /// <summary>
        /// Retrieves a list of <see cref="AssetMonthlyPrice"/> records based on specified ISIN codes and corresponding dates.
        /// Each set of ISIN codes aligns with a specific date. The method handles current month prices directly
        /// and batches historical queries for efficient deferred execution.
        /// </summary>
        /// <param name="assetMonthlyPriceRequest">The request containing ISIN codes and their respective dates.</param>
        /// <returns>
        /// A task representing the asynchronous operation, returning a read-only list of 
        /// <see cref="AssetMonthlyPrice"/> objects matching the provided ISIN codes and dates.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Thrown when the request contains null or empty ISIN codes, null or empty dates,
        /// or if the lengths of the ISIN codes and dates arrays do not match.
        /// </exception>
        public async Task<IReadOnlyList<AssetMonthlyPrice>> GetAssetMonthlyPricesAsync(AssetMonthlyPriceRequest assetMonthlyPriceRequest)
        {
            ValidateRequest(assetMonthlyPriceRequest);

            var assetsDictionary = await _assetService.GetAllDictionaryAsync();
            var results = new List<AssetMonthlyPrice>();
            var queries = PrepareQueries(assetMonthlyPriceRequest, assetsDictionary, results);

            if (queries.Count > 0)
            {
                await ExecuteBatchQueriesAsync(queries, results);
            }

            return results.AsReadOnly();
        }


        #endregion

        #region Private Methods

        /// <summary>
        /// Validates the request object for null or inconsistent data.
        /// </summary>
        private static void ValidateRequest(AssetMonthlyPriceRequest request)
        {
            if (request.AssetsIsinCode == null || request.AssetsIsinCode.Length == 0)
                throw new ArgumentException("Assets ISIN code array cannot be null or empty.", nameof(request.AssetsIsinCode));

            if (request.Dates == null || request.Dates.Length == 0)
                throw new ArgumentException("Dates array cannot be null or empty.", nameof(request.Dates));

            if (request.AssetsIsinCode.Length != request.Dates.Length)
                throw new ArgumentException("The lengths of assetsIsinCode and dates arrays must match.");
        }

        /// <summary>
        /// Prepares batch queries and handles the current month's data.
        /// </summary>
        private List<QueryFutureEnumerable<AssetMonthlyPrice>> PrepareQueries(
            AssetMonthlyPriceRequest request,
            Dictionary<string, Asset> assetsDictionary,
            List<AssetMonthlyPrice> results)
        {
            var queries = new List<QueryFutureEnumerable<AssetMonthlyPrice>>();

            for (int i = 0; i < request.AssetsIsinCode.Length; i++)
            {
                var isinCodes = request.AssetsIsinCode[i];
                var date = request.Dates[i];

                if (isinCodes == null || isinCodes.Length == 0) continue;

                if (IsCurrentMonth(date))
                {
                    AddCurrentMonthPrices(isinCodes, date, assetsDictionary, results);
                }
                else
                {
                    var query = CreateHistoricalPriceQuery(isinCodes, date);
                    queries.Add(query);
                }
            }

            return queries;
        }

        /// <summary>
        /// Checks if the given date corresponds to the current month.
        /// </summary>
        private static bool IsCurrentMonth(DateTime date) =>
            date.Year == DateTime.Now.Year && date.Month == DateTime.Now.Month;

        /// <summary>
        /// Adds the current month's prices directly from the assets dictionary.
        /// </summary>
        private static void AddCurrentMonthPrices(
            string[] isinCodes, DateTime date,
            Dictionary<string, Asset> assetsDictionary,
            List<AssetMonthlyPrice> results)
        {
            foreach (var isinCode in isinCodes)
            {
                if (assetsDictionary.TryGetValue(isinCode, out var asset))
                {
                    results.Add(new AssetMonthlyPrice
                    {
                        AssetIsinCode = asset.IsinCode,
                        Year = date.Year,
                        Month = date.Month,
                        Price = asset.CurrentPrice
                    });
                }
            }
        }

        /// <summary>
        /// Creates a query for historical asset prices based on ISIN codes and a specific date.
        /// </summary>
        private QueryFutureEnumerable<AssetMonthlyPrice> CreateHistoricalPriceQuery(string[] isinCodes, DateTime date)
        {
            return _assetMonthlyPriceRepository.Query()
                .Where(a => isinCodes.Contains(a.AssetIsinCode) && a.Year == date.Year && a.Month == date.Month)
                .Future();
        }

        /// <summary>
        /// Executes batch queries asynchronously and adds the results to the main list.
        /// </summary>
        private static async Task ExecuteBatchQueriesAsync(
            List<QueryFutureEnumerable<AssetMonthlyPrice>> queries,
            List<AssetMonthlyPrice> results)
        {
            foreach (var query in queries)
            {
                var batchResults = await query.ToListAsync();
                results.AddRange(batchResults);
            }
        }

        private async Task<Dictionary<string, Asset>> GetAssetsDictionaryAsync()
        {
            var assets = await _assetService.GetAllAsync();

            var assetsDictionary = assets.Where(a => !string.IsNullOrWhiteSpace(a.Ticker) && !string.IsNullOrWhiteSpace(a.IsinCode))
                                        .ToDictionary(a => a.Ticker, a => a, StringComparer.OrdinalIgnoreCase);
            return assetsDictionary;
        }

        #endregion
    }
}
