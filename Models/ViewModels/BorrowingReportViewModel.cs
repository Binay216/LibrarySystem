namespace LibrarySystem.Models.ViewModels
{
    /// <summary>
    /// Loan activity for a date range: every figure here is scoped to loans
    /// whose BorrowedDate falls within Filters.StartDate..EndDate. ActiveLoanCount
    /// and OverdueCount are evaluated as of now (ILibraryClock.Today), not as of
    /// EndDate - "of the loans that started in this period, how many are still
    /// outstanding today" is the meaningful question even for a past period.
    /// </summary>
    public class BorrowingReportViewModel
    {
        public required ReportFilterInfo Filters { get; init; }

        /// <summary>Loans borrowed within the period.</summary>
        public int TotalLoans { get; init; }

        /// <summary>Of those, not yet returned (as of today).</summary>
        public int ActiveLoanCount { get; init; }

        /// <summary>Of the still-active ones, how many are now past their due date.</summary>
        public int OverdueCount { get; init; }

        /// <summary>Every item borrowed during the period, ranked by loan count - not truncated to a top N, unlike the dashboard's chart.</summary>
        public IReadOnlyList<TopBorrowedItem> MostBorrowed { get; init; } = [];

        /// <summary>Loan counts per branch for the period. Only branches with at least one loan appear - an empty period means an empty list, not a zero-padded row per branch.</summary>
        public IReadOnlyList<BranchLoanBreakdown> BreakdownByBranch { get; init; } = [];

        public record TopBorrowedItem(string ItemName, string LibraryCode, int LoanCount);

        public record BranchLoanBreakdown(string BranchName, int LoanCount);
    }
}
