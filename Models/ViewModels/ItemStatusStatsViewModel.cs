using LibrarySystem.Models.Enums;

namespace LibrarySystem.Models.ViewModels
{
    /// <summary>Requirement 3b - item counts per status, and per status within each type.</summary>
    public class ItemStatusStatsViewModel
    {
        public IReadOnlyList<StatusCount> CountsByStatus { get; init; } = [];

        public IReadOnlyList<TypeStatusBreakdown> BreakdownByType { get; init; } = [];

        public record StatusCount(ItemStatus Status, int Count);

        public record TypeStatusBreakdown(string TypeLabel, int Available, int Borrowed, int Damaged, int Destroyed);
    }
}
