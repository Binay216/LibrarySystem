using LibrarySystem.Data;
using LibrarySystem.Models;
using LibrarySystem.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace LibrarySystem.Services
{
    /// <inheritdoc cref="INotificationService" />
    public class NotificationService(ApplicationDbContext context, ILibraryClock clock, ILogger<NotificationService> logger) : INotificationService
    {
        /// <inheritdoc />
        public async Task<Notification> QueueAsync(int borrowerId, NotificationType type, IReadOnlyDictionary<string, string> tokens)
        {
            var borrower = await context.Borrowers.FindAsync(borrowerId)
                ?? throw new InvalidOperationException($"Borrower {borrowerId} not found.");

            var channel = string.IsNullOrWhiteSpace(borrower.Phone) ? NotificationChannel.Email : NotificationChannel.Sms;
            // The ternary above guarantees Phone is set whenever we pick Sms here.
            var recipient = channel == NotificationChannel.Sms ? borrower.Phone! : borrower.Email;

            var template = NotificationTemplates.GetTemplate(type, channel);
            var allTokens = new Dictionary<string, string>(tokens)
            {
                ["BorrowerFirstName"] = borrower.FirstName,
                ["BorrowerLastName"] = borrower.LastName
            };

            var notification = new Notification
            {
                BorrowerId = borrower.Id,
                Channel = channel,
                Type = type,
                Recipient = recipient,
                Subject = Render(template.Subject, allTokens),
                Body = Render(template.Body, allTokens),
                CreatedDate = clock.Now,
                Status = NotificationStatus.Queued
            };

            context.Notifications.Add(notification);

            // No real email/SMS provider yet - "sending" is simulated by logging
            // the rendered notification so the simulation is visible while running,
            // then marking it sent immediately.
            logger.LogInformation(
                "[Notification] {Channel} to {Recipient} ({Type})\nSubject: {Subject}\n{Body}",
                notification.Channel, notification.Recipient, notification.Type, notification.Subject, notification.Body);

            notification.SentDate = clock.Now;
            notification.Status = NotificationStatus.Sent;

            await context.SaveChangesAsync();

            return notification;
        }

        private static string Render(string template, IReadOnlyDictionary<string, string> tokens)
        {
            foreach (var (key, value) in tokens)
            {
                template = template.Replace("{" + key + "}", value);
            }

            return template;
        }
    }
}
