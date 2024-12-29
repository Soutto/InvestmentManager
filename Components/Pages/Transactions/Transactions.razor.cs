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
        [Inject] private ISnackbar Snackbar { get; set; } = default!;
        [Inject] private ITransactionService TransactionService { get; set; } = default!;
        [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; } = default!;
        [Inject] private IDialogService DialogService { get; set; } = default!;
        #endregion

        #region Properties    
        protected List<Transaction> FilteredTransactions
        {
            get
            {
                _filteredTransactions = _transactions.Where(FilterTransactions).ToList();
                return _filteredTransactions;
            }
            set
            {
                _filteredTransactions = value;
            }
        }
        protected bool IsLoading { get; set; } = true;
        protected Transaction? SelectedItem1 { get; set; } = null;
        protected string TransactionTypeAll { get; } = "All";
        protected string TransactionTypePurchase { get; } = "Purchase";
        protected string TransactionTypeSale { get; } = "Sale" ;
        protected string SearchString { get; set; } = "";
        #endregion

        #region Fields
        private List<Transaction> _filteredTransactions = [];
        private List<Transaction> _transactions = [];
        private Transaction? _tempDeletedTransaction;
        private Timer? _undoTimer;
        private string _selectedTransactionType = "All";
        private readonly string _snackBarKey = "SnackBarKey";
        private string? _userId;
        #endregion

        #region Protected Methods
        protected static string GetAssetTypeDescription(AssetType assetType) => assetType.ToDescription();

        protected override async Task OnInitializedAsync()
        {
            _userId = await GetUserIdAsync();

            if (string.IsNullOrEmpty(_userId))
            {
                throw new InvalidOperationException("User ID is not available. Please check Identity configuration.");
            }

            await LoadTransactionsAsync();
            IsLoading = false;
        }

        protected void DeleteTransaction(object? obj)
        {
            if (obj is null || obj is not Transaction transaction)
            {
                Snackbar.Add("Invalid transaction.", Severity.Error);
                return;
            }

            if (!_transactions.Remove(transaction))
            {
                Snackbar.Add("Transaction not found.", Severity.Warning);
                return;
            }

            _tempDeletedTransaction = transaction;
            StateHasChanged();
            ShowUndoSnackbar();
            StartUndoTimer(transaction.Id);
        }

        protected static string GetTotalValue(Transaction transaction)
        {
            var culture = System.Globalization.CultureInfo.GetCultureInfo("pt-BR");
            var format = transaction.Asset?.Type == AssetType.Cryptocurrency
                ? "N8"
                : "N2";
            return transaction.TotalValue.ToString(format, culture);
        }

        protected static string GetQuantity(Transaction transaction)
        {
            var format = transaction.Asset?.Type == AssetType.Cryptocurrency ? "N8" : "N0";
            return transaction.Quantity.ToString(format);
        }

        protected bool FilterTransactions(Transaction transaction)
        {
            return MatchesSearchString(transaction) && MatchesTransactionType(transaction);
        }

        protected async Task OpenAddTransactionsDialogAsync()
        {

            var options = new DialogOptions
            {
                BackgroundClass = "add-transactions-dialog-class",
                MaxWidth = MaxWidth.Large,
                FullWidth = true
            };

            var dialog = await DialogService.ShowAsync<AddTransactionsDialog>("Adicionar transações", options);

            var result = await dialog.Result;

            if (result is null) return;

            await LoadTransactionsAsync();
            StateHasChanged();
        }
        #endregion

        #region Private Methods
        private async Task LoadTransactionsAsync() => _transactions = await TransactionService.GetUserTransactionsAsync(_userId!);

        private async Task<string?> GetUserIdAsync()
        {
            var authState = await AuthStateProvider.GetAuthenticationStateAsync();

            return authState.User.Identity?.IsAuthenticated == true ? authState.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value : null;
        }
        private void StartUndoTimer(Guid transactionId)
        {
            _undoTimer?.Dispose();
            _undoTimer = new Timer(async _ =>
            {
                await InvokeAsync(async () =>
                {
                    await ConfirmDeleteTransactionAsync(transactionId);
                    _undoTimer?.Dispose();
                });
            }, null, 5000, Timeout.Infinite);
        }

        private void ShowUndoSnackbar()
        {
            Snackbar.Add("Transação excluída.", Severity.Error, options =>
            {
                options.Action = "Desfazer";
                options.RequireInteraction = false;
                options.VisibleStateDuration = 5000;

                options.Onclick = snackbar =>
                {
                    UndoDeleteTransaction();
                    Snackbar.RemoveByKey(_snackBarKey);
                    return Task.CompletedTask;
                };
            }, _snackBarKey);
        }

        private void UndoDeleteTransaction()
        {
            if (_tempDeletedTransaction != null)
            {
                _transactions.Add(_tempDeletedTransaction);
                _tempDeletedTransaction = null;
                StateHasChanged();
            }
            _undoTimer?.Dispose();
        }

        private async Task ConfirmDeleteTransactionAsync(Guid id)
        {
            await TransactionService.RemoveAsync(id);
            Snackbar.RemoveByKey(_snackBarKey);
            _tempDeletedTransaction = null;
            _undoTimer?.Dispose();
        }
        private bool MatchesSearchString(Transaction transaction)
        {
            return string.IsNullOrWhiteSpace(SearchString) ||
                   transaction.Asset?.Ticker.Contains(SearchString, StringComparison.OrdinalIgnoreCase) == true ||
                   transaction.TransactionDate.GetValueOrDefault().ToString("dd/MM/yyyy").Contains(SearchString);
        }

        private bool MatchesTransactionType(Transaction transaction)
        {
            return _selectedTransactionType == TransactionTypeAll ||
                   (_selectedTransactionType == TransactionTypePurchase && transaction.IsBuy) ||
                   (_selectedTransactionType == TransactionTypeSale && !transaction.IsBuy);
        }
        #endregion
    }
}