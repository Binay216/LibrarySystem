using LibrarySystem.Models;
using LibrarySystem.Models.Enums;
using LibrarySystem.Models.ViewModels;

namespace LibrarySystem.Services
{
    /// <summary>Placing, queuing and cancelling item reservations.</summary>
    public interface IReservationService
    {
        /// <summary>
        /// Reserves an item for a borrower. Only allowed when the item's status is
        /// Borrowed or Damaged - an Available item should be borrowed directly
        /// rather than reserved, and a Destroyed item can't be reserved at all.
        /// Fails if the borrower already has an active (Waiting or Ready)
        /// reservation for this item - one active reservation per borrower per
        /// item. QueuePosition is derived from the queue's actual state after
        /// placing, same as every other path that changes who's active on this item.
        /// </summary>
        Task<ReservationResult> PlaceAsync(int itemId, int borrowerId);

        /// <summary>
        /// Cancels a reservation that's still Waiting or Ready, then re-derives
        /// 1-based queue positions for everyone still active on that item. Fails
        /// if the reservation has already left the active queue (Collected,
        /// Expired or already Cancelled).
        /// </summary>
        Task<ReservationResult> CancelAsync(int reservationId);

        /// <summary>
        /// Marks a Ready reservation Collected - called by LoanService once the
        /// borrow it was holding the item for has gone through - and re-derives
        /// queue positions for the rest of that item's active queue. Fails if the
        /// reservation isn't currently Ready.
        /// </summary>
        Task<ReservationResult> MarkCollectedAsync(int reservationId);

        /// <summary>The active (Waiting or Ready) reservations for an item, in queue order.</summary>
        Task<List<ReservationQueueEntryViewModel>> GetQueueAsync(int itemId);

        /// <summary>
        /// Every active (Waiting or Ready) reservation across all items, newest
        /// first by PlacedDate, optionally narrowed to just one of those two statuses.
        /// </summary>
        Task<List<ReservationQueueEntryViewModel>> GetActiveReservationsAsync(ReservationStatus? statusFilter);

        /// <summary>How many borrowers are currently Waiting (not yet Ready) for an item.</summary>
        Task<int> GetWaitingCountAsync(int itemId);

        /// <summary>
        /// The current Ready reservation for an item, if any - at most one exists
        /// at a time, since a reservation only becomes Ready once the previous
        /// Ready one has left that state (Collected, Expired or Cancelled).
        /// BorrowAsync uses this to decide whether to hold the item; once the
        /// loan goes through, it calls MarkCollectedAsync with this reservation's
        /// Id rather than mutating it directly.
        /// </summary>
        Task<Reservation?> GetReadyReservationAsync(int itemId);

        /// <summary>
        /// Promotes the first Waiting reservation (by queue position) on an item
        /// to Ready: stamps ReadyDate, sets ExpiryDate three days out, and queues
        /// a best-effort ReservationReady notification (a failure here is logged
        /// and never bubbles up). Returns false if there was no one Waiting, or
        /// if the item has since been Destroyed (nothing to collect).
        /// </summary>
        Task<bool> PromoteNextAsync(int itemId);

        /// <summary>
        /// Finds every Ready reservation past its ExpiryDate, marks it Expired,
        /// and promotes the next Waiting reservation (if any) for each affected
        /// item - which cascades its own ReservationReady notification.
        /// </summary>
        Task<ReservationExpiryResult> ExpireStaleAsync();
    }
}
