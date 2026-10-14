namespace LibrarySystem.Models.ViewModels
{
    /// <summary>The kiosk search screen: the typed term plus results, if any were searched for.</summary>
    public class KioskSearchViewModel
    {
        public string? Term { get; init; }

        public IReadOnlyList<ItemListItemViewModel> Results { get; init; } = [];

        public bool HasSearched => !string.IsNullOrWhiteSpace(Term);
    }
}
