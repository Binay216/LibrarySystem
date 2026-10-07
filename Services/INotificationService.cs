using LibrarySystem.Models;
using LibrarySystem.Models.Enums;

namespace LibrarySystem.Services
{
    /// <summary>
    /// Queues and (for now, simulated-)sends notifications to borrowers. This is
    /// the model/service/template layer only - nothing calls QueueAsync yet.
    /// </summary>
    public interface INotificationService
    {
        /// <summary>
        /// Renders the template for <paramref name="type"/> with <paramref name="tokens"/>
        /// (plus the borrower's own name, added automatically), picks a channel - SMS
        /// if the borrower has a phone number, otherwise email - and "sends" it: with
        /// no real email/SMS provider yet, sending is simulated by logging the
        /// rendered notification and marking it Sent immediately. Throws if
        /// <paramref name="borrowerId"/> doesn't match an existing borrower - this is
        /// called by other code with a borrower id it already knows is valid, not by
        /// user input, so that's a programming error rather than an expected rejection.
        /// <paramref name="loanId"/> records which loan this is about, for types that
        /// have one - it's also what NotificationSweepService checks to avoid sending
        /// the same reminder for the same loan more than once a day.
        /// </summary>
        Task<Notification> QueueAsync(int borrowerId, NotificationType type, IReadOnlyDictionary<string, string> tokens, int? loanId = null);
    }
}
