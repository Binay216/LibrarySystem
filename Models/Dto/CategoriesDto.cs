namespace LibrarySystem.Models.Dto
{
    public record ItemTypeCountDto(string Type, int Count);

    /// <summary>Item types with their counts, plus the distinct book genres and toy types currently in the collection.</summary>
    public record CategoriesDto(
        IReadOnlyList<ItemTypeCountDto> Types,
        IReadOnlyList<string> BookGenres,
        IReadOnlyList<string> ToyTypes);
}
