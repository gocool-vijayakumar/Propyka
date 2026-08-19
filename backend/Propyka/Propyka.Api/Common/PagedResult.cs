namespace Propyka.Api.Common;

/// <summary>
/// One paging shape for the whole application. Lives in Common rather than a
/// module because Identity, Listings and every future module returns it.
/// </summary>
public sealed record PagedResult<T>(
    int Page,
    int PageSize,
    int Total,
    int TotalPages,
    IReadOnlyList<T> Items)
{
    public static PagedResult<T> Create(int page, int pageSize, int total, IReadOnlyList<T> items)
        => new(page, pageSize, total, (int)Math.Ceiling(total / (double)pageSize), items);

    /// <summary>Clamps caller-supplied paging into a range the database can serve.</summary>
    public static (int Page, int PageSize) Normalise(int page, int pageSize, int maxPageSize = 50)
        => (Math.Max(page, 1), Math.Clamp(pageSize, 1, maxPageSize));
}
