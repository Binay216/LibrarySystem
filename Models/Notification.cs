using System.ComponentModel.DataAnnotations;
using LibrarySystem.Models.Enums;

namespace LibrarySystem.Models
{
    /// <summary>
    /// A record of one notification to a borrower, queued and (simulated-)sent by
    /// <see cref="Services.INotificationService"/>. Recipient is copied at send
    /// time rather than read live from Borrower, so the record stays an accurate
    /// account of what was actually sent even if the borrower's contact details
    /// change later.
    /// </summary>
    public class Notification
    {
        public int Id { get; set; }

        [Required]
        public int BorrowerId { get; set; }

        public Borrower? Borrower { get; set; }

        /// <summary>
        /// The loan this notification is about, when it's about one - Borrowed,
        /// DueSoon, Overdue and FineAccrued all set this; ReservationReady and
        /// TransferArrived aren't loan-driven, so it's null for those. Doubles as
        /// the idempotency key for NotificationSweepService: (LoanId, Type, day)
        /// identifies "have we already sent this reminder today".
        /// </summary>
        public int? LoanId { get; set; }

        public Loan? Loan { get; set; }

        [Required]
        public NotificationChannel Channel { get; set; }

        [Required]
        public NotificationType Type { get; set; }

        /// <summary>The email address or phone number this was actually sent to.</summary>
        [Required]
        [StringLength(256)]
        public string Recipient { get; set; } = string.Empty;

        [StringLength(200)]
        public string Subject { get; set; } = string.Empty;

        [Required]
        [StringLength(2000)]
        public string Body { get; set; } = string.Empty;

        public DateTime CreatedDate { get; set; }

        /// <summary>Null until sent (or if sending ultimately fails).</summary>
        public DateTime? SentDate { get; set; }

        [Required]
        public NotificationStatus Status { get; set; } = NotificationStatus.Queued;
    }
}
