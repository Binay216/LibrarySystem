namespace LibrarySystem.Models.ViewModels
{
    /// <summary>A single row on the Reception outstanding-fines list.</summary>
    public class OutstandingFineViewModel
    {
        public int LoanId { get; set; }

        public string ItemName { get; set; } = string.Empty;

        public string LibraryCode { get; set; } = string.Empty;

        public string BorrowerName { get; set; } = string.Empty;

        public string BorrowerPhone { get; set; } = string.Empty;

        public string BorrowerEmail { get; set; } = string.Empty;

        public DateTime? ReturnedDate { get; set; }

        public decimal FineAmount { get; set; }
    }
}
