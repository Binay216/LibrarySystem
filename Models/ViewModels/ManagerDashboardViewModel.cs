namespace LibrarySystem.Models.ViewModels
{
    /// <summary>Bundles all three manager statistics for the single dashboard view.</summary>
    public class ManagerDashboardViewModel
    {
        public required BorrowingStatsViewModel Borrowing { get; init; }

        public required ItemStatusStatsViewModel ItemStatus { get; init; }

        public required FineStatsViewModel Fines { get; init; }
    }
}
