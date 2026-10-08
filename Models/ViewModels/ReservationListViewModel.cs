using LibrarySystem.Models.Enums;

namespace LibrarySystem.Models.ViewModels
{
    /// <summary>Backs the Reservations Index page - every active reservation across all items, optionally narrowed to one status.</summary>
    public class ReservationListViewModel
    {
        /// <summary>Null means "all active" (Waiting and Ready); otherwise narrowed to just Waiting or just Ready.</summary>
        public ReservationStatus? StatusFilter { get; set; }

        /// <summary>The library's current date, for highlighting Ready rows near or past expiry in the view.</summary>
        public DateTime Today { get; set; }

        public List<ReservationQueueEntryViewModel> Reservations { get; set; } = [];
    }
}
