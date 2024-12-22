﻿using InvestmentManager.Services.Interfaces;
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

        private MonthlyHeritageEvolution? heritageEvolution = new([]);
        private Shared.Models.Application.Portfolio? portfolio = new();

        public double[]? Series = [];
        public string[]? XAxisLabels = [];

        List<DataItem> dataItems = [];
        class DataItem
        {
            public string Date { get; set; }
            public decimal Revenue { get; set; }
        }

        private async Task LoadPortfolioAsync(string userId) => portfolio = await _transactionService.GetPortfolioAsync(userId);
        private async Task LoadHeritageEvolutionAsync(string userId) => heritageEvolution = await _transactionService.GetMonthlyHeritageEvolutionAsync(userId, 12);

        protected override async Task OnInitializedAsync()
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            string? userId = await GetUserIdAsync();

            if (string.IsNullOrEmpty(userId))
            {
                throw new InvalidOperationException("User ID is not available. Please check Identity configuration.");
            }

            await LoadDataAsync(userId);
            stopwatch.Stop();
            Console.WriteLine($"Portfolio page loaded in {stopwatch.ElapsedMilliseconds} ms.");
        }

        private async Task LoadDataAsync(string userId)
        {
            await LoadPortfolioAsync(userId);
            await LoadHeritageEvolutionAsync(userId);


            foreach (var record in heritageEvolution.Records)
            {
                dataItems.Add(new DataItem { Date = record.MonthEnd.ToString("MM/yy"), Revenue = record.TotalHeritage });
            }
            
        }

        private async Task<string?> GetUserIdAsync()
        {
            var authState = await _authStateProvider.GetAuthenticationStateAsync();

            return authState.User.Identity?.IsAuthenticated == true ? authState.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value : null;
        }

        private static string FormatAsUSD(object value)
        {
            return ((double)value).ToString("C0", CultureInfo.CreateSpecificCulture("pt-BR"));
        }
    }
}
