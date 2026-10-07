namespace LibrarySystem.Services
{
    /// <summary>Outcome of one <see cref="INotificationSweepService.RunAsync"/> pass.</summary>
    public class NotificationSweepResult
    {
        public int DueSoonQueued { get; init; }

        public int OverdueQueued { get; init; }

        /// <summary>Loans that already had today's reminder of the same type queued by an earlier run of the sweep.</summary>
        public int SkippedAlreadyNotified { get; init; }

        /// <summary>Loans where queuing itself threw - logged individually, never aborts the rest of the sweep.</summary>
        public int Failed { get; init; }
    }
}
