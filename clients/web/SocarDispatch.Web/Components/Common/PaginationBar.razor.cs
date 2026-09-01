using Microsoft.AspNetCore.Components;

namespace SocarDispatch.Web.Components.Common;

public partial class PaginationBar : ComponentBase
{
    [Parameter] public int CurrentPage { get; set; } = 1;
    [Parameter] public int PageSize { get; set; } = 25;
    [Parameter] public int TotalCount { get; set; } = 0;
    [Parameter] public int[] PageSizeOptions { get; set; } = new[] { 10, 25, 50, 100 };
    [Parameter] public string ItemName { get; set; } = "incidents";
    [Parameter] public bool Disabled { get; set; } = false;

    [Parameter] public EventCallback<int> OnPageChanged { get; set; }
    [Parameter] public EventCallback<int> OnPageSizeChanged { get; set; }

    protected int TotalPages => PageSize > 0 ? Math.Max(1, (int)Math.Ceiling(TotalCount / (double)PageSize)) : 1;
    protected bool HasPreviousPage => CurrentPage > 1;
    protected bool HasNextPage => CurrentPage < TotalPages;
    protected int StartIndex => TotalCount == 0 ? 0 : (CurrentPage - 1) * PageSize + 1;
    protected int EndIndex => Math.Min(CurrentPage * PageSize, TotalCount);

    protected async Task GoToPage(int page)
    {
        if (Disabled || page == CurrentPage || page < 1 || page > TotalPages)
            return;

        await OnPageChanged.InvokeAsync(page);
    }

    protected async Task HandlePageSizeChange(ChangeEventArgs e)
    {
        if (Disabled)
            return;

        if (int.TryParse(e.Value?.ToString(), out var newSize) && newSize > 0 && newSize != PageSize)
        {
            await OnPageSizeChanged.InvokeAsync(newSize);
        }
    }

    /// <summary>
    /// Computes visible page numbers and returns -1 as sentinel for ellipsis indicators.
    /// </summary>
    protected IEnumerable<int> GetVisiblePageNumbers()
    {
        var total = TotalPages;
        var current = CurrentPage;

        if (total <= 7)
        {
            for (var i = 1; i <= total; i++)
                yield return i;
            yield break;
        }

        // Always show first page
        yield return 1;

        if (current <= 4)
        {
            for (var i = 2; i <= 5; i++)
                yield return i;

            yield return -1; // Ellipsis
            yield return total;
        }
        else if (current >= total - 3)
        {
            yield return -1; // Ellipsis

            for (var i = total - 4; i <= total; i++)
                yield return i;
        }
        else
        {
            yield return -1; // Left Ellipsis
            yield return current - 1;
            yield return current;
            yield return current + 1;
            yield return -1; // Right Ellipsis
            yield return total;
        }
    }
}
