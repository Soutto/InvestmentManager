using ApexCharts;
using InvestmentManager.Services.Interfaces;
using InvestmentManager.Shared.Models.Application;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Options;
using MudBlazor;
using System.Diagnostics;
using System.Globalization;
using static MudBlazor.Colors;

namespace InvestmentManager.Components.Pages.Portfolio
{
	public partial class Portfolio : ComponentBase
	{
		#region Dependencies (Injected Services)
		[Inject] private ITransactionService TransactionService { get; set; } = default!;
		[Inject] private AuthenticationStateProvider AuthStateProvider { get; set; } = default!;
		#endregion

		#region Public Properties
		public bool IsDataLoaded { get; private set; }
		public decimal PortfolioValue { get; private set; } = 158452.85m;
		public decimal TotalInvested { get; private set; } = 140000.00m;
		public decimal ProfitLoss => PortfolioValue - TotalInvested;
		public decimal ProfitLossPercentage => TotalInvested != 0 ? ProfitLoss / TotalInvested : 0;
		#endregion

		#region Private Fields
		private string _currentMonthsText = "3 meses";
		private int _currentMonths = 3;
		private string? _userId = string.Empty;
		private MonthlyHeritageEvolution? _heritageEvolution = new([]);
		private MonthlyInvestmentEvolution? _investmentEvolution = new([]);
		private Shared.Models.Application.Portfolio _portfolio = new();
		private readonly List<DataItem> _heritageDataItems = [];
		private readonly List<DataItem> _investmentDataItems = [];
		private readonly ApexChartOptions<DataItem> _chartOptions = new();
		private ApexChart<DataItem> _chart = new();
		#endregion

		#region Inner Classes
		private class DataItem
		{
			public string Date { get; set; } = string.Empty;
			public decimal Revenue { get; set; }
		}
		#endregion

		#region Lifecycle Methods
		protected override async Task OnInitializedAsync()
		{
			var stopwatch = Stopwatch.StartNew();

			_userId = await GetUserIdAsync();
			if (string.IsNullOrEmpty(_userId))
			{
				throw new InvalidOperationException("User ID is not available. Please check Identity configuration.");
			}

			ConfigureChartOptions();
			await LoadInitialDataAsync();

			stopwatch.Stop();
			Console.WriteLine($"Portfolio page loaded in {stopwatch.ElapsedMilliseconds} ms.");
			IsDataLoaded = true;
		}
		#endregion

		#region Public Methods
		public string GetButtonClass(string period) =>
			_currentMonthsText.Equals(period, StringComparison.OrdinalIgnoreCase) ? "selected-button" : "not-selected-button";

		public string GetProfitLossColor(decimal value) =>
			value >= 0 ? "color: green;" : "color: red;";
		#endregion

		#region Private Methods
		private void ConfigureChartOptions()
		{
			_chartOptions.Chart.Width = "100%";
			_chartOptions.Chart.Height = "100%";
			_chartOptions.Chart.Zoom = new Zoom { Enabled = false };
			_chartOptions.Yaxis =
			[
				new YAxis
				{
					Labels = new YAxisLabels
					{
						Formatter = @"function (value) { return 'R$ ' + new Intl.NumberFormat('pt-BR', { minimumFractionDigits: 2, maximumFractionDigits: 2 }).format(value);}"
					}
				}
,
			];
		}

		private async Task LoadInitialDataAsync()
		{
			await LoadPortfolioAsync();
			await LoadHeritageEvolutionAsync();
			await LoadInvestmentEvolutionAsync();
			PrepareChartData();
		}

		private async Task LoadPortfolioAsync()
		{
			_portfolio = await TransactionService.GetPortfolioAsync(_userId);
		}

		private async Task LoadHeritageEvolutionAsync() =>
			_heritageEvolution = await TransactionService.GetMonthlyHeritageEvolutionAsync(_userId, _currentMonths);

		private async Task LoadInvestmentEvolutionAsync() =>
			_investmentEvolution = await TransactionService.GetMonthlyInvestmentEvolutionAsync(_userId, _currentMonths);

		private void PrepareChartData()
		{
			_heritageDataItems.Clear();
			_investmentDataItems.Clear();

			if (_heritageEvolution?.Records != null && _heritageEvolution?.Records.Count > 0)
			{
				_heritageDataItems.AddRange(_heritageEvolution.Records.Select(record =>
					new DataItem
					{
						Date = record.MonthEnd.ToString("MM/yy"),
						Revenue = record.TotalHeritage
					}));
			}

			if (_investmentEvolution?.Investments != null && _investmentEvolution?.Investments.Count > 0)
			{
				_investmentDataItems.AddRange(_investmentEvolution.Investments.Select(investment =>
					new DataItem
					{
						Date = investment.MonthEnd.ToString("MM/yy"),
						Revenue = investment.TotalInvestment
					}));
			}
		}

		private async Task LoadChartDataAsync(string period)
		{
			int months = GetMonthsPeriod(period);

			if (months != _currentMonths)
			{
				_currentMonths = months;
				await LoadHeritageEvolutionAsync();
				await LoadInvestmentEvolutionAsync();
				PrepareChartData();
			}

			_currentMonthsText = period;
			await _chart.RenderAsync();
		}

		private static int GetMonthsPeriod(string period)
		{
			return period switch
			{
				_ when string.Equals(period, "3 meses", StringComparison.OrdinalIgnoreCase) => 3,
				_ when string.Equals(period, "6 meses", StringComparison.OrdinalIgnoreCase) => 6,
				_ when string.Equals(period, "YTD", StringComparison.OrdinalIgnoreCase) => GetMonthsSinceStartOfYear(),
				_ when string.Equals(period, "1 ano", StringComparison.OrdinalIgnoreCase) => 12,
				_ when string.Equals(period, "5 anos", StringComparison.OrdinalIgnoreCase) => 60,
				_ when string.Equals(period, "MÁX", StringComparison.OrdinalIgnoreCase) => 360,
				_ => 3
			};
		}

		private async Task<string?> GetUserIdAsync()
		{
			var authState = await AuthStateProvider.GetAuthenticationStateAsync();
			var user = authState.User;

			return user.Identity?.IsAuthenticated == true
				? user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
				: null;
		}
		private static int GetMonthsSinceStartOfYear()
		{
			var currentDate = DateTime.Now;
			return currentDate.Month;
		}
		#endregion
	}
}