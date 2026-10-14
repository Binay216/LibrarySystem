using LibrarySystem.Models;
using LibrarySystem.Models.ViewModels;

namespace LibrarySystem.Services
{
    /// <summary>
    /// Orchestration for the public kiosk's transactions. Thin by design - the
    /// actual borrowing/reservation rules live in ILoanService/IReservationService
    /// and are reused as-is; this just adapts them to what the kiosk screens need
    /// (card-number lookup, a borrower-scoped account view, search).
    /// </summary>
    public interface IKioskService
    {
        /// <summary>
        /// Resolves a card number to an active borrower. There's no separate card
        /// number field or patron login in this system - the card number IS the
        /// borrower's database Id. See the remarks on KioskController for why
        /// that's a reasonable demo shortcut but not what a real deployment should do.
        /// </summary>
        Task<Borrower?> FindActiveBorrowerAsync(int cardNumber);

        /// <summary>Looks up an item's current name/status before a checkout attempt, to decide whether a hold can be offered on failure.</summary>
        Task<Item?> FindItemByLibraryCodeAsync(string libraryCode);

        /// <summary>Borrows one item via ILoanService.BorrowAsync, translated into what the running checkout list and hold-offer need.</summary>
        Task<KioskCheckoutAttemptResult> CheckOutItemAsync(string libraryCode, int borrowerId);

        /// <summary>Current loans (overdue flagged), outstanding fines and active reservations for one borrower.</summary>
        Task<KioskAccountSummaryViewModel> GetAccountSummaryAsync(int borrowerId);

        /// <summary>Places a hold via IReservationService.PlaceAsync.</summary>
        Task<ReservationResult> PlaceHoldAsync(int itemId, int borrowerId);

        /// <summary>Searches items by name or description, with availability and current branch - the same shape the public catalog shows.</summary>
        Task<List<ItemListItemViewModel>> SearchAsync(string term);
    }
}
