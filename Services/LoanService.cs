using LibrarySystem.Data;
using LibrarySystem.Models;
using LibrarySystem.Models.Enums;
using LibrarySystem.Models.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace LibrarySystem.Services
{
    /// <inheritdoc cref="ILoanService" />
    public class LoanService(
        ApplicationDbContext context,
        IOptions<FinePolicyOptions> finePolicyOptions,
        ILibraryClock clock,
        ICurrentBranchAccessor currentBranchAccessor,
        ITransferService transferService,
        INotificationService notificationService,
        IReservationService reservationService,
        ILogger<LoanService> logger) : ILoanService
    {
        private readonly FinePolicyOptions _finePolicy = finePolicyOptions.Value;

        /// <inheritdoc />
        public async Task<LoanResult> BorrowAsync(string libraryCode, int borrowerId)
        {
            var item = await context.Items.Include(i => i.CurrentBranch).FirstOrDefaultAsync(i => i.LibraryCode == libraryCode.Trim());
            if (item is null)
            {
                return LoanResult.Failure($"No item found with library code '{libraryCode}'.");
            }

            var currentBranch = await currentBranchAccessor.GetCurrentBranchAsync();
            if (item.CurrentBranchId != currentBranch.Id)
            {
                return LoanResult.Failure($"'{item.Name}' is at {item.CurrentBranch!.Name}, not {currentBranch.Name}.");
            }

            if (item.Status != ItemStatus.Available)
            {
                return LoanResult.Failure($"'{item.Name}' is not available to borrow (status: {item.Status}).");
            }

            var borrower = await context.Borrowers.FindAsync(borrowerId);
            if (borrower is null || !borrower.IsActive)
            {
                return LoanResult.Failure("Select a valid, active borrower.");
            }

            // Hold the item for whoever it's Ready for - unless that's the same
            // borrower doing the borrowing, in which case this is them collecting
            // it, not jumping the queue.
            var readyReservation = await reservationService.GetReadyReservationAsync(item.Id);
            if (readyReservation is not null && readyReservation.BorrowerId != borrower.Id)
            {
                return LoanResult.Failure($"'{item.Name}' is on hold for another borrower's reservation.");
            }

            var today = clock.Today;
            var loan = new Loan
            {
                ItemId = item.Id,
                BorrowerId = borrower.Id,
                BorrowedAtBranchId = currentBranch.Id,
                BorrowedDate = today,
                DueDate = today.AddDays(_finePolicy.LoanPeriodDays)
            };

            item.Status = ItemStatus.Borrowed;
            context.Loans.Add(loan);
            await context.SaveChangesAsync();

            // Same borrower the hold was for (the check above ruled out anyone
            // else) - this fulfils it. Goes through MarkCollectedAsync rather
            // than setting the status here directly, so the queue-position
            // renumbering it does stays encapsulated in ReservationService
            // instead of being duplicated at every call site. Best-effort, same
            // as the notification below: the loan above has already succeeded.
            if (readyReservation is not null)
            {
                await TryMarkReservationCollectedAsync(readyReservation.Id);
            }

            await TryQueueNotificationAsync(borrower.Id, NotificationType.Borrowed, new Dictionary<string, string>
            {
                ["ItemName"] = item.Name,
                ["LibraryCode"] = item.LibraryCode,
                ["DueDate"] = loan.DueDate.ToString("d"),
                ["BranchName"] = currentBranch.Name
            }, loan.Id);

            var message = $"'{item.Name}' borrowed by {borrower.FirstName} {borrower.LastName}. Due back {loan.DueDate:d}.";
            if (readyReservation is not null)
            {
                message += " Reservation fulfilled.";
            }

            return LoanResult.Success(loan, message);
        }

        /// <inheritdoc />
        public async Task<LoanResult> ReturnAsync(string libraryCode, ItemStatus resultingStatus, string actingUserId)
        {
            var item = await context.Items.FirstOrDefaultAsync(i => i.LibraryCode == libraryCode.Trim());
            if (item is null)
            {
                return LoanResult.Failure($"No item found with library code '{libraryCode}'.");
            }

            var loan = await context.Loans
                .Where(l => l.ItemId == item.Id && l.ReturnedDate == null)
                .OrderByDescending(l => l.BorrowedDate)
                .FirstOrDefaultAsync();

            if (loan is null)
            {
                return LoanResult.Failure($"'{item.Name}' is not currently on loan.");
            }

            // Deliberately no branch check here - a return at a different branch
            // from the item's CurrentBranchId is allowed. CurrentBranchId is left
            // as-is; reconciling it is a transfer, not something a return does.
            var currentBranch = await currentBranchAccessor.GetCurrentBranchAsync();
            var today = clock.Today;
            loan.ReturnedDate = today;
            loan.ReturnedAtBranchId = currentBranch.Id;
            loan.FineAmount = CalculateFine(loan.DueDate, today, resultingStatus);
            item.Status = resultingStatus;

            await context.SaveChangesAsync();

            // Best-effort: if this wasn't returned at the item's home branch, queue
            // it to go back. RaiseTransferAsync refuses on its own (e.g. an already
            // active transfer, or the item not being Available) - either way, the
            // return above has already succeeded and isn't affected.
            if (currentBranch.Id != item.HomeBranchId)
            {
                await transferService.RaiseTransferAsync(item.Id, item.HomeBranchId, "Returning to home branch", actingUserId);
            }

            // Only an Available return is collectable - damaged or destroyed
            // items can't be handed to the next person in the queue. Wrapped the
            // same way as the notification triggers: the return above has
            // already succeeded and must stay that way regardless of what
            // happens here.
            if (resultingStatus == ItemStatus.Available)
            {
                await TryPromoteNextReservationAsync(item.Id);
            }

            if (loan.FineAmount > 0)
            {
                var reasons = new List<string>();
                if (today > loan.DueDate)
                {
                    reasons.Add("late");
                }
                if (resultingStatus == ItemStatus.Damaged)
                {
                    reasons.Add("damaged");
                }
                if (resultingStatus == ItemStatus.Destroyed)
                {
                    reasons.Add("destroyed");
                }

                await TryQueueNotificationAsync(loan.BorrowerId, NotificationType.FineAccrued, new Dictionary<string, string>
                {
                    ["ItemName"] = item.Name,
                    ["FineAmount"] = loan.FineAmount.ToString("C"),
                    ["Reason"] = string.Join(", ", reasons)
                }, loan.Id);
            }

            var message = loan.FineAmount > 0
                ? $"'{item.Name}' returned. Fine due: {loan.FineAmount:C}."
                : $"'{item.Name}' returned. No fine due.";

            return LoanResult.Success(loan, message);
        }

        /// <inheritdoc />
        public decimal CalculateFine(DateTime dueDate, DateTime returnedDate, ItemStatus resultingStatus)
        {
            var daysLate = Math.Max(0, (returnedDate.Date - dueDate.Date).Days);
            var lateFee = Math.Min(daysLate * _finePolicy.DailyLateFee, _finePolicy.MaximumFine);

            var surcharge = resultingStatus switch
            {
                ItemStatus.Damaged => _finePolicy.DamagedSurcharge,
                ItemStatus.Destroyed => _finePolicy.DestroyedReplacementFee,
                _ => 0m
            };

            return lateFee + surcharge;
        }

        /// <inheritdoc />
        public async Task<List<OverdueLoanViewModel>> GetOverdueLoansAsync()
        {
            var today = clock.Today;
            var currentBranch = await currentBranchAccessor.GetCurrentBranchAsync();

            var overdueLoans = await context.Loans
                .Include(l => l.Item)
                .Include(l => l.Borrower)
                .Where(l => l.ReturnedDate == null && l.DueDate < today && l.BorrowedAtBranchId == currentBranch.Id)
                .OrderBy(l => l.DueDate)
                .ToListAsync();

            return overdueLoans.Select(loan => new OverdueLoanViewModel
            {
                LoanId = loan.Id,
                ItemName = loan.Item!.Name,
                LibraryCode = loan.Item.LibraryCode,
                BorrowerName = $"{loan.Borrower!.FirstName} {loan.Borrower.LastName}",
                BorrowerPhone = loan.Borrower.Phone ?? string.Empty,
                BorrowerEmail = loan.Borrower.Email,
                DueDate = loan.DueDate,
                DaysOverdue = (today - loan.DueDate).Days,
                EstimatedFine = CalculateFine(loan.DueDate, today, ItemStatus.Available)
            }).ToList();
        }

        /// <inheritdoc />
        public async Task<List<OutstandingFineViewModel>> GetOutstandingFinesAsync()
        {
            var unpaidLoans = await context.Loans
                .Include(l => l.Item)
                .Include(l => l.Borrower)
                .Where(l => l.FineAmount > 0 && !l.FinePaid)
                .OrderByDescending(l => l.ReturnedDate)
                .ToListAsync();

            return unpaidLoans.Select(loan => new OutstandingFineViewModel
            {
                LoanId = loan.Id,
                ItemName = loan.Item!.Name,
                LibraryCode = loan.Item.LibraryCode,
                BorrowerName = $"{loan.Borrower!.FirstName} {loan.Borrower.LastName}",
                BorrowerPhone = loan.Borrower.Phone ?? string.Empty,
                BorrowerEmail = loan.Borrower.Email,
                ReturnedDate = loan.ReturnedDate,
                FineAmount = loan.FineAmount
            }).ToList();
        }

        /// <inheritdoc />
        public async Task<LoanResult> MarkFinePaidAsync(int loanId)
        {
            var loan = await context.Loans.Include(l => l.Item).FirstOrDefaultAsync(l => l.Id == loanId);
            if (loan is null)
            {
                return LoanResult.Failure("Loan not found.");
            }

            if (loan.FineAmount <= 0)
            {
                return LoanResult.Failure("This loan has no fine to pay.");
            }

            if (loan.FinePaid)
            {
                return LoanResult.Failure("This fine is already marked as paid.");
            }

            loan.FinePaid = true;
            await context.SaveChangesAsync();

            return LoanResult.Success(loan, $"Fine of {loan.FineAmount:C} for '{loan.Item!.Name}' marked as paid.");
        }

        /// <inheritdoc />
        public async Task<List<LoanHistoryEntryViewModel>> GetLoanHistoryAsync(int borrowerId)
        {
            var today = clock.Today;

            var loans = await context.Loans
                .Include(l => l.Item)
                .Where(l => l.BorrowerId == borrowerId)
                .OrderByDescending(l => l.BorrowedDate)
                .ToListAsync();

            return loans.Select(loan => new LoanHistoryEntryViewModel
            {
                ItemName = loan.Item!.Name,
                LibraryCode = loan.Item.LibraryCode,
                BorrowedDate = loan.BorrowedDate,
                DueDate = loan.DueDate,
                ReturnedDate = loan.ReturnedDate,
                IsOverdue = loan.ReturnedDate == null && loan.DueDate < today,
                FineAmount = loan.FineAmount,
                FinePaid = loan.FinePaid
            }).ToList();
        }

        /// <summary>
        /// Queues a notification without letting it affect the caller: borrowing or
        /// returning an item must succeed regardless of whether notifying the
        /// borrower does. Any exception - a missing template case, a DB hiccup,
        /// anything - is logged and swallowed here rather than bubbling up.
        /// </summary>
        private async Task TryQueueNotificationAsync(int borrowerId, NotificationType type, Dictionary<string, string> tokens, int? loanId = null)
        {
            try
            {
                await notificationService.QueueAsync(borrowerId, type, tokens, loanId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to queue a {Type} notification for borrower {BorrowerId}.", type, borrowerId);
            }
        }

        /// <summary>
        /// Promotes the next reservation for an item without letting a failure
        /// here affect the return that triggered it - same reasoning as
        /// <see cref="TryQueueNotificationAsync"/>, just one level up: this also
        /// protects against a failure that isn't itself a notification failure
        /// (e.g. the save inside PromoteNextAsync), not just the notification it queues.
        /// </summary>
        private async Task TryPromoteNextReservationAsync(int itemId)
        {
            try
            {
                await reservationService.PromoteNextAsync(itemId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to promote the next reservation for item {ItemId}.", itemId);
            }
        }

        /// <summary>Marks a reservation collected without letting a failure here affect the borrow that fulfilled it - same reasoning as <see cref="TryPromoteNextReservationAsync"/>.</summary>
        private async Task TryMarkReservationCollectedAsync(int reservationId)
        {
            try
            {
                await reservationService.MarkCollectedAsync(reservationId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to mark reservation {ReservationId} as collected.", reservationId);
            }
        }
    }
}
