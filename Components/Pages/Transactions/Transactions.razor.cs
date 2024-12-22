using InvestmentManager.Shared.Models;
using InvestmentManager.Shared.Models.Enums;
using InvestmentManager.Services.Interfaces;
using InvestmentManager.Utils;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using MudBlazor;

namespace InvestmentManager.Components.Pages.Transactions
{
    public partial class Transactions : ComponentBase
    {
        #region Dependencies (Injected Services)
        [Inject] private ISnackbar _snackbar { get; set; } = default!;
        [Inject] private IAssetService _assetService { get; set; } = default!;
        [Inject] private ITransactionService _transactionService { get; set; } = default!;
        [Inject] private AuthenticationStateProvider _authStateProvider { get; set; } = default!;
        #endregion

        #region Properties and Fields
        private List<Transaction> FilteredTransactions => transactions.Where(FilterTransactions).ToList();
        private string searchString = "";
        private Transaction? selectedItem1 = null;
        private List<Transaction> transactions = [];
        private bool isLoading = true;
        private Transaction? tempDeletedTransaction;
        private Timer? undoTimer;
        private const string TransactionTypeAll = "All";
        private const string TransactionTypePurchase = "Purchase";
        private const string TransactionTypeSale = "Sale";
        private string selectedTransactionType = "All";
        private string newTransactionDate = DateTime.Today.ToString("dd/MM/yyyy");
        private readonly IMask dateMask = new DateMask("dd/MM/yyyy");
        private bool isAddFormVisible = false;
        private MudForm? quickAddForm;
        private readonly string SnackBarKey = "SnackBarKey";
        private string? userId;
        private List<Asset> assets = [];
        private List<string> assetTickers = [];

        private Transaction newTransaction = InitializeNewTransaction();
        #endregion

        #region Initialization and Data Loading
        
        private async Task LoadAssetsAsync() => assets = await _assetService.GetAllAsync();
        private async Task LoadTransactionsAsync() => transactions = await _transactionService.GetUserTransactionsAsync(userId!);
        private async Task LoadAssetsTickersAsync() => assetTickers = await _assetService.GetAllAssetsTickersAsync();

        protected override async Task OnInitializedAsync()
        {
            userId = await GetUserIdAsync();

            if (string.IsNullOrEmpty(userId))
            {
                throw new InvalidOperationException("User ID is not available. Please check Identity configuration.");
            }

            await LoadDataAsync();
            isLoading = false;
        }

        private async Task LoadDataAsync()
        {
            await LoadAssetsAsync();
            await LoadAssetsTickersAsync();
            await LoadTransactionsAsync();
        }

        private async Task<string?> GetUserIdAsync()
        {
            var authState = await _authStateProvider.GetAuthenticationStateAsync();

            return authState.User.Identity?.IsAuthenticated == true ? authState.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value : null;
        }
        #endregion
        
        #region CRUD Operations

        private async Task AddTransactionAsync()
        {
            if (quickAddForm == null || !quickAddForm.IsValid) return;

            await PopulateTransactionDetailsAsync();

            await _transactionService.AddAsync(newTransaction);

            transactions.Add(newTransaction);

            _snackbar.Add($"Transação adicionada com sucesso.", Severity.Success);

            newTransaction = InitializeNewTransaction();

            ReloadTable();
        }

        private async Task PopulateTransactionDetailsAsync()
        {
            newTransaction.UserId = userId;
            newTransaction.CreateDate = DateTime.Now;
            newTransaction.TransactionDate = DateTime.Parse(newTransactionDate);
            newTransaction.Asset = await _assetService.GetByIdAsync(newTransaction.AssetIsinCode);
        }
        
        private void DeleteTransaction(object? obj)
        {
            if (obj is null || obj is not Transaction transaction)
            {
                _snackbar.Add("Invalid transaction.", Severity.Error);
                return;
            }

            if (!transactions.Remove(transaction))
            {
                _snackbar.Add("Transaction not found.", Severity.Warning);
                return;
            }

            tempDeletedTransaction = transaction;
            ReloadTable();
            ShowUndoSnackbar();
            StartUndoTimer(transaction.Id);
        }

