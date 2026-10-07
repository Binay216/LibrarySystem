namespace LibrarySystem.Models.Enums
{
    /// <summary>What event a notification is about - each has its own template per channel, see NotificationTemplates.</summary>
    public enum NotificationType
    {
        Borrowed,
        DueSoon,
        Overdue,
        FineAccrued,
        ReservationReady,
        TransferArrived
    }
}
