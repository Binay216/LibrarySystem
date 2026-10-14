namespace LibrarySystem.Models.ViewModels
{
    /// <summary>A single row on the Reception overdue-items report.</summary>
    public class OverdueLoanViewModel
    {
        public int LoanId { get; set; }

        public string ItemName { get; set; } = string.Empty;

        public string LibraryCode { get; set; } = string.Empty;

        public string BorrowerName { get; set; } = string.Empty;

        public string BorrowerPhone { get; set; } = string.Empty;

        public string BorrowerEmail { get; set; } = string.Empty;

        public DateTime DueDate { get; set; }

        public int DaysOverdue { get; set; }

        /// <summary>Late fee if returned today in undamaged condition - see <see cref="LibrarySystem.Services.ILoanService.CalculateFine"/>.</summary>
        public decimal EstimatedFine { get; set; }
    }
}