        private void StartUndoTimer(Guid transactionId)
        {
            undoTimer?.Dispose();
            undoTimer = new Timer(async _ =>
            {
                await InvokeAsync(async () =>
                {
                    await ConfirmDeleteTransactionAsync(transactionId);
                    undoTimer?.Dispose();
                });
            }, null, 5000, Timeout.Infinite);
        }
        
        private void ShowUndoSnackbar()
        {
            _snackbar.Add("Transação excluída.", Severity.Error, options =>
            {
                options.Action = "Desfazer";
                options.RequireInteraction = false;
                options.VisibleStateDuration = 5000;

                options.Onclick = snackbar =>
                {
                    UndoDeleteTransaction();
                    _snackbar.RemoveByKey(SnackBarKey);
                    return Task.CompletedTask;
                };
            }, SnackBarKey);
        }

        private void UndoDeleteTransaction()
        {
            if (tempDeletedTransaction != null)
            {
                transactions.Add(tempDeletedTransaction);
                tempDeletedTransaction = null;
                ReloadTable();
            }
            undoTimer?.Dispose();
        }

        private async Task ConfirmDeleteTransactionAsync(Guid id)
        {
            await _transactionService.RemoveAsync(id);
            _snackbar.RemoveByKey(SnackBarKey);
            tempDeletedTransaction = null;
            undoTimer?.Dispose();
        }

        #endregion

        #region Utility Methods
        private static string GetAssetTypeDescription(AssetType assetType) => assetType.ToDescription();
        private void ToggleAddForm() => isAddFormVisible = !isAddFormVisible;
        private void ReloadTable() => StateHasChanged();

        private static Transaction InitializeNewTransaction()
        {
            return new Transaction
            {
                Id = Guid.NewGuid(),
                TransactionDate = DateTime.Today,
                UnitPrice = 1.00m,
                OtherCosts = 0.00m,
                Quantity = 1.00m,
                Asset = new Asset
                {
                    Type = AssetType.Stock
                },
                IsBuy = true
            };
        }

        private static string GetTotalValue(Transaction transaction)
        {   
            var culture = System.Globalization.CultureInfo.GetCultureInfo("pt-BR");
            var format = transaction.Asset?.Type == AssetType.Cryptocurrency && Math.Floor(transaction.TotalValue) == 0
                ? "N8"
                : "N2";
            return transaction.TotalValue.ToString(format, culture);
        }

        private static string GetQuantity(Transaction transaction)
        {
            var format = transaction.Asset?.Type == AssetType.Cryptocurrency ? "N8" : "N0";
            return transaction.Quantity.ToString(format);
        }

        private bool FilterTransactions(Transaction transaction)
        {
            return MatchesSearchString(transaction) && MatchesTransactionType(transaction);
        }

        private bool MatchesSearchString(Transaction transaction)
        {
            return string.IsNullOrWhiteSpace(searchString) ||
                   transaction.Asset?.Ticker.Contains(searchString, StringComparison.OrdinalIgnoreCase) == true ||
                   transaction.TransactionDate.ToString("dd/MM/yyyy").Contains(searchString);
        }

        private bool MatchesTransactionType(Transaction transaction)
        {
            return selectedTransactionType == TransactionTypeAll ||
                   (selectedTransactionType == TransactionTypePurchase && transaction.IsBuy) ||
                   (selectedTransactionType == TransactionTypeSale && !transaction.IsBuy);
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
        #endregion

        #region Events

        private void OnTickerValueChanged(string value)
        {
            var asset = assets.FirstOrDefault(a => a.Ticker.Equals(value, StringComparison.OrdinalIgnoreCase));
            if (asset != null)
            {
                newTransaction.Asset = new() { Type = asset.Type };
                newTransaction.AssetIsinCode = asset.IsinCode;
            }
        }

        #endregion
    }
}