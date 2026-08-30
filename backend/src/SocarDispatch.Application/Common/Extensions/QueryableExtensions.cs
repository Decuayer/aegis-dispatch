using Microsoft.EntityFrameworkCore;
using SocarDispatch.Application.Common.Models;

namespace SocarDispatch.Application.Common.Extensions;

/// <summary>
/// Extension methods for IQueryable pagination evaluation.
/// </summary>
public static class QueryableExtensions
{
    public static async Task<PagedResult<T>> ToPagedResultAsync<T>(
        this IQueryable<T> source,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var validPageNumber = pageNumber < 1 ? 1 : pageNumber;
        var validPageSize = pageSize > 100 ? 100 : (pageSize < 1 ? 25 : pageSize);

        var count = await source.CountAsync(cancellationToken);
        var items = await source
            .Skip((validPageNumber - 1) * validPageSize)
            .Take(validPageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<T>(items, count, validPageNumber, validPageSize);
    }
}
