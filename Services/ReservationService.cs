using LibrarySystem.Data;
using LibrarySystem.Models;
using LibrarySystem.Models.Enums;
using LibrarySystem.Models.ViewModels;
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

            // Position is derived from the queue's actual state, not counted -
            // see RenumberQueueAsync. A count would just perpetuate any existing
            // drift instead of correcting it.
            var reservation = new Reservation
            {
                ItemId = item.Id,
                BorrowerId = borrower.Id,
                PlacedDate = clock.Today,
                QueuePosition = 0,
                Status = ReservationStatus.Waiting
            };

            context.Reservations.Add(reservation);
            await context.SaveChangesAsync();

            await RenumberQueueAsync(itemId);
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
            await context.SaveChangesAsync();

            await RenumberQueueAsync(reservation.ItemId);
            await context.SaveChangesAsync();

            return ReservationResult.Success(reservation, $"Reservation for '{reservation.Item!.Name}' cancelled.");
        }

        /// <inheritdoc />
        public async Task<ReservationResult> MarkCollectedAsync(int reservationId)
        {
            var reservation = await context.Reservations.Include(r => r.Item).FirstOrDefaultAsync(r => r.Id == reservationId);
            if (reservation is null)
            {
                return ReservationResult.Failure("Reservation not found.");
            }

            if (reservation.Status != ReservationStatus.Ready)
            {
                return ReservationResult.Failure($"This reservation is {reservation.Status}, not Ready, and can't be marked collected.");
            }

            reservation.Status = ReservationStatus.Collected;
            await context.SaveChangesAsync();

            await RenumberQueueAsync(reservation.ItemId);
            await context.SaveChangesAsync();

            return ReservationResult.Success(reservation, $"Reservation for '{reservation.Item!.Name}' marked collected.");
        }

        /// <summary>
        /// Re-derives 1-based queue positions for every still-active (Waiting or
        /// Ready) reservation on an item, ordered by PlacedDate (first reserved,
        /// first in line) with Id as a tiebreaker. The single place positions are
        /// ever assigned - every path that can change who's active on an item
        /// (place, cancel, collect, expire, promote) calls this afterward, so
        /// positions are always rebuilt from an independent fact rather than
        /// trusted or incrementally patched - they can't drift, no matter how a
        /// reservation entered or left the active set. Queries the database, so
        /// callers must save whatever status change led here *before* calling
        /// this, not after - otherwise this sees the pre-change state.
        /// </summary>
        private async Task RenumberQueueAsync(int itemId)
        {
            var active = await context.Reservations
                .Where(r => r.ItemId == itemId && (r.Status == ReservationStatus.Waiting || r.Status == ReservationStatus.Ready))
                .OrderBy(r => r.PlacedDate)
                .ThenBy(r => r.Id)
                .ToListAsync();

            for (var i = 0; i < active.Count; i++)
            {
                active[i].QueuePosition = i + 1;
            }
        }

        /// <inheritdoc />
        public async Task<List<ReservationQueueEntryViewModel>> GetQueueAsync(int itemId)
        {
            var reservations = await context.Reservations
                .Include(r => r.Item)
                .Include(r => r.Borrower)
                .Where(r => r.ItemId == itemId && (r.Status == ReservationStatus.Waiting || r.Status == ReservationStatus.Ready))
                .OrderBy(r => r.QueuePosition)
                .ToListAsync();

            return reservations.Select(ToEntry).ToList();
        }

        /// <inheritdoc />
        public async Task<List<ReservationQueueEntryViewModel>> GetActiveReservationsAsync(ReservationStatus? statusFilter)
        {
            var query = context.Reservations
                .Include(r => r.Item)
                .Include(r => r.Borrower)
                .Where(r => r.Status == ReservationStatus.Waiting || r.Status == ReservationStatus.Ready);

            if (statusFilter is { } status)
            {
                query = query.Where(r => r.Status == status);
            }

            // Grouped by item (by name, then id as a tiebreaker for same-named
            // items), then by position within that item's queue - not by
            // PlacedDate or QueuePosition alone, either of which would interleave
            // unrelated items' queues with each other. Positions only mean
            // something within a single item's queue.
            var reservations = await query
                .OrderBy(r => r.Item!.Name)
                .ThenBy(r => r.ItemId)
                .ThenBy(r => r.QueuePosition)
                .ToListAsync();

            return reservations.Select(ToEntry).ToList();
        }

        private static ReservationQueueEntryViewModel ToEntry(Reservation r) => new()
        {
            Id = r.Id,
            ItemId = r.ItemId,
            ItemName = r.Item!.Name,
            LibraryCode = r.Item.LibraryCode,
            BorrowerName = $"{r.Borrower!.FirstName} {r.Borrower.LastName}",
            QueuePosition = r.QueuePosition,
            Status = r.Status,
            PlacedDate = r.PlacedDate,
            ExpiryDate = r.ExpiryDate
        };

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

            // Promotion doesn't remove anyone from the active set (Waiting and
            // Ready are both active), so this shouldn't change any positions -
            // but it's cheap, and calling it on every path that touches the
            // active set is what keeps positions self-correcting rather than
            // only "usually correct".
            await RenumberQueueAsync(itemId);
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

            var affectedItemIds = stale.Select(r => r.ItemId).Distinct().ToList();
            foreach (var itemId in affectedItemIds)
            {
                await RenumberQueueAsync(itemId);
            }
            await context.SaveChangesAsync();

            var promotedCount = 0;
            foreach (var itemId in affectedItemIds)
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
