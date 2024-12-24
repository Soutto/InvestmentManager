using ApexCharts;
using InvestmentManager.Services.Interfaces;
using InvestmentManager.Shared.Models.Application;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using MudBlazor;
using System.Diagnostics;
using System.Globalization;

namespace InvestmentManager.Components.Pages.Portfolio
{
    public partial class Portfolio : ComponentBase
    {
        #region Dependencies (Injected Services)
        [Inject] private ITransactionService _transactionService { get; set; } = default!;
        [Inject] private AuthenticationStateProvider _authStateProvider { get; set; } = default!;
        #endregion
        public bool IsDataLoaded { get; set; } = false;
        public decimal PortfolioValue { get; set; } = 158452.85m;
        public decimal TotalInvested { get; set; } = 140000.00m;
        public decimal ProfitLoss => PortfolioValue - TotalInvested;
        public decimal ProfitLossPercentage => (TotalInvested != 0) ? (ProfitLoss / TotalInvested) : 0;

        public double[] allocationData = [];
        public string[] allocationLabels = [];

        // Reference to the Pie Chart
        MudChart pieChart = new();

        private MonthlyHeritageEvolution? heritageEvolution = new([]);
        private MonthlyInvestmentEvolution? investmentEvolution = new([]);
        private Shared.Models.Application.Portfolio? portfolio = new();
        private ApexChartOptions<DataItem> ChartOptions = new();
        private ApexChart<DataItem> chart = new();
        private int currentMonths = 3; // Default to 1 month
        public double[]? Series = [];
        public string[]? XAxisLabels = [];
        string UserId = string.Empty;
        List<DataItem> dataItems = [];
        List<DataItem> dataItems2 = [];
        private string selectedPeriod = "3m";

        class DataItem
        {
            public string Date { get; set; }
            public decimal Revenue { get; set; }
        }

        private async Task LoadPortfolioAsync(string userId) => portfolio = await _transactionService.GetPortfolioAsync(userId);
        private async Task LoadHeritageEvolutionAsync(string userId, int months) => heritageEvolution = await _transactionService.GetMonthlyHeritageEvolutionAsync(userId, months);
        private async Task LoadInvestmentsEvolutionAsync(string userId, int months) => investmentEvolution = await _transactionService.GetMonthlyInvestmentEvolutionAsync(userId, months);
        protected override async Task OnInitializedAsync()
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            UserId = await GetUserIdAsync();

            if (string.IsNullOrEmpty(UserId))
            {
                throw new InvalidOperationException("User ID is not available. Please check Identity configuration.");
            }
            ChartOptions.Chart.Width = "100%";
            ChartOptions.Chart.Height = "100%";

            await LoadDataAsync(UserId);
            stopwatch.Stop();
            Console.WriteLine($"Portfolio page loaded in {stopwatch.ElapsedMilliseconds} ms.");
            IsDataLoaded = true;
        }
        private string GetButtonClass(int months)
        {
            return currentMonths == months ? "selected-button" : "not-selected-button";
        }
        private async Task LoadChartDataAsync(int months)
        {
            currentMonths = months;
            await LoadHeritageEvolutionAsync(UserId, currentMonths);
            await LoadInvestmentsEvolutionAsync(UserId, currentMonths);
            dataItems.Clear();
            dataItems2.Clear();
            foreach (var record in heritageEvolution.Records)
            {
                dataItems.Add(new DataItem { Date = record.MonthEnd.ToString("MM/yy"), Revenue = record.TotalHeritage });
            }
            foreach (var investment in investmentEvolution.Investments)
            {
                dataItems2.Add(new DataItem { Date = investment.MonthEnd.ToString("MM/yy"), Revenue = investment.TotalInvestment });
            }
            await chart.RenderAsync();
        }
        private async Task LoadDataAsync(string userId)
        {
            
            await LoadPortfolioAsync(userId);
            await LoadHeritageEvolutionAsync(userId, currentMonths);
            await LoadInvestmentsEvolutionAsync(userId, currentMonths);
            allocationData = portfolio.PortfolioAssets.Select(a => (double)a.CurrentTotalValue).ToArray();
            allocationLabels = portfolio.PortfolioAssets.Select(a => a.Asset.Ticker).ToArray();
            foreach (var record in heritageEvolution.Records)
            {
                dataItems.Add(new DataItem { Date = record.MonthEnd.ToString("MM/yy"), Revenue = record.TotalHeritage });
            }
            foreach (var investment in investmentEvolution.Investments)
            {
                dataItems2.Add(new DataItem { Date = investment.MonthEnd.ToString("MM/yy"), Revenue = investment.TotalInvestment });
            }
        }
        private string GetProfitLossColor(decimal value)
        {
            return value >= 0 ? "color: green;" : "color: red;";
        }
        private async Task<string?> GetUserIdAsync()
        {
            var authState = await _authStateProvider.GetAuthenticationStateAsync();

            return authState.User.Identity?.IsAuthenticated == true ? authState.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value : null;
        }
    }
}
