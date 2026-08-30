namespace SocarDispatch.Application.Common.Models;

/// <summary>
/// Base pagination query parameters with safe boundary constraints.
/// </summary>
public record PaginationFilter
{
    private const int MaxPageSize = 100;
    private int _pageSize = 25;
    private int _pageNumber = 1;

    public int PageNumber
    {
        get => _pageNumber;
        init => _pageNumber = value < 1 ? 1 : value;
    }

    public int PageSize
    {
        get => _pageSize;
        init => _pageSize = value > MaxPageSize ? MaxPageSize : (value < 1 ? 25 : value);
    }
}
