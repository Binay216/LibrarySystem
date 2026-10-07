using System.ComponentModel.DataAnnotations;
using LibrarySystem.Models.Enums;

namespace LibrarySystem.Models
{
    /// <summary>A borrower's place in the queue for an item that isn't available to borrow right now.</summary>
    public class Reservation
    {
        public int Id { get; set; }

        [Required]
        public int ItemId { get; set; }

        public Item? Item { get; set; }

        [Required]
        public int BorrowerId { get; set; }

        public Borrower? Borrower { get; set; }

        [DataType(DataType.Date)]
        public DateTime PlacedDate { get; set; }

        /// <summary>
        /// 1-based position in the active queue for this item. Only meaningful
        /// while Status is Waiting or Ready - see ReservationService for how it's
        /// assigned and recalculated.
        /// </summary>
        [Required]
        public int QueuePosition { get; set; }

        [Required]
        public ReservationStatus Status { get; set; } = ReservationStatus.Waiting;

        /// <summary>Set when the item becomes available and this reservation moves to Ready.</summary>
        [DataType(DataType.Date)]
        public DateTime? ReadyDate { get; set; }

        /// <summary>Set alongside ReadyDate - how long the borrower has to collect it before it Expires.</summary>
        [DataType(DataType.Date)]
        public DateTime? ExpiryDate { get; set; }

        /// <summary>When the borrower was notified it's ready for pickup.</summary>
        [DataType(DataType.Date)]
        public DateTime? NotifiedDate { get; set; }
    }
}
