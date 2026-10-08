namespace LibrarySystem.Models.Dto
{
    /// <summary>
    /// A single item's full public detail. Same exclusions as
    /// <see cref="ItemSummaryDto"/> (no loan/borrower data, no internal Id, no
    /// home-branch/transfer bookkeeping), but exposes the type-specific fields
    /// structurally (Author/Genre, Artist/Year, ToyType/RecommendedAge) rather
    /// than only the flattened Summary string - a single-item lookup is exactly
    /// where a consumer wants the structured data, not just a display blurb.
    /// Only the fields matching this item's actual type are populated; the rest
    /// are null.
    /// </summary>
    public record ItemDetailDto(
        string LibraryCode,
        string Name,
        string Description,
        string Type,
        string Status,
        string CurrentBranchName,
        int QueueCount,
        string? Author,
        string? Genre,
        string? Artist,
        int? Year,
        string? ToyType,
        int? RecommendedAge);
}
