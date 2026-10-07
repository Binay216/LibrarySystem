using LibrarySystem.Models.Enums;

namespace LibrarySystem.Models.ViewModels
{
    /// <summary>One row in the notification report - a flattened summary of a Notification plus its borrower's name.</summary>
    public class NotificationListItemViewModel
    {
        public int Id { get; set; }

        public DateTime CreatedDate { get; set; }

        public string BorrowerName { get; set; } = string.Empty;

        public NotificationChannel Channel { get; set; }

        public NotificationType Type { get; set; }

        public string Recipient { get; set; } = string.Empty;

        public string Subject { get; set; } = string.Empty;

        public NotificationStatus Status { get; set; }
    }
}
