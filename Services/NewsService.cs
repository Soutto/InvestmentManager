using DadosDeMercadoClient.Interfaces;
using InvestmentManager.Services.Interfaces;
using InvestmentManager.Shared.Factories;
using InvestmentManager.Shared.Models.Application;

namespace InvestmentManager.Services
{
    public class NewsService(INewsClient newsClient, ILogger<AssetService> logger, ICacheService cacheService) : INewsService
    {
        private readonly INewsClient _newsClient = newsClient;
        private readonly ILogger<AssetService> _logger = logger;
        private readonly ICacheService _cacheService = cacheService;

        public async Task<IEnumerable<News>> GetAllAsync()
        {
            try
            {
                var news = await _cacheService.GetOrSetAsync(key: CacheKeysFactory.News(),
                                                             getFromSource: GetAllFromClientAsync,
                                                             _cacheService.DefaultExpiration());

                if (news is null || !news.Any())
                {
                    _logger.LogError("News not found");
                    return [];
                }

                return news;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving assets");
                throw;
            }
        }

        private async Task<IEnumerable<News>> GetAllFromClientAsync()
        {
            try
            {
                var news = await _newsClient.GetAllAsync();

                return news ?? [];
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving news from client");
                throw;
            }
        }
    }
}
