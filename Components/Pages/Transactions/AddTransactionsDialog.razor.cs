using InvestmentManager.Services.Interfaces;
using InvestmentManager.Shared.Models;
using InvestmentManager.Shared.Models.Enums;
using InvestmentManager.Utils;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using MudBlazor;

namespace InvestmentManager.Components.Pages.Transactions
{
    public partial class AddTransactionsDialog : ComponentBase
    {
		[Inject] private AuthenticationStateProvider _authStateProvider { get; set; } = default!;
		[Inject] private IAssetService _assetService { get; set; } = default!;
		[Inject] private ITransactionService _transactionService { get; set; } = default!;
        [Inject] private ISnackbar _snackbar { get; set; } = default!;

        [CascadingParameter]
        private MudDialogInstance MudDialog { get; set; } = default!;
		private string? userId = string.Empty;
		private List<Asset> assets = [];

		[Parameter]
		public List<string> assetTickers { get; set; } = [];

		private string SelectedAssetText { get; set; } = string.Empty;
		private Asset? SelectedAsset { get; set; } = default!;
		private List<Transaction> Transactions { get; set; } = [];

		private async Task LoadAssetsAsync() => assets = await _assetService.GetAllAsync();
		private async Task LoadAssetsTickersAsync() => assetTickers = await _assetService.GetAllAssetsTickersAsync();
		private static string GetAssetTypeDescription(AssetType assetType) => assetType.ToDescription();
		private string quantityFormat = "N0";


        protected override async Task OnInitializedAsync()
		{
			userId = await GetUserIdAsync();

			if (string.IsNullOrEmpty(userId))
			{
				throw new InvalidOperationException("User ID is not available. Please check Identity configuration.");
			}

			await LoadDataAsync();
		
		}
		private async Task LoadDataAsync()
		{
			await LoadAssetsAsync();
			await LoadAssetsTickersAsync();
		}
		private async Task<string?> GetUserIdAsync()
		{
			var authState = await _authStateProvider.GetAuthenticationStateAsync();

			return authState.User.Identity?.IsAuthenticated == true ? authState.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value : null;
		}
		private Transaction InitializeNewTransaction()
		{
			decimal quantity = SelectedAsset?.Type == AssetType.Stock ? 1m : 1.00000000m;

            return new Transaction
			{
				Id = Guid.NewGuid(),
				TransactionDate = DateTime.Today,
				UnitPrice = 1.00m,
				OtherCosts = 0.00m,
				Quantity =  quantity,
				IsBuy = true,
				AssetIsinCode = SelectedAsset?.IsinCode,
				CreateDate = DateTime.Now,
				UserId = userId
			};
        }
		private static bool SearchItems(string value, string searchString)
		{
			if (searchString == "")
			{
				return true;
			}

			if (value.StartsWith(searchString, StringComparison.CurrentCultureIgnoreCase))
			{
				return true;
			}

			return false;
		}
		private void OnTickerValueChanged(string value)
		{
			SelectedAssetText = value;
            SelectedAsset = assets.Find(a => a.Ticker.Equals(SelectedAssetText, StringComparison.OrdinalIgnoreCase));
			quantityFormat = SelectedAsset.Type == AssetType.Stock ? "N0" : "N8";

			Transactions = [InitializeNewTransaction()];
			
        }
		private void AddAsset()
		{

			Transactions.Add(InitializeNewTransaction());
			StateHasChanged();
		}

		private void RemoveAsset(Transaction transaction)
		{
			Transactions.Remove(transaction);
            StateHasChanged();
        }

		private async Task SubmitAndAddAnotherAsync()
        {
            await AddTransactionsAsync();
			string transactionWord = Transactions.Count > 1 ? "Transações adicionadas" : "Transação adicionada";
            ClearDialog();
            StateHasChanged();
            _snackbar.Add($"{transactionWord} com sucesso.", Severity.Success);
        }

        private void ClearDialog()
        {
            SelectedAssetText = string.Empty;
            Transactions = [];
        }

        private async Task SubmitAsync()
		{
			await AddTransactionsAsync();
            _snackbar.Add($"Transações adicionadas com sucesso.", Severity.Success);
            MudDialog.Close();
        }

        private void Cancel() => MudDialog.Cancel();

		private async Task AddTransactionsAsync()
		{
            if (Transactions.Count > 0)
            {
                await _transactionService.AddRangeAsync(Transactions);
				await _transactionService.ClearCacheAsync(userId!);
            }
        }
    }
}
