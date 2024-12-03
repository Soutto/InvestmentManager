using System.ComponentModel.DataAnnotations;
using System.Reflection;
using InvestmentManager.Models;
using InvestmentManager.Models.Enums;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace InvestmentManager.Components.Pages.Transactions
{
    public partial class Transactions : ComponentBase
    {
        private List<string> editEvents = new();
        private string searchString = "";
        private Transaction? selectedItem1 = null;
        private Transaction? elementBeforeEdit;
        private List<Transaction> transactionList = new List<Transaction>();
        private bool isLoading = true;
        private Transaction? _tempDeletedTransaction;
        private Timer? _undoTimer;
        private const string All = "All";
        private const string Purchase = "Purchase";
        private const string Sale = "Sale";
        private string selectedTransactionType = "All";
        private List<Transaction> filteredTransactions => transactionList.Where(FilterFunc).ToList();
        // Nova transação para o formulário
        private string newTransactionDate = DateTime.Today.ToString("dd/MM/yyyy");
        IMask dateMask = new DateMask("dd/MM/yyyy");
        private bool isAddFormVisible = false;
        private MudForm? quickAddForm;
        private readonly string SnackBarKey = "SnackBarKey";
        private string? userId;
        private List<Asset> assets = new List<Asset>();
        private List<string> assets2 = new List<string>();

        protected override async Task OnInitializedAsync()
        {
            userId = await GetUserIdAsync();
            if (!string.IsNullOrEmpty(userId))
            {
                assets = await _assetService.GetAllAsync();
                assets2 = await _assetService.GetAllTickersTextAsync();
                transactionList = await _transactionService.GetAllByUserIdAsync(userId);
            }
            isLoading = false;
        }
        private string GetTotalValue(Transaction transaction)
        {
            var culture = System.Globalization.CultureInfo.GetCultureInfo("pt-BR");

            if (transaction.Asset.Type == AssetType.Cryptocurrency)
            {
                // Check if the number on the left side of the decimal is 0
                if (Math.Floor(transaction.TotalValue) == 0)
                {
                    return transaction.TotalValue.ToString("N8", culture);
                }
                else
                {
                    return transaction.TotalValue.ToString("N2", culture);
                }
            }
            else
            {
                return transaction.TotalValue.ToString("N2", culture);
            }
        }

        private Transaction newTransaction = new Transaction
        {
            TransactionDate = DateTime.Today,
            UnitPrice = 1.00m,
            OtherCosts = 0.00m,
            Quantity = 1.00m,
            Asset = new Asset(),
            IsBuy = true
        };
        private void ToggleAddForm()
        {
            isAddFormVisible = !isAddFormVisible;
        }
        
        private void AddEditionEvent(string message)
        {
            editEvents.Add(message);
            ReloadTable();
        }

        private void BackupItem(object element)
        {
            elementBeforeEdit = new()
                {
                    TransactionDate = ((Transaction)element).TransactionDate,
                    Asset = new Asset
                    {
                        Ticker = ((Transaction)element).Asset.Ticker
                    },
                    UnitPrice = ((Transaction)element).UnitPrice,
                };
            AddEditionEvent($"RowEditPreview event: made a backup of Element {((Transaction)element).Asset.Ticker}");
        }
        private void ItemHasBeenCommitted(object element)
        {
            AddEditionEvent($"RowEditCommit event: Changes to Element {((Transaction)element).Asset.Ticker} committed");
        }
        private void ResetItemToOriginalValues(object element)
        {
            ((Transaction)element).TransactionDate = elementBeforeEdit.TransactionDate;
            ((Transaction)element).Asset.Ticker = elementBeforeEdit.Asset.Ticker;
            ((Transaction)element).UnitPrice = elementBeforeEdit.UnitPrice;
            AddEditionEvent($"RowEditCancel event: Editing of Element {((Transaction)element).Asset.Ticker} canceled");
        }
        
        private bool FilterFunc(Transaction transaction)
        {
            // Filter by search string
            bool matchesSearch = transaction.Asset.Ticker.Contains(searchString, StringComparison.OrdinalIgnoreCase)
                            || transaction.TransactionDate.ToString("dd/MM/yyyy").Contains(searchString);

            // Filtra por tipo de transação
            bool matchesType = selectedTransactionType == All ||
                            (selectedTransactionType == Purchase && transaction.IsBuy) ||
                            (selectedTransactionType == Sale && !transaction.IsBuy);

            return matchesSearch && matchesType;
        }
        private async Task<string?> GetUserIdAsync()
        {
            var authstate = await GetAuthenticationStateAsync.GetAuthenticationStateAsync();
            if (authstate.User.Identity != null && authstate.User.Identity.IsAuthenticated)
            {
                return authstate.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            }
            return string.Empty;
        }
        private string GetAssetTypeDescription(AssetType assetType)
        {
            var displayAttribute = assetType.GetType()
                                    .GetField(assetType.ToString())
                                    ?.GetCustomAttribute<DisplayAttribute>();

            return displayAttribute?.Name ?? assetType.ToString();
        }

        private async Task AddTransactionAsync()
        {
            if (quickAddForm.IsValid)
            {
                // Atribuir um ID único à nova transação
                newTransaction.Id = Guid.NewGuid();
                newTransaction.UserId = userId;
                newTransaction.TransactionDate = DateTime.Today;
                newTransaction.CreateDate = DateTime.Now;
                newTransaction.TransactionDate = DateTime.Parse(newTransactionDate);
                newTransaction.Asset = await _assetService.GetByIdAsync(newTransaction.AssetIsinCode);

                _transactionService.Add(newTransaction);
                
                transactionList.Add(newTransaction);

                Snackbar.Add($"Transação adicionada com sucesso.", Severity.Success);

                newTransaction = BuildNewTransaction();

                ReloadTable();
            }
        }
        private Transaction BuildNewTransaction()
        {
            return  new Transaction
            {
                UnitPrice = 1.00m,
                OtherCosts = 0.00m,
                Quantity = 1.00m,
                Asset = new Asset {Type = AssetType.Stock},
                IsBuy = true
            };
        }
        private void ImportFromExcel()
        {
            // Lógica para importar dados de um arquivo Excel
            Console.WriteLine("Abrindo seletor de arquivos para Importar Excel.");
        }

        private void DeleteTransaction(object? obj)
        {
            if (obj is not Transaction transaction)
            {
                Snackbar.Add("Invalid transaction.", Severity.Error);
                return;
            }

            var index = transactionList.FindIndex(t => t.Id == transaction.Id);

            if (index < 0)
            {
                Snackbar.Add("Transaction not found.", Severity.Warning);
                return;
            }

            _tempDeletedTransaction = transactionList[index];
            transactionList.RemoveAt(index);

            ReloadTable();

            Snackbar.Add("Transação excluída.", Severity.Error, options =>
            {
                options.Action = "Desfazer";
                options.RequireInteraction = false;
                options.VisibleStateDuration = 5000; // 5 seconds

                options.Onclick = snackbar =>
                {
                    UndoDeleteTransaction();
                    Snackbar.RemoveByKey(SnackBarKey);
                    return Task.CompletedTask;
                };
            }, SnackBarKey);

            // Initialize the timer to confirm deletion after 5 seconds
            _undoTimer?.Dispose(); // Dispose any existing timer
            _undoTimer = new Timer(async _ =>
            {
                await InvokeAsync(() =>
                {
                    ConfirmDeleteTransaction(transaction.Id);
                    _undoTimer?.Dispose(); // Dispose the timer after execution
                });
            }, null, 5000, Timeout.Infinite);
        }

        private void UndoDeleteTransaction()
        {
            if (_tempDeletedTransaction != null)
            {
                transactionList.Add(_tempDeletedTransaction);
                _tempDeletedTransaction = null;

                ReloadTable();
            }

            _undoTimer?.Dispose();
        }

        private void ConfirmDeleteTransaction(Guid id)
        {
            _transactionService.RemoveAsync(id);
            Snackbar.RemoveByKey(SnackBarKey);
            _tempDeletedTransaction = null;
            _undoTimer?.Dispose();
        }

        private void OnTickerValueChanged(string value)
        {
            var asset = assets.FirstOrDefault(a => a.Ticker.Equals(value, StringComparison.OrdinalIgnoreCase));
            
            if (asset is not null)
            {
                newTransaction.Asset = new()
                {
                    IsinCode = asset.IsinCode,
                    CurrentPrice = asset.CurrentPrice,
                    Ticker = asset.Ticker,
                    Type = asset.Type
                };
                newTransaction.AssetIsinCode = asset.IsinCode;
            }
        }
        private bool SearchItems(string value, string searchString)
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
        private string GetQuantity(Transaction transaction)
        {
            if (transaction.Asset.Type == AssetType.Cryptocurrency)
            {
                return transaction.Quantity.ToString("N8");
            }
            else
            {
                return transaction.Quantity.ToString("N0");
            }
        }
        private void ReloadTable()
        {
            StateHasChanged();
        }
    }
}