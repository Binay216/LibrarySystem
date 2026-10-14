namespace LibrarySystem.Models.ViewModels
{
    /// <summary>
    /// The kiosk's whole in-progress transaction, held server-side in session
    /// (never in a client-visible cookie value or query string) so a patron's
    /// card number is never exposed in a URL and can't be swapped by editing
    /// one. Cleared on Cancel, on completion, and by the 60s inactivity reset -
    /// see KioskController.Reset.
    /// </summary>
    public class KioskSessionState
    {
        public const string SessionKey = "KioskState";

        public int? BorrowerId { get; set; }

        public List<KioskCheckedOutItem> CheckedOutItems { get; set; } = [];
    }

    public record KioskCheckedOutItem(string LibraryCode, string ItemName, DateTime DueDate);
}
