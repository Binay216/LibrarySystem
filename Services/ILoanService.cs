using LibrarySystem.Models.Enums;
using LibrarySystem.Models.ViewModels;

namespace LibrarySystem.Services
{
    /// <summary>Borrowing, returning and fine calculation for reception.</summary>
    public interface ILoanService
    {
        /// <summary>
        /// Borrows an available item, identified by its library code, out to a
        /// borrower. The item must be physically at the branch this app instance is
        /// configured as (<see cref="ICurrentBranchAccessor"/>) - if it's at another
        /// branch, the failure message names which one. Records
        /// <see cref="Loan.BorrowedAtBranchId"/> as the current branch.
        /// </summary>
        Task<LoanResult> BorrowAsync(string libraryCode, int borrowerId);

        /// <summary>
        /// Returns the item currently on loan under the given library code, computing
        /// and recording its fine. <paramref name="resultingStatus"/> is the item's
        /// condition on return - Available, Damaged or Destroyed. Unlike
        /// <see cref="BorrowAsync"/>, a return is accepted regardless of which branch
        /// the item is currently at - that's deliberate, a cross-branch return is
        /// legitimate and reconciled by a transfer rather than refused here. Records
        /// <see cref="Loan.ReturnedAtBranchId"/> as the current branch, but leaves
        /// <see cref="Item.CurrentBranchId"/> untouched. If the return happens away
        /// from the item's home branch, a Requested transfer back home is raised
        /// automatically (best-effort - see <see cref="ITransferService.RaiseTransferAsync"/>;
        /// if one is already active for this item, or the item wasn't returned
        /// Available, no transfer is queued, but the return itself still succeeds).
        /// <paramref name="actingUserId"/> is attributed as that transfer's requester.
        /// </summary>
        Task<LoanResult> ReturnAsync(string libraryCode, ItemStatus resultingStatus, string actingUserId);

        /// <summary>
        /// Calculates a loan's fine from the fine policy in configuration: a late fee
        /// of policy.DailyLateFee per day between <paramref name="dueDate"/> and
        /// <paramref name="returnedDate"/> (capped at policy.MaximumFine), plus a flat
        /// surcharge if <paramref name="resultingStatus"/> is Damaged or Destroyed.
        /// The sole place fine calculation happens - see SPEC.md section 5.
        /// </summary>
        decimal CalculateFine(DateTime dueDate, DateTime returnedDate, ItemStatus resultingStatus);

        /// <summary>
        /// Lists every loan that is still out past its due date and was borrowed at
        /// the current branch, ordered by due date (most overdue first), with days
        /// overdue and an estimated fine if returned today undamaged.
        /// </summary>
        Task<List<OverdueLoanViewModel>> GetOverdueLoansAsync();

        /// <summary>Lists every returned loan with an unpaid fine, most recently returned first.</summary>
        Task<List<OutstandingFineViewModel>> GetOutstandingFinesAsync();

        /// <summary>Marks a loan's fine as paid. Fails if the loan has no fine or it is already paid.</summary>
        Task<LoanResult> MarkFinePaidAsync(int loanId);

        /// <summary>Lists every loan a borrower has ever had, most recent first.</summary>
        Task<List<LoanHistoryEntryViewModel>> GetLoanHistoryAsync(int borrowerId);
    }
}
