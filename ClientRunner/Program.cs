using System;
using Client;
namespace InvestmentManager.ClientRunner
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            // Initialize your client and invoke methods
            var client = new AssetClient(new HttpClient());
            
            try
            {
                var result = await client.GetDataAsync();
                Console.WriteLine($"Data received: {result}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }
}

