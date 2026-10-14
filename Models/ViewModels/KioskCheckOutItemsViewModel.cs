namespace LibrarySystem.Models.ViewModels
{
    /// <summary>The "enter library codes" screen: who's checking out, and the running list of what's gone through this session.</summary>
    public class KioskCheckOutItemsViewModel
    {
        public required string BorrowerName { get; init; }

        public required IReadOnlyList<KioskCheckedOutItem> CheckedOutItems { get; init; }

        /// <summary>Set after a failed checkout attempt whose item is Borrowed/Damaged - offers "Place a Hold Instead" for that exact code.</summary>
        public string? HoldOfferLibraryCode { get; init; }
    }
}
