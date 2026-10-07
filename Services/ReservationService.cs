using LibrarySystem.Data;
using LibrarySystem.Models;
using LibrarySystem.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace LibrarySystem.Services
{
    /// <inheritdoc cref="IReservationService" />
    public class ReservationService(ApplicationDbContext context, ILibraryClock clock) : IReservationService
    {
        /// <inheritdoc />
        public async Task<ReservationResult> PlaceAsync(int itemId, int borrowerId)
        {
            var item = await context.Items.FirstOrDefaultAsync(i => i.Id == itemId);
            if (item is null)
            {
                return ReservationResult.Failure("Item not found.");
            }

            if (item.Status == ItemStatus.Available)
            {
                return ReservationResult.Failure($"'{item.Name}' is available right now - borrow it instead of reserving it.");
            }

            if (item.Status == ItemStatus.Destroyed)
            {
                return ReservationResult.Failure($"'{item.Name}' has been destroyed and can't be reserved.");
            }

            // Only Borrowed or Damaged items reach here - both are legitimately
            // reservable (Damaged items still get repaired and lent out again).
            var borrower = await context.Borrowers.FindAsync(borrowerId);
            if (borrower is null || !borrower.IsActive)
            {
                return ReservationResult.Failure("Select a valid, active borrower.");
            }

            var hasActiveReservation = await context.Reservations.AnyAsync(r =>
                r.ItemId == itemId
                && r.BorrowerId == borrowerId
                && (r.Status == ReservationStatus.Waiting || r.Status == ReservationStatus.Ready));
            if (hasActiveReservation)
            {
                return ReservationResult.Failure($"{borrower.FirstName} {borrower.LastName} already has an active reservation for '{item.Name}'.");
            }

            var activeCount = await context.Reservations.CountAsync(r =>
                r.ItemId == itemId
                && (r.Status == ReservationStatus.Waiting || r.Status == ReservationStatus.Ready));

            var reservation = new Reservation
            {
                ItemId = item.Id,
                BorrowerId = borrower.Id,
                PlacedDate = clock.Today,
                QueuePosition = activeCount + 1,
                Status = ReservationStatus.Waiting
            };

            context.Reservations.Add(reservation);
            await context.SaveChangesAsync();

            return ReservationResult.Success(reservation, $"'{item.Name}' reserved for {borrower.FirstName} {borrower.LastName} - position {reservation.QueuePosition} in the queue.");
        }

        /// <inheritdoc />
        public async Task<ReservationResult> CancelAsync(int reservationId)
        {
            var reservation = await context.Reservations.Include(r => r.Item).FirstOrDefaultAsync(r => r.Id == reservationId);
            if (reservation is null)
            {
                return ReservationResult.Failure("Reservation not found.");
            }

            if (reservation.Status is not (ReservationStatus.Waiting or ReservationStatus.Ready))
            {
                return ReservationResult.Failure($"This reservation is already {reservation.Status} and can't be cancelled.");
            }

            reservation.Status = ReservationStatus.Cancelled;

            // Re-derive positions for everyone still active on this item, rather
            // than just decrementing whoever was behind the cancelled slot. Both
            // get the same end result when the queue was already consistent, but
            // re-deriving from PlacedDate order is the one that's still correct
            // even if it wasn't - it can't drift, because it never trusts the
            // existing QueuePosition values at all, just rebuilds them from an
            // independent fact (placement order). Cheap to do in full: an item's
            // reservation queue is a handful of rows, never worth optimizing into
            // a partial shift.
            var stillActive = await context.Reservations
                .Where(r => r.ItemId == reservation.ItemId
                    && r.Id != reservation.Id
                    && (r.Status == ReservationStatus.Waiting || r.Status == ReservationStatus.Ready))
                .OrderBy(r => r.PlacedDate)
                .ThenBy(r => r.Id)
                .ToListAsync();

            for (var i = 0; i < stillActive.Count; i++)
            {
                stillActive[i].QueuePosition = i + 1;
            }

            await context.SaveChangesAsync();

            return ReservationResult.Success(reservation, $"Reservation for '{reservation.Item!.Name}' cancelled.");
        }

        /// <inheritdoc />
        public async Task<List<Reservation>> GetQueueAsync(int itemId)
        {
            return await context.Reservations
                .Include(r => r.Borrower)
                .Where(r => r.ItemId == itemId && (r.Status == ReservationStatus.Waiting || r.Status == ReservationStatus.Ready))
                .OrderBy(r => r.QueuePosition)
                .ToListAsync();
        }

        /// <inheritdoc />
        public async Task<int> GetWaitingCountAsync(int itemId)
        {
            return await context.Reservations.CountAsync(r => r.ItemId == itemId && r.Status == ReservationStatus.Waiting);
        }
    }
}
