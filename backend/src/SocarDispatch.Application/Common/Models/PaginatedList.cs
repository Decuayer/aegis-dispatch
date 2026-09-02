namespace SocarDispatch.Application.Common.Models;

public class PaginatedList<T> : PagedResult<T>
{
    public PaginatedList() { }

    public PaginatedList(List<T> items, int count, int pageNumber, int pageSize)
        : base(items, count, pageNumber, pageSize)
    {
    }
}
