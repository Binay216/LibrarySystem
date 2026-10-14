namespace LibrarySystem.Models.ViewModels
{
    /// <summary>
    /// The "place a hold" screen. If the patron isn't already identified this
    /// session (arriving from an anonymous search result), it first asks for a
    /// card number; if they are (mid-checkout, or mid-account-summary), it goes
    /// straight to confirming the hold.
    /// </summary>
    public class KioskPlaceHoldViewModel
    {
        public required string LibraryCode { get; init; }

        public required string ItemName { get; init; }

        public required bool NeedsCardNumber { get; init; }

        /// <summary>Where "Continue Browsing" should return to after a hold is placed - "checkout" or null (back to idle).</summary>
        public string? ReturnTo { get; init; }

        /// <summary>Set once a hold attempt has been made, success or failure.</summary>
        public string? ResultMessage { get; init; }

        public bool? ResultSucceeded { get; init; }
    }
}
