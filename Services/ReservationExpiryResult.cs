namespace LibrarySystem.Services
{
    /// <summary>Outcome of one <see cref="IReservationService.ExpireStaleAsync"/> pass.</summary>
    public class ReservationExpiryResult
    {
        public int ExpiredCount { get; init; }

        /// <summary>How many of the expirations above successfully promoted the next person in that item's queue.</summary>
        public int PromotedCount { get; init; }
    }
}
