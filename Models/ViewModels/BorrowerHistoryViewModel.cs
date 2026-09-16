namespace LibrarySystem.Models.ViewModels
{
    /// <summary>Backs the borrower loan history page - the borrower plus every loan they've had.</summary>
    public class BorrowerHistoryViewModel
    {
        public Borrower Borrower { get; set; } = null!;

        public List<LoanHistoryEntryViewModel> Loans { get; set; } = [];
    }
}
