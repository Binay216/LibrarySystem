namespace LibrarySystem.Models.Dto
{
    /// <summary>A page of results plus enough paging metadata for a client to request the next one.</summary>
    public record PagedResultDto<T>(IReadOnlyList<T> Results, int TotalCount, int Page, int PageSize);
}
