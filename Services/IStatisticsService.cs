using LibrarySystem.Models.ViewModels;

namespace LibrarySystem.Services
{
    /// <summary>Read-only statistics for the Manager dashboards. Loan is the sole data source for all three.</summary>
    public interface IStatisticsService
    {
        /// <summary>
        /// Requirement 3a: loans over time, most borrowed items, and active/overdue
        /// loans. When <paramref name="branchId"/> is given, restricted to loans
        /// borrowed at that branch (Loan.BorrowedAtBranchId); null means every branch.
        /// </summary>
        Task<BorrowingStatsViewModel> GetBorrowingStatsAsync(int? branchId);

        /// <summary>
        /// Requirement 3b: item counts per status, broken down by type and by branch,
        /// plus items currently away from their home branch. When
        /// <paramref name="branchId"/> is given, restricted to items physically at
        /// that branch (Item.CurrentBranchId); null means every branch.
        /// </summary>
        Task<ItemStatusStatsViewModel> GetItemStatusStatsAsync(int? branchId);

        /// <summary>
        /// Requirement 3c: accrued, paid, outstanding and average fines across
        /// returned loans. When <paramref name="branchId"/> is given, restricted to
        /// loans borrowed at that branch (Loan.BorrowedAtBranchId); null means every branch.
        /// </summary>
        Task<FineStatsViewModel> GetFineStatsAsync(int? branchId);

        /// <summary>Up to one currently-available item per type (Book, Music, Toy), for the public landing page.</summary>
        Task<List<ItemListItemViewModel>> GetFeaturedItemsAsync();
    }
}
