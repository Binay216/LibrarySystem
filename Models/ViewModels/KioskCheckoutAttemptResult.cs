namespace LibrarySystem.Models.ViewModels
{
    /// <summary>The outcome of one "add this library code" attempt on the kiosk checkout screen.</summary>
    public class KioskCheckoutAttemptResult
    {
        public required bool Succeeded { get; init; }

        public required string Message { get; init; }

        public KioskCheckedOutItem? CheckedOutItem { get; init; }

        /// <summary>Set when the attempt failed because the item is Borrowed/Damaged - a hold can be offered for exactly this code.</summary>
        public string? HoldOfferLibraryCode { get; init; }
    }
}
