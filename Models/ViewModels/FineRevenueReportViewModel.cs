namespace LibrarySystem.Models.ViewModels
{
    /// <summary>
    /// Fine activity for a date range, scoped to loans returned (not borrowed)
    /// within Filters.StartDate..EndDate with a non-zero fine - a fine is only
    /// ever finalised at return time, so return date is the natural anchor here
    /// (unlike BorrowingReportViewModel, which anchors on borrow date). "Paid"
    /// reflects FinePaid's current value, not when it was marked paid - Loan has
    /// no paid-date field, so "paid within the period" isn't something this data
    /// model can answer; this is "of what accrued in the period, how much is
    /// currently paid vs still outstanding, as of now".
    /// </summary>
    public class FineRevenueReportViewModel
    {
        public required ReportFilterInfo Filters { get; init; }

        public decimal TotalAccrued { get; init; }

        public decimal TotalPaid { get; init; }

        public decimal TotalOutstanding { get; init; }

        /// <summary>Only borrowers with at least one fine-bearing return in the period.</summary>
        public IReadOnlyList<BorrowerFineBreakdown> ByBorrower { get; init; } = [];

        /// <summary>Monthly breakdown across the period, by return date.</summary>
        public IReadOnlyList<PeriodFineBreakdown> ByPeriod { get; init; } = [];

        public record BorrowerFineBreakdown(string BorrowerName, decimal Accrued, decimal Paid, decimal Outstanding);

        public record PeriodFineBreakdown(string Period, decimal Accrued, decimal Paid, decimal Outstanding);
    }
}
