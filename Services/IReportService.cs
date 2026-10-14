using LibrarySystem.Models.ViewModels;

namespace LibrarySystem.Services
{
    /// <summary>
    /// Date-ranged reports for export, for the Manager role. Distinct from
    /// IStatisticsService, which serves the dashboard's live, non-date-ranged
    /// figures - see each method's remarks for where this does and doesn't
    /// reuse it. An empty result (a period with no activity) is a valid report,
    /// not an error: zeroed totals and empty lists, never thrown.
    /// </summary>
    public interface IReportService
    {
        /// <summary>
        /// Loan activity for items borrowed between <paramref name="startDate"/>
        /// and <paramref name="endDate"/> (inclusive), optionally restricted to
        /// one branch's BorrowedAtBranchId. Not sourced from
        /// IStatisticsService.GetBorrowingStatsAsync - that method has no date
        /// bounds at all (it reports on every loan ever), so there's no hook to
        /// reuse; this runs its own BorrowedDate-bounded queries.
        /// </summary>
        Task<BorrowingReportViewModel> BorrowingReportAsync(DateTime startDate, DateTime endDate, int? branchId);

        /// <summary>
        /// Fine activity for loans returned between <paramref name="startDate"/>
        /// and <paramref name="endDate"/> (inclusive) with a non-zero fine,
        /// optionally restricted to one branch's BorrowedAtBranchId. Same
        /// reasoning as <see cref="BorrowingReportAsync"/>:
        /// IStatisticsService.GetFineStatsAsync isn't date-ranged, so this is a
        /// fresh query, not a reuse of it.
        /// </summary>
        Task<FineRevenueReportViewModel> FineRevenueReportAsync(DateTime startDate, DateTime endDate, int? branchId);

        /// <summary>
        /// Current inventory health, optionally restricted to one branch's
        /// CurrentBranchId. The status/type/branch breakdown and away-from-home
        /// list are reused directly from
        /// IStatisticsService.GetItemStatusStatsAsync - items have no historical
        /// status, so those figures are a current snapshot regardless of the
        /// given date range either way, and re-deriving the same EF queries a
        /// second time here would be the exact duplication this interface is
        /// meant to avoid. Only the damaged/destroyed item lists are new.
        /// </summary>
        Task<InventoryHealthReportViewModel> InventoryHealthReportAsync(DateTime startDate, DateTime endDate, int? branchId);
    }
}
