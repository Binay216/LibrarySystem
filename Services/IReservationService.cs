using LibrarySystem.Models;

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
        /// item. QueuePosition is assigned as (active reservations already on this
        /// item) + 1, so new reservations always join at the back of the queue.
        /// </summary>
        Task<ReservationResult> PlaceAsync(int itemId, int borrowerId);

        /// <summary>
        /// Cancels a reservation that's still Waiting or Ready, then re-derives
        /// 1-based queue positions for everyone still active on that item. Fails
        /// if the reservation has already left the active queue (Collected,
        /// Expired or already Cancelled).
        /// </summary>
        Task<ReservationResult> CancelAsync(int reservationId);

        /// <summary>The active (Waiting or Ready) reservations for an item, in queue order.</summary>
        Task<List<Reservation>> GetQueueAsync(int itemId);

        /// <summary>How many borrowers are currently Waiting (not yet Ready) for an item.</summary>
        Task<int> GetWaitingCountAsync(int itemId);
    }
}
