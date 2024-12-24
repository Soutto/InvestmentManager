using InvestmentManager.Services.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace InvestmentManager.Components.Pages
{
    public partial class News() : ComponentBase
    {
        [Inject] private IJSRuntime _jsRuntime { get; set; } = default!;
        [Inject] private INewsService _newsService { get; set; } = default!;

        private IEnumerable<InvestmentManager.Shared.Models.Application.News> newsList = [];
        private int currentPage = 1;
        private int itemsPerPage = 9;

        protected override async Task OnInitializedAsync()
        {
            newsList = await FetchNewsAsync();
        }

        private async Task<IEnumerable<InvestmentManager.Shared.Models.Application.News>> FetchNewsAsync()
        {
            return await _newsService.GetAllAsync();
        }

        private IEnumerable<InvestmentManager.Shared.Models.Application.News> PagedNewsList => newsList.Skip((currentPage - 1) * itemsPerPage).Take(itemsPerPage);

        private int TotalPages => (int)Math.Ceiling((double)(newsList?.Count() ?? 0) / itemsPerPage);

        private void PageChanged(int i)
        {
            currentPage = i;
            StateHasChanged();
        }

        private async Task NavigateToUrlAsync(string url)
        {
            await _jsRuntime.InvokeVoidAsync("open", url, "_blank");
        }
    }
}
