namespace LibrarySystem.Models.Enums
{
    /// <summary>
    /// The lifecycle state of a branch-to-branch item transfer: requested ->
    /// in transit -> completed, with cancelled as an off-ramp.
    /// </summary>
    public enum TransferStatus
    {
        Requested,
        InTransit,
        Completed,
        Cancelled
    }
}
