using Microsoft.AspNetCore.Mvc.Rendering;

namespace LibrarySystem.Models.ViewModels
{
    /// <summary>Bundles all three manager statistics for the single dashboard view.</summary>
    public class ManagerDashboardViewModel
    {
        public required BorrowingStatsViewModel Borrowing { get; init; }

        public required ItemStatusStatsViewModel ItemStatus { get; init; }

        public required FineStatsViewModel Fines { get; init; }

        /// <summary>The branch being filtered to, or null for every branch (the default).</summary>
        public int? SelectedBranchId { get; init; }

        /// <summary>Every branch, for the filter dropdown - populated by the controller.</summary>
        public IEnumerable<SelectListItem> Branches { get; init; } = [];
    }
}
