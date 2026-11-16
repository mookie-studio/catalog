namespace Catalog.Api.Infrastructure;

/// <summary>Paged list response: { items, page, pageSize, totalCount }.</summary>
public sealed record PagedResponse<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);

/// <summary>Normalized paging parameters: page is 1-based, pageSize defaults to 20 and is capped at 50.</summary>
public readonly record struct Paging(int Page, int PageSize)
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 50;

    public int Skip => (Page - 1) * PageSize;

    public static Paging From(int? page, int? pageSize) => new(
        Math.Max(page ?? 1, 1),
        Math.Clamp(pageSize ?? DefaultPageSize, 1, MaxPageSize));
}
