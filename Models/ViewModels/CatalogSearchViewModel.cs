namespace LibrarySystem.Models.ViewModels
{
    /// <summary>Backs the public catalog search - the search form's state plus its results.</summary>
    public class CatalogSearchViewModel
    {
        public string? SearchTerm { get; set; }

        public ItemTypeOption? ItemType { get; set; }

        public bool AvailableOnly { get; set; }

        public IReadOnlyList<ItemListItemViewModel> Results { get; set; } = [];
    }
}
