using FluentValidation.Results;
using InvestmentManager.Services.Interfaces;
using InvestmentManager.Shared.Models;
using InvestmentManager.Shared.Models.Enums;
using InvestmentManager.Shared.Models.Inputs;
using InvestmentManager.Shared.Validators;
using InvestmentManager.Utils;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using MudBlazor;
using System.Text;
using System.Text.RegularExpressions;

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
		private TransactionInputs TransactionInputs { get; set; } = new();

		private async Task LoadAssetsAsync() => assets = await _assetService.GetAllAsync();
		private async Task LoadAssetsTickersAsync() => assetTickers = await _assetService.GetAllAssetsTickersAsync();
		private static string GetAssetTypeDescription(AssetType assetType) => assetType.ToDescription();
		private bool isFractional;

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
		private TransactionInput InitializeNewTransactionInput()
		{
			string quantity = SelectedAsset?.Type == AssetType.Stock ? "1" : "1,00000000";

            return new TransactionInput
			{
				TransactionDate = DateTime.Today,
				UnitPrice = "1,00",
				OtherCosts = "0,00",
				Quantity = quantity.ToString(),
				IsBuy = true,
				IsFractional = isFractional
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
			isFractional = SelectedAsset?.Type != AssetType.Stock;
			TransactionInputs.Inputs = [InitializeNewTransactionInput()];
			
        }
		private void AddAsset()
		{

            TransactionInputs.Inputs.Add(InitializeNewTransactionInput());
			StateHasChanged();
		}

		private void RemoveAsset(TransactionInput transaction)
		{
            TransactionInputs.Inputs.Remove(transaction);
            StateHasChanged();
        }

		private async Task SubmitAndAddAnotherAsync()
        {
			var validator = new TransactionInputsValidator();
			var validationResult = validator.Validate(TransactionInputs);

			if (validationResult.IsValid)
			{
                await AddTransactionsAsync();
                string transactionWord = TransactionInputs.Inputs.Count > 1 ? "Transações adicionadas" : "Transação adicionada";
                ClearDialog();
                StateHasChanged();
                _snackbar.Add($"{transactionWord} com sucesso.", Severity.Success);
            }
			else
            {
                _snackbar.Configuration.PositionClass = Defaults.Classes.Position.TopCenter;
                string errorsMessages = BuildErrorsMessages(validationResult);

                _snackbar.Add(new MarkupString(errorsMessages), Severity.Error);
            }

        }

        private static string BuildErrorsMessages(ValidationResult validationResult)
        {
            StringBuilder message = new();

            var groupedErrors = validationResult.Errors
                .GroupBy(error => GetInputIndex(error))
                .OrderBy(group => group.Key);

            foreach (var group in groupedErrors)
            {
                message.AppendLine($"Linha {group.Key}:<br>");
                foreach (var error in group)
                {
                    message.AppendLine($"- {error.ErrorMessage}<br>");
                }
				message.AppendLine("<br>");
            }

            return message.ToString();
        }

        private static int GetInputIndex(ValidationFailure error)
        {
            if (error.PropertyName.Contains("Inputs["))
            {
                var match = Regex.Match(error.PropertyName, @"Inputs\[(\d+)\]");
                if (match.Success && int.TryParse(match.Groups[1].Value, out var index))
                {
                    return index + 1;
                }
            }
            return 0; 
        }


        private void ClearDialog()
        {
            SelectedAssetText = string.Empty;
            TransactionInputs.Inputs = [];
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
            if (TransactionInputs.Inputs.Count > 0)
            {
				var transactions = ConvertToTransaction(TransactionInputs);
                await _transactionService.AddRangeAsync(transactions);
				await _transactionService.ClearCacheAsync(userId!);
            }
        }

		private List<Transaction> ConvertToTransaction(TransactionInputs inputs)
		{
            return inputs.Inputs.Select(input => new Transaction
			{
				Id = Guid.NewGuid(),
				UserId = userId,
				IsBuy = input.IsBuy,
				TransactionDate = input.TransactionDate,
				Quantity = Convert.ToDecimal(input.Quantity),
				UnitPrice = Convert.ToDecimal(input.UnitPrice),
				OtherCosts = Convert.ToDecimal(input.OtherCosts),
				CreateDate = DateTime.Now,
				AssetIsinCode = SelectedAsset?.IsinCode
			}).ToList();
		}

    }
}
