using RealEstate.Core.Common;

namespace RealEstate.Application.Common;

public class PagedResponse<T>
{
    public IReadOnlyList<T> Items { get; init; } = [];
    public int PageNumber { get; init; }
    public int PageSize { get; init; }
    public long TotalCount { get; init; }
    public int TotalPages { get; init; }

    public static PagedResponse<T> From<TSource>(PagedResult<TSource> source, IReadOnlyList<T> mappedItems) => new()
    {
        Items = mappedItems,
        PageNumber = source.PageNumber,
        PageSize = source.PageSize,
        TotalCount = source.TotalCount,
        TotalPages = source.TotalPages
    };
}
