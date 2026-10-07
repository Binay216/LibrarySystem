using LibrarySystem.Models.Enums;

namespace LibrarySystem.Models.ViewModels
{
    /// <summary>Requirement 3b - item counts per status, and per status within each type.</summary>
    public class ItemStatusStatsViewModel
    {
        public IReadOnlyList<StatusCount> CountsByStatus { get; init; } = [];

        public IReadOnlyList<TypeStatusBreakdown> BreakdownByType { get; init; } = [];

        /// <summary>Stock distribution across branches - one row per branch (all branches, regardless of the dashboard's own branch filter being empty).</summary>
        public IReadOnlyList<BranchStatusBreakdown> BreakdownByBranch { get; init; } = [];

        public int AwayFromHomeCount { get; init; }

        /// <summary>Items currently at a branch other than their home branch.</summary>
        public IReadOnlyList<AwayFromHomeItem> AwayFromHomeItems { get; init; } = [];

        public record StatusCount(ItemStatus Status, int Count);

        public record TypeStatusBreakdown(string TypeLabel, int Available, int Borrowed, int Damaged, int Destroyed);

        public record BranchStatusBreakdown(string BranchName, int Available, int Borrowed, int Damaged, int Destroyed);

        public record AwayFromHomeItem(string ItemName, string LibraryCode, string HomeBranchName, string CurrentBranchName);
    }
}
