namespace LibrarySystem.Models.ViewModels
{
    /// <summary>Everything the account summary screen shows for one borrower: current loans, outstanding fines, active reservations.</summary>
    public class KioskAccountSummaryViewModel
    {
        public required string BorrowerName { get; init; }

        /// <summary>Loans not yet returned - IsOverdue flags the ones past their due date.</summary>
        public required IReadOnlyList<LoanHistoryEntryViewModel> CurrentLoans { get; init; }

        /// <summary>Returned loans with a fine that's still unpaid.</summary>
        public required IReadOnlyList<LoanHistoryEntryViewModel> OutstandingFines { get; init; }

        public required IReadOnlyList<ReservationQueueEntryViewModel> Reservations { get; init; }
    }
}
