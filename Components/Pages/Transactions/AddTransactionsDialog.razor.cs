using InvestmentManager.Services.Interfaces;
using InvestmentManager.Shared.Models;
using InvestmentManager.Shared.Models.DTOs;
using InvestmentManager.Shared.Models.Enums;
using InvestmentManager.Shared.Validators;
using InvestmentManager.Utils;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using MudBlazor;
using System.Text;

namespace InvestmentManager.Components.Pages.Transactions
{
    public partial class AddTransactionsDialog : ComponentBase
    {
        #region Dependencies (Injected Services)
        [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; } = default!;
        [Inject] private IAssetService AssetService { get; set; } = default!;
        [Inject] private ITransactionService TransactionService { get; set; } = default!;
        [Inject] private ISnackbar Snackbar { get; set; } = default!;
        #endregion

        #region Properties
        protected List<TransactionDto> Transactions { get; set; } = [];
        protected string SelectedAssetText { get; set; } = string.Empty;
        protected Asset? SelectedAsset { get; set; } = default!;
        protected List<string> AssetTickers { get; set; } = [];
        #endregion

        #region Fields
        private MudDialogInstance _mudDialog = default!;
        private string? _userId = string.Empty;
        private List<Asset> _assets = [];
        #endregion

        #region Protected Methods
        protected void Cancel() => _mudDialog.Cancel();

        protected override async Task OnInitializedAsync()
        {
            _userId = await GetUserIdAsync();

            if (string.IsNullOrEmpty(_userId))
            {
                throw new InvalidOperationException("User ID is not available. Please check Identity configuration.");
            }

            await LoadDataAsync();

        }

        protected async Task SubmitAsync()
        {
            bool success = await ProcessTransactionsAsync();

            if (success)
            {
                _mudDialog.Close();
            }
        }

        protected async Task SubmitAndAddAnotherAsync()
        {
            await ProcessTransactionsAsync();
        }

        protected static bool SearchItems(string value, string searchString)
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

        protected void AddAsset()
        {
            Transactions.Add(InitializeNewTransactionDto());
            StateHasChanged();
        }

        protected void RemoveAsset(TransactionDto transaction)
        {
            Transactions.Remove(transaction);
            StateHasChanged();
        }
        #endregion

        #region Private Methods
        private async Task LoadAssetsAsync() => _assets = await AssetService.GetAllAsync();
        private async Task LoadAssetsTickersAsync() => AssetTickers = await AssetService.GetAllAssetsTickersAsync();
        private static string GetAssetTypeDescription(AssetType assetType) => assetType.ToDescription();
        private bool IsTransactionListEmpty() => Transactions.Count == 0;

        private async Task LoadDataAsync()
        {
            await LoadAssetsAsync();
            await LoadAssetsTickersAsync();
        }

        private async Task<string?> GetUserIdAsync()
        {
            var authState = await AuthStateProvider.GetAuthenticationStateAsync();

            return authState.User.Identity?.IsAuthenticated == true ? authState.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value : null;
        }

        private TransactionDto InitializeNewTransactionDto()
        {
            decimal quantity = SelectedAsset?.Type == AssetType.Stock ? 1m : 1.00000000m;

            return new TransactionDto
            {
                IsBuy = true,
                TransactionDate = DateTime.Today,
                UnitPrice = 1.00m,
                OtherCosts = 0.00m,
                Quantity = quantity,
                AssetIsinCode = SelectedAsset?.IsinCode ?? string.Empty,
                Asset = SelectedAsset ?? new Asset(),
            };
        }

        private void OnTickerValueChanged(string value)
        {
            SelectedAssetText = value;
            SelectedAsset = _assets.Find(a => a.Ticker.Equals(SelectedAssetText, StringComparison.OrdinalIgnoreCase));
            Transactions = [InitializeNewTransactionDto()];
        }

        private async Task<bool> ProcessTransactionsAsync()
        {
            if (IsTransactionListEmpty())
            {
                ShowAlert(new MarkupString("Adicione uma transação."), Severity.Error);
                return false;
            }

            var (isValid, errorMessage) = await TryValidateTransactionsAsync();

            if (!isValid)
            {
                ShowAlert(new MarkupString(errorMessage), Severity.Error);
                return false;
            }

            await AddTransactionsAsync();

            string successMessage = (Transactions.Count > 1 ? "Transações adicionadas" : "Transação adicionada") + " com sucesso.";

            ClearDialog();

            ShowAlert(new MarkupString(successMessage), Severity.Success);

            StateHasChanged();

            return true;
        }
        private async Task<(bool IsValid, string ErrorMessage)> TryValidateTransactionsAsync()
        {
            string errorMessage = await ValidateTransactionsAsync();
            return (string.IsNullOrEmpty(errorMessage), errorMessage);
        }

        private void ShowAlert(MarkupString message, Severity severity)
        {
            Snackbar.Add(message, severity);
        }

        private async Task<string> ValidateTransactionsAsync()
        {
            string errorMessage = string.Empty;
            var validator = new TransactionDtoValidator();

            for (int i = 0; i < Transactions.Count; i++)
            {
                var validationResult = await validator.ValidateAsync(Transactions[i]);
                if (!validationResult.IsValid)
                {
                    errorMessage += ExtractErrorsMessages(i + 1, validationResult);
                }
            }

            return errorMessage;
        }

        private string ExtractErrorsMessages(int rowNumber, FluentValidation.Results.ValidationResult validationResult)
        {
            StringBuilder errorMessage = new();

            errorMessage.AppendLine($"Linha {rowNumber}:<br>");

            foreach (var error in validationResult.Errors)
            {
                errorMessage.AppendLine($"- {error.ErrorMessage}<br>");
            }

            if (rowNumber < Transactions.Count)
            {
                errorMessage.AppendLine("<br>");
            }

            return errorMessage.ToString();
        }

        private void ClearDialog()
        {
            SelectedAssetText = string.Empty;
            SelectedAsset = null;
            Transactions = [];
        }

        private async Task AddTransactionsAsync()
        {
            if (Transactions.Count > 0)
            {
                var createDate = DateTime.Now;

                await TransactionService.AddRangeAsync(Transactions.Select(t => new Transaction
                {
                    Id = Guid.NewGuid(),
                    UserId = _userId!,
                    CreateDate = createDate,
                    IsBuy = t.IsBuy,
                    TransactionDate = t.TransactionDate,
                    UnitPrice = t.UnitPrice,
                    OtherCosts = t.OtherCosts,
                    Quantity = t.Quantity,
                    AssetIsinCode = SelectedAsset?.IsinCode
                }).ToList());

                await TransactionService.ClearCacheAsync(_userId!);
            }
        }
        #endregion
    }
}
