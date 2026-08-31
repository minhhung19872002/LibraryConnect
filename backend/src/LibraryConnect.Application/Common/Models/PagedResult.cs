namespace LibraryConnect.Application.Common.Models;

/// <summary>
/// Server-side pagination envelope. Every list endpoint returns this shape — the client never
/// receives an unbounded collection (section 6.3).
/// </summary>
public class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; set; } = Array.Empty<T>();
    public int TotalCount { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;

    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPrevious => Page > 1;
    public bool HasNext => Page < TotalPages;

    public PagedResult() { }

    public PagedResult(IReadOnlyList<T> items, int totalCount, int page, int pageSize)
    {
        Items = items;
        TotalCount = totalCount;
        Page = page;
        PageSize = pageSize;
    }

    public static PagedResult<T> Empty(int page = 1, int pageSize = 20) =>
        new(Array.Empty<T>(), 0, page, pageSize);
}

/// <summary>Base class for every paged query, with the sorting and paging parameters shared by all lists.</summary>
public abstract class PagedRequest
{
    private const int MaxPageSize = 500;

    private int _pageSize = 20;
    private int _page = 1;

    public int Page
    {
        get => _page;
        set => _page = value < 1 ? 1 : value;
    }

    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value switch
        {
            < 1 => 20,
            > MaxPageSize => MaxPageSize,
            _ => value
        };
    }

    /// <summary>Free-text filter applied to the columns each query decides are searchable.</summary>
    public string? Keyword { get; set; }

    /// <summary>Property name to sort by; each handler validates it against a whitelist.</summary>
    public string? SortBy { get; set; }

    public bool SortDescending { get; set; }

    public int Skip => (Page - 1) * PageSize;
}
