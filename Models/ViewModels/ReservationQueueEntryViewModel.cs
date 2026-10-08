using LibrarySystem.Models.Enums;

namespace LibrarySystem.Models.ViewModels
{
    /// <summary>One row in a reservation queue - a flattened summary of a Reservation plus its item and borrower.</summary>
    public class ReservationQueueEntryViewModel
    {
        public int Id { get; set; }

        public int ItemId { get; set; }

        public string ItemName { get; set; } = string.Empty;

        public string LibraryCode { get; set; } = string.Empty;

        public string BorrowerName { get; set; } = string.Empty;

        public int QueuePosition { get; set; }

        public ReservationStatus Status { get; set; }

        public DateTime PlacedDate { get; set; }

        /// <summary>Only meaningful while Status is Ready.</summary>
        public DateTime? ExpiryDate { get; set; }
    }
}
