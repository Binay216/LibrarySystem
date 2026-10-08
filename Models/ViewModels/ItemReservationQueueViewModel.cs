namespace LibrarySystem.Models.ViewModels
{
    /// <summary>Backs the per-item "where am I in the queue" lookup - a library code search plus that item's ordered queue.</summary>
    public class ItemReservationQueueViewModel
    {
        public string? LibraryCode { get; set; }

        public string? ItemName { get; set; }

        public bool ItemFound { get; set; }

        /// <summary>Set when a library code was searched but didn't match any item.</summary>
        public string? ErrorMessage { get; set; }

        public DateTime Today { get; set; }

        public List<ReservationQueueEntryViewModel> Queue { get; set; } = [];
    }
}
