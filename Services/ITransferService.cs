using LibrarySystem.Models.Enums;
using LibrarySystem.Models.ViewModels;

namespace LibrarySystem.Services
{
    /// <summary>Raising and progressing branch-to-branch item transfers.</summary>
    public interface ITransferService
    {
        /// <summary>
        /// Requests moving an item to another branch. Fails if the item isn't
        /// Available, is already at the destination, the destination isn't a valid
        /// active branch, or the item already has an active (non-Completed,
        /// non-Cancelled) transfer - an item can only be mid-transfer once at a time.
        /// </summary>
        Task<TransferResult> RaiseTransferAsync(int itemId, int toBranchId, string reason, string requestedByUserId);

        /// <summary>
        /// Moves a Requested transfer to InTransit: the branch shipping the item
        /// marks it as now in transit. Only the source branch (<paramref
        /// name="actingBranchId"/> must equal the transfer's FromBranchId) can do
        /// this, and only while it's still Requested.
        /// </summary>
        Task<TransferResult> MarkInTransitAsync(int transferId, int actingBranchId);

        /// <summary>
        /// Completes an InTransit transfer: moves the item's CurrentBranchId to the
        /// destination and stamps CompletedDate. Only the destination branch
        /// (<paramref name="actingBranchId"/> must equal the transfer's ToBranchId)
        /// can do this, and only once the transfer is InTransit.
        /// </summary>
        Task<TransferResult> CompleteAsync(int transferId, int actingBranchId);

        /// <summary>Cancels a transfer that is still Requested or InTransit. Fails if it's already Completed or Cancelled.</summary>
        Task<TransferResult> CancelAsync(int transferId);

        /// <summary>
        /// The current branch's transfer queue, split into incoming (destination is
        /// this branch) and outgoing (origin is this branch). With no filter, shows
        /// only the open statuses (Requested, InTransit); passing a status narrows to
        /// just that one, which doubles as the history view for Completed/Cancelled.
        /// </summary>
        Task<TransferQueueViewModel> GetQueueAsync(TransferStatus? statusFilter);
    }
}
