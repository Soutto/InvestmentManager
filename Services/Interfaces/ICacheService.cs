namespace InvestmentManager.Services.Interfaces
{
    public interface ICacheService
    {
        Task<T?> GetOrSetAsync<T>(string key, Func<Task<T>> getFromSource, TimeSpan? expiration = null);
        Task RefreshKeyAsync(string key, TimeSpan slidingExpiration);
        Task KeyDeleteAsync(string key);
        Task DeleteAllKeysAsync();
        TimeSpan DefaultExpiration();
    }
}
