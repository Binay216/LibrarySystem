using LibrarySystem.Data;
using LibrarySystem.Models;
using LibrarySystem.Models.Enums;
using LibrarySystem.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace LibrarySystem.Services
{
    /// <inheritdoc cref="IKioskService" />
    public class KioskService(ApplicationDbContext context, ILoanService loanService, IReservationService reservationService) : IKioskService
    {
        public async Task<Borrower?> FindActiveBorrowerAsync(int cardNumber)
        {
            return await context.Borrowers.AsNoTracking().FirstOrDefaultAsync(b => b.Id == cardNumber && b.IsActive);
        }

        public async Task<Item?> FindItemByLibraryCodeAsync(string libraryCode)
        {
            return await context.Items.AsNoTracking().FirstOrDefaultAsync(i => i.LibraryCode == libraryCode.Trim());
        }

        public async Task<KioskCheckoutAttemptResult> CheckOutItemAsync(string libraryCode, int borrowerId)
        {
            var code = libraryCode.Trim();

            // Looked up independently of BorrowAsync's own validation, purely to
            // decide whether a hold can be offered if the checkout fails below -
            // never used to pre-empt or duplicate BorrowAsync's own checks.
            var item = await FindItemByLibraryCodeAsync(code);

            var result = await loanService.BorrowAsync(code, borrowerId);
            if (result.Succeeded)
            {
                return new KioskCheckoutAttemptResult
                {
                    Succeeded = true,
                    Message = result.Message,
                    CheckedOutItem = new KioskCheckedOutItem(code, item!.Name, result.Loan!.DueDate)
                };
            }

            // Same precondition ReservationService.PlaceAsync enforces (Borrowed
            // or Damaged) - offering a hold outside that would just trade one
            // failure for another the patron can't see coming.
            var holdOffer = item is not null && item.Status is ItemStatus.Borrowed or ItemStatus.Damaged
                ? code
                : null;

            return new KioskCheckoutAttemptResult { Succeeded = false, Message = result.Message, HoldOfferLibraryCode = holdOffer };
        }

        public async Task<KioskAccountSummaryViewModel> GetAccountSummaryAsync(int borrowerId)
        {
            var borrower = await context.Borrowers.AsNoTracking().FirstAsync(b => b.Id == borrowerId);
            var history = await loanService.GetLoanHistoryAsync(borrowerId);

            var reservations = await context.Reservations
                .AsNoTracking()
                .Include(r => r.Item)
                .Where(r => r.BorrowerId == borrowerId && (r.Status == ReservationStatus.Waiting || r.Status == ReservationStatus.Ready))
                .OrderBy(r => r.QueuePosition)
                .Select(r => new ReservationQueueEntryViewModel
                {
                    Id = r.Id,
                    ItemId = r.ItemId,
                    ItemName = r.Item!.Name,
                    LibraryCode = r.Item.LibraryCode,
                    BorrowerName = borrower.FirstName + " " + borrower.LastName,
                    QueuePosition = r.QueuePosition,
                    Status = r.Status,
                    PlacedDate = r.PlacedDate,
                    ExpiryDate = r.ExpiryDate
                })
                .ToListAsync();

            return new KioskAccountSummaryViewModel
            {
                BorrowerName = $"{borrower.FirstName} {borrower.LastName}",
                CurrentLoans = [.. history.Where(l => l.ReturnedDate is null)],
                OutstandingFines = [.. history.Where(l => l.ReturnedDate is not null && l.FineAmount > 0 && !l.FinePaid)],
                Reservations = reservations
            };
        }

        public Task<ReservationResult> PlaceHoldAsync(int itemId, int borrowerId) => reservationService.PlaceAsync(itemId, borrowerId);

        public async Task<List<ItemListItemViewModel>> SearchAsync(string term)
        {
            var trimmed = term.Trim();
            var items = await context.Items
                .AsNoTracking()
                .Include(i => i.CurrentBranch)
                .Where(i => EF.Functions.Like(i.Name, $"%{trimmed}%") || EF.Functions.Like(i.Description, $"%{trimmed}%"))
                .OrderBy(i => i.Name)
                .ToListAsync();

            var results = items.Select(ItemListItemViewModel.FromItem).ToList();

            var queueCounts = await reservationService.GetActiveReservationCountsAsync(items.Select(i => i.Id));
            foreach (var result in results)
            {
                result.QueueCount = queueCounts.GetValueOrDefault(result.Id);
            }

            return results;
        }
    }
}
