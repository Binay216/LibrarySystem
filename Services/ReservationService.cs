using LibrarySystem.Data;
using LibrarySystem.Models;
using LibrarySystem.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace LibrarySystem.Services
{
    /// <inheritdoc cref="IReservationService" />
    public class ReservationService(
        ApplicationDbContext context,
        ILibraryClock clock,
        INotificationService notificationService,
        ILogger<ReservationService> logger) : IReservationService
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

        /// <inheritdoc />
        public async Task<Reservation?> GetReadyReservationAsync(int itemId)
        {
            return await context.Reservations.FirstOrDefaultAsync(r => r.ItemId == itemId && r.Status == ReservationStatus.Ready);
        }

        /// <inheritdoc />
        public async Task<bool> PromoteNextAsync(int itemId)
        {
            var next = await context.Reservations
                .Where(r => r.ItemId == itemId && r.Status == ReservationStatus.Waiting)
                .OrderBy(r => r.QueuePosition)
                .FirstOrDefaultAsync();

            if (next is null)
            {
                return false;
            }

            var item = await context.Items.Include(i => i.CurrentBranch).FirstOrDefaultAsync(i => i.Id == itemId);
            if (item is null || item.Status == ItemStatus.Destroyed)
            {
                // Nothing sensible to promote someone into - a destroyed item can
                // never be collected. The reservation is left Waiting rather than
                // silently cancelled; there's no cleanup path for this yet.
                return false;
            }

            var today = clock.Today;
            next.Status = ReservationStatus.Ready;
            next.ReadyDate = today;
            next.ExpiryDate = today.AddDays(3);

            await context.SaveChangesAsync();

            await TryQueueNotificationAsync(next.BorrowerId, NotificationType.ReservationReady, new Dictionary<string, string>
            {
                ["ItemName"] = item.Name,
                ["LibraryCode"] = item.LibraryCode,
                ["BranchName"] = item.CurrentBranch!.Name,
                ["ExpiryDate"] = next.ExpiryDate.Value.ToString("d")
            });

            return true;
        }

        /// <inheritdoc />
        public async Task<ReservationExpiryResult> ExpireStaleAsync()
        {
            var today = clock.Today;

            var stale = await context.Reservations
                .Where(r => r.Status == ReservationStatus.Ready && r.ExpiryDate != null && r.ExpiryDate.Value.Date < today)
                .ToListAsync();

            foreach (var reservation in stale)
            {
                reservation.Status = ReservationStatus.Expired;
            }

            await context.SaveChangesAsync();

            var promotedCount = 0;
            foreach (var itemId in stale.Select(r => r.ItemId).Distinct())
            {
                if (await PromoteNextAsync(itemId))
                {
                    promotedCount++;
                }
            }

            return new ReservationExpiryResult { ExpiredCount = stale.Count, PromotedCount = promotedCount };
        }

        /// <summary>
        /// Queues a notification without letting it affect the caller, mirroring
        /// LoanService's identically-named helper: any exception here is logged
        /// and swallowed rather than bubbling up into PromoteNextAsync's result.
        /// </summary>
        private async Task TryQueueNotificationAsync(int borrowerId, NotificationType type, Dictionary<string, string> tokens)
        {
            try
            {
                await notificationService.QueueAsync(borrowerId, type, tokens);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to queue a {Type} notification for borrower {BorrowerId}.", type, borrowerId);
            }
        }
    }
}
