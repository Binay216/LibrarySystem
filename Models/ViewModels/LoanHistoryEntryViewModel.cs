namespace LibrarySystem.Models.ViewModels
{
    /// <summary>A single loan row on a borrower's loan history page.</summary>
    public class LoanHistoryEntryViewModel
    {
        public string ItemName { get; set; } = string.Empty;

        public string LibraryCode { get; set; } = string.Empty;

        public DateTime BorrowedDate { get; set; }

        public DateTime DueDate { get; set; }

        /// <summary>Null while the item is still out.</summary>
        public DateTime? ReturnedDate { get; set; }

        /// <summary>True if still out and past its due date.</summary>
        public bool IsOverdue { get; set; }

        public decimal FineAmount { get; set; }

        public bool FinePaid { get; set; }
    }
}
