namespace LibrarySystem.Models.ViewModels
{
    /// <summary>Requirement 3a - loans over time, most borrowed, active and overdue loans.</summary>
    public class BorrowingStatsViewModel
    {
        public IReadOnlyList<MonthlyLoanCount> LoansOverTime { get; init; } = [];

        public IReadOnlyList<TopBorrowedItem> MostBorrowed { get; init; } = [];

        public int ActiveLoanCount { get; init; }

        public IReadOnlyList<ActiveLoanSummary> ActiveLoans { get; init; } = [];

        public int OverdueLoanCount { get; init; }

        public IReadOnlyList<OverdueLoanSummary> OverdueLoans { get; init; } = [];

        public record MonthlyLoanCount(string Period, int Count);

        public record TopBorrowedItem(string ItemName, string LibraryCode, int LoanCount);

        public record ActiveLoanSummary(string ItemName, string LibraryCode, string BorrowerName, DateTime DueDate);

        public record OverdueLoanSummary(string ItemName, string LibraryCode, string BorrowerName, DateTime DueDate, int DaysOverdue);
    }
}
