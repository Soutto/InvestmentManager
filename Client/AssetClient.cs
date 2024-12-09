using System.Text.Json;

namespace Client
{
    public class AssetClient
    {
        private readonly HttpClient _httpClient;

        public AssetClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<string> GetDataAsync()
        {
            var response = await _httpClient.GetAsync("https://localhost:5098/api/asset/getall");
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<string>(content);
        }
    }
}