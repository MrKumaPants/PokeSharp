namespace PokeSharp.Data.Extensions;

/// <summary>
///     Result of a paginated query.
/// </summary>
/// <typeparam name="T">Entity type</typeparam>
public sealed class PaginatedResult<T>
{
    public List<T> Items { get; init; } = new();
    public int PageNumber { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public int TotalPages { get; init; }
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;
}

