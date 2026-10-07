using LibrarySystem.Data;
using LibrarySystem.Models;
using LibrarySystem.Models.Enums;
using LibrarySystem.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace LibrarySystem.Services
{
    /// <inheritdoc cref="ITransferService" />
    public class TransferService(ApplicationDbContext context, ILibraryClock clock, ICurrentBranchAccessor currentBranchAccessor) : ITransferService
    {
        /// <inheritdoc />
        public async Task<TransferResult> RaiseTransferAsync(int itemId, int toBranchId, string reason, string requestedByUserId)
        {
            var item = await context.Items.FirstOrDefaultAsync(i => i.Id == itemId);
            if (item is null)
            {
                return TransferResult.Failure("Item not found.");
            }

            if (item.Status != ItemStatus.Available)
            {
                return TransferResult.Failure($"'{item.Name}' is not available to transfer (status: {item.Status}).");
            }

            if (item.CurrentBranchId == toBranchId)
            {
                return TransferResult.Failure($"'{item.Name}' is already at the destination branch.");
            }

            var hasActiveTransfer = await context.Transfers.AnyAsync(t =>
                t.ItemId == itemId && t.Status != TransferStatus.Completed && t.Status != TransferStatus.Cancelled);
            if (hasActiveTransfer)
            {
                return TransferResult.Failure($"'{item.Name}' already has an active transfer.");
            }

            var toBranch = await context.Branches.FindAsync(toBranchId);
            if (toBranch is null || !toBranch.IsActive)
            {
                return TransferResult.Failure("Select a valid, active destination branch.");
            }

            var transfer = new Transfer
            {
                ItemId = item.Id,
                FromBranchId = item.CurrentBranchId,
                ToBranchId = toBranchId,
                Status = TransferStatus.Requested,
                RequestedDate = clock.Today,
                RequestedByUserId = requestedByUserId,
                Reason = reason
            };

            context.Transfers.Add(transfer);
            await context.SaveChangesAsync();

            return TransferResult.Success(transfer, $"Transfer of '{item.Name}' to {toBranch.Name} requested.");
        }

        /// <inheritdoc />
        public async Task<TransferResult> MarkInTransitAsync(int transferId, int actingBranchId)
        {
            var transfer = await context.Transfers
                .Include(t => t.Item)
                .Include(t => t.FromBranch)
                .FirstOrDefaultAsync(t => t.Id == transferId);
            if (transfer is null)
            {
                return TransferResult.Failure("Transfer not found.");
            }

            if (transfer.FromBranchId != actingBranchId)
            {
                return TransferResult.Failure($"Only {transfer.FromBranch!.Name}, the source branch, can mark this transfer in transit.");
            }

            if (transfer.Status != TransferStatus.Requested)
            {
                return TransferResult.Failure($"Only a Requested transfer can be marked in transit (this one is {transfer.Status}).");
            }

            transfer.Status = TransferStatus.InTransit;
            await context.SaveChangesAsync();

            return TransferResult.Success(transfer, $"Transfer of '{transfer.Item!.Name}' marked in transit.");
        }

        /// <inheritdoc />
        public async Task<TransferResult> CompleteAsync(int transferId, int actingBranchId)
        {
            var transfer = await context.Transfers
                .Include(t => t.Item)
                .Include(t => t.ToBranch)
                .FirstOrDefaultAsync(t => t.Id == transferId);
            if (transfer is null)
            {
                return TransferResult.Failure("Transfer not found.");
            }

            if (transfer.ToBranchId != actingBranchId)
            {
                return TransferResult.Failure($"Only {transfer.ToBranch!.Name}, the destination branch, can complete this transfer.");
            }

            if (transfer.Status != TransferStatus.InTransit)
            {
                return TransferResult.Failure($"Only an in-transit transfer can be completed (this one is {transfer.Status}).");
            }

            transfer.Status = TransferStatus.Completed;
            transfer.CompletedDate = clock.Today;
            transfer.Item!.CurrentBranchId = transfer.ToBranchId;

            await context.SaveChangesAsync();

            // Deliberately not queuing a TransferArrived notification here. A
            // Transfer has no borrower on it at all - it's two branches moving an
            // item, not a per-borrower flow. RequestedByUserId is the staff member
            // who raised it (an AspNetUsers account), not a Borrower, so there's no
            // honest way to fill Notification.BorrowerId yet. TransferArrived will
            // have a real target once a reservation feature exists linking a
            // borrower to a specific item-at-a-branch request - completing the
            // matching transfer would then notify that reservation's borrower.
            return TransferResult.Success(transfer, $"'{transfer.Item.Name}' transfer completed - now at {transfer.ToBranch!.Name}.");
        }

        /// <inheritdoc />
        public async Task<TransferResult> CancelAsync(int transferId)
        {
            var transfer = await context.Transfers.Include(t => t.Item).FirstOrDefaultAsync(t => t.Id == transferId);
            if (transfer is null)
            {
                return TransferResult.Failure("Transfer not found.");
            }

            if (transfer.Status is TransferStatus.Completed or TransferStatus.Cancelled)
            {
                return TransferResult.Failure($"This transfer is already {transfer.Status} and can't be cancelled.");
            }

            transfer.Status = TransferStatus.Cancelled;
            await context.SaveChangesAsync();

            return TransferResult.Success(transfer, $"Transfer of '{transfer.Item!.Name}' cancelled.");
        }

        /// <inheritdoc />
        public async Task<TransferQueueViewModel> GetQueueAsync(TransferStatus? statusFilter)
        {
            var currentBranch = await currentBranchAccessor.GetCurrentBranchAsync();

            var query = context.Transfers
                .Include(t => t.Item)
                .Include(t => t.FromBranch)
                .Include(t => t.ToBranch)
                .Where(t => t.FromBranchId == currentBranch.Id || t.ToBranchId == currentBranch.Id);

            query = statusFilter is { } status
                ? query.Where(t => t.Status == status)
                : query.Where(t => t.Status == TransferStatus.Requested || t.Status == TransferStatus.InTransit);

            var transfers = await query.OrderByDescending(t => t.RequestedDate).ToListAsync();

            return new TransferQueueViewModel
            {
                Incoming = transfers.Where(t => t.ToBranchId == currentBranch.Id).Select(ToEntry).ToList(),
                Outgoing = transfers.Where(t => t.FromBranchId == currentBranch.Id).Select(ToEntry).ToList(),
                StatusFilter = statusFilter
            };
        }

        private static TransferQueueEntryViewModel ToEntry(Transfer transfer) => new()
        {
            Id = transfer.Id,
            ItemName = transfer.Item!.Name,
            LibraryCode = transfer.Item.LibraryCode,
            FromBranchName = transfer.FromBranch!.Name,
            ToBranchName = transfer.ToBranch!.Name,
            Status = transfer.Status,
            RequestedDate = transfer.RequestedDate,
            CompletedDate = transfer.CompletedDate,
            Reason = transfer.Reason
        };
    }
}
