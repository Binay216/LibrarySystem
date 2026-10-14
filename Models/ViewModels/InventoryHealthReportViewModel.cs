namespace LibrarySystem.Models.ViewModels
{
    /// <summary>
    /// Current inventory state - item status/type/branch breakdown and the
    /// away-from-home list are a current-state snapshot (Item has no historical
    /// status tracking, so there's nothing date-ranged to compute for those
    /// specifically), reused directly from IStatisticsService.GetItemStatusStatsAsync
    /// rather than re-derived. Filters.StartDate/EndDate are still recorded for
    /// the export to show what was asked for, even though they don't change
    /// these particular figures - only DamagedItems/DestroyedItems (the actual
    /// item lists, which the dashboard only ever shows as counts) are new here.
    /// </summary>
    public class InventoryHealthReportViewModel
    {
        public required ReportFilterInfo Filters { get; init; }

        public IReadOnlyList<ItemStatusStatsViewModel.StatusCount> CountsByStatus { get; init; } = [];

        public IReadOnlyList<ItemStatusStatsViewModel.TypeStatusBreakdown> BreakdownByType { get; init; } = [];

        public IReadOnlyList<ItemStatusStatsViewModel.BranchStatusBreakdown> BreakdownByBranch { get; init; } = [];

        public int AwayFromHomeCount { get; init; }

        public IReadOnlyList<ItemStatusStatsViewModel.AwayFromHomeItem> AwayFromHomeItems { get; init; } = [];

        public IReadOnlyList<InventoryItemSummary> DamagedItems { get; init; } = [];

        public IReadOnlyList<InventoryItemSummary> DestroyedItems { get; init; } = [];

        public record InventoryItemSummary(string ItemName, string LibraryCode, string CurrentBranchName);
    }
}
