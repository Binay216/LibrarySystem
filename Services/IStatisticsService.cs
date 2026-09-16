using LibrarySystem.Models.ViewModels;

namespace LibrarySystem.Services
{
    /// <summary>Read-only statistics for the Manager dashboards. Loan is the sole data source for all three.</summary>
    public interface IStatisticsService
    {
        /// <summary>Requirement 3a: loans over time, most borrowed items, and active/overdue loans.</summary>
        Task<BorrowingStatsViewModel> GetBorrowingStatsAsync();

        /// <summary>Requirement 3b: item counts per status, overall and broken down by type.</summary>
        Task<ItemStatusStatsViewModel> GetItemStatusStatsAsync();

        /// <summary>Requirement 3c: accrued, paid, outstanding and average fines across returned loans.</summary>
        Task<FineStatsViewModel> GetFineStatsAsync();

        /// <summary>Up to one currently-available item per type (Book, Music, Toy), for the public landing page.</summary>
        Task<List<ItemListItemViewModel>> GetFeaturedItemsAsync();
    }
}
