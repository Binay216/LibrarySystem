namespace LibrarySystem.Models.ViewModels
{
    /// <summary>Requirement 3c - accrued, paid, outstanding and average fines.</summary>
    public class FineStatsViewModel
    {
        public decimal TotalAccrued { get; init; }

        public decimal TotalPaid { get; init; }

        public decimal TotalOutstanding { get; init; }

        public decimal AverageFinePerReturnedLoan { get; init; }

        public int ReturnedLoanCount { get; init; }

        public int FinedLoanCount { get; init; }
    }
}
