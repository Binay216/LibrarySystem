namespace LibrarySystem.Services
{
    /// <summary>
    /// Scans open loans for due-soon and overdue reminders. Kept separate from
    /// <see cref="INotificationService"/>: that interface is "render and send one
    /// notification to one borrower", called at a specific business event (borrow,
    /// return). This is a batch scan with its own query, its own idempotency
    /// concern, and a summary to report back - a different shape of operation,
    /// triggered on demand (for now) from the Manager dashboard rather than by an
    /// event in another service.
    /// </summary>
    public interface INotificationSweepService
    {
        /// <summary>
        /// Queues a DueSoon notification for every open loan due within the next 3
        /// days (inclusive of today, per <see cref="ILibraryClock.Today"/>), and an
        /// Overdue notification for every open loan already past its due date.
        /// Skips any loan/type pair already notified today, so running the sweep
        /// more than once on the same day doesn't duplicate notifications. A
        /// failure queuing one loan's notification is logged and does not stop the
        /// rest of the sweep.
        /// </summary>
        Task<NotificationSweepResult> RunAsync();
    }
}
