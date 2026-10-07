namespace LibrarySystem.Models.Enums
{
    /// <summary>
    /// The lifecycle state of a reservation: waiting in the queue, ready for
    /// pickup once the item's available, then either collected or, if it sat
    /// unclaimed too long, expired. Cancelled is the borrower-initiated off-ramp
    /// from either Waiting or Ready.
    /// </summary>
    public enum ReservationStatus
    {
        Waiting,
        Ready,
        Collected,
        Expired,
        Cancelled
    }
}
