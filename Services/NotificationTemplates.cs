using LibrarySystem.Models.Enums;

namespace LibrarySystem.Services
{
    /// <summary>
    /// One Subject/Body template per (NotificationType, NotificationChannel) pair,
    /// with {TokenName} placeholders substituted by <see cref="NotificationService"/>.
    /// Kept as a plain C# class rather than a config file: every template is keyed
    /// by the NotificationType/NotificationChannel enums, so there's no runtime
    /// value a config file would add - and a missing case is a compile-time switch
    /// gap here, not a silent lookup miss at 2am. If librarians ever need to edit
    /// wording themselves without a deploy, that's the point to move this to a
    /// config file or a DB-backed table instead.
    /// </summary>
    public static class NotificationTemplates
    {
        public record Template(string Subject, string Body);

        public static Template GetTemplate(NotificationType type, NotificationChannel channel) => (type, channel) switch
        {
            (NotificationType.Borrowed, NotificationChannel.Email) => new(
                "You've borrowed '{ItemName}'",
                "Hi {BorrowerFirstName},\n\nYou've borrowed '{ItemName}' ({LibraryCode}). It's due back on {DueDate}.\n\nThanks for visiting us."),
            (NotificationType.Borrowed, NotificationChannel.Sms) => new(
                string.Empty,
                "You borrowed '{ItemName}', due {DueDate}."),

            (NotificationType.DueSoon, NotificationChannel.Email) => new(
                "Your loan is due soon",
                "Hi {BorrowerFirstName},\n\n'{ItemName}' ({LibraryCode}) is due back on {DueDate}. Please return or renew it soon.\n\nThanks."),
            (NotificationType.DueSoon, NotificationChannel.Sms) => new(
                string.Empty,
                "'{ItemName}' is due {DueDate}."),

            (NotificationType.Overdue, NotificationChannel.Email) => new(
                "Your loan is overdue",
                "Hi {BorrowerFirstName},\n\n'{ItemName}' ({LibraryCode}) was due on {DueDate} and is now overdue. Please return it as soon as possible to avoid further fines.\n\nThanks."),
            (NotificationType.Overdue, NotificationChannel.Sms) => new(
                string.Empty,
                "'{ItemName}' is overdue (was due {DueDate})."),

            (NotificationType.FineAccrued, NotificationChannel.Email) => new(
                "A fine has been added to your account",
                "Hi {BorrowerFirstName},\n\nA fine of {FineAmount} has been added to your account for '{ItemName}' ({LibraryCode}).\n\nThanks."),
            (NotificationType.FineAccrued, NotificationChannel.Sms) => new(
                string.Empty,
                "Fine of {FineAmount} added for '{ItemName}'."),

            (NotificationType.ReservationReady, NotificationChannel.Email) => new(
                "Your reservation is ready for pickup",
                "Hi {BorrowerFirstName},\n\n'{ItemName}' ({LibraryCode}) is ready for you to pick up at {BranchName}.\n\nThanks."),
            (NotificationType.ReservationReady, NotificationChannel.Sms) => new(
                string.Empty,
                "'{ItemName}' is ready for pickup at {BranchName}."),

            (NotificationType.TransferArrived, NotificationChannel.Email) => new(
                "An item you reserved has arrived",
                "Hi {BorrowerFirstName},\n\n'{ItemName}' ({LibraryCode}) has arrived at {BranchName}.\n\nThanks."),
            (NotificationType.TransferArrived, NotificationChannel.Sms) => new(
                string.Empty,
                "'{ItemName}' has arrived at {BranchName}."),

            _ => throw new InvalidOperationException($"No template defined for {type}/{channel}.")
        };
    }
}
