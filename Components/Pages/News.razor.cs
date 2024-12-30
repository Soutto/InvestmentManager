using InvestmentManager.Services.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace InvestmentManager.Components.Pages
{
    public partial class News() : ComponentBase
    {
        #region Dependencies (Injected Services)
        [Inject] private IJSRuntime JSRuntime { get; set; } = default!;
        [Inject] private INewsService NewsService { get; set; } = default!;
        #endregion

        #region Properties
        protected IEnumerable<Shared.Models.Application.News> PagedNewsList
        {
            get
            {
                _pagedNewsList = _newsList.Skip((CurrentPage - 1) * itemsPerPage).Take(itemsPerPage);
                return _pagedNewsList;
            }
            set
            {
                _pagedNewsList = value;
            }
        }

        protected int TotalPages
        {
            get
            {
                _totalPages = (int)Math.Ceiling((double)(_newsList?.Count() ?? 0) / itemsPerPage);
                return _totalPages;
            }
            set
            {
                _totalPages = value;
            }
        }
        protected int CurrentPage { get; set; } = 1;
        #endregion
        
        #region Fields
        private IEnumerable<Shared.Models.Application.News> _pagedNewsList = [];
        private int _totalPages;
        private IEnumerable<Shared.Models.Application.News> _newsList = [];
        private int itemsPerPage = 9;
        #endregion

        #region Protected Methods
        protected override async Task OnInitializedAsync()
        {
            _newsList = await FetchNewsAsync();
        }

        private void PageChanged(int i)
        {
            CurrentPage = i;
            StateHasChanged();
        }

        private async Task NavigateToUrlAsync(string url)
        {
            await JSRuntime.InvokeVoidAsync("open", url, "_blank");
        }
        #endregion

        #region Private Methods
        private async Task<IEnumerable<InvestmentManager.Shared.Models.Application.News>> FetchNewsAsync()
        {
            return await NewsService.GetAllAsync();
        }
        #endregion
    }
}
