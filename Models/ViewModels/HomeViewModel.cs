namespace LibrarySystem.Models.ViewModels
{
    /// <summary>Backs the public landing page - catalog-wide item stats plus a few featured items.</summary>
    public class HomeViewModel
    {
        public required ItemStatusStatsViewModel ItemStatus { get; init; }

        public required IReadOnlyList<ItemListItemViewModel> FeaturedItems { get; init; }
    }
}
