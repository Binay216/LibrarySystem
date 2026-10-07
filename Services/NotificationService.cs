using System.Text.RegularExpressions;
using LibrarySystem.Data;
using LibrarySystem.Models;
using LibrarySystem.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace LibrarySystem.Services
{
    /// <inheritdoc cref="INotificationService" />
    public class NotificationService(ApplicationDbContext context, ILibraryClock clock, ILogger<NotificationService> logger) : INotificationService
    {
        private static readonly Regex TokenPlaceholderPattern = new(@"\{([^{}]+)\}", RegexOptions.Compiled);

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

            var subject = Render(template.Subject, allTokens);
            var body = Render(template.Body, allTokens);

            // Safety net: a template referencing a token the caller didn't supply
            // (or vice versa) is a template/caller mismatch, not something to fail
            // on - a slightly broken notification beats a dropped one. Still send
            // it, but make the mismatch visible rather than shipping it silently.
            var missingTokens = TokenPlaceholderPattern.Matches(subject + "\n" + body)
                .Select(m => m.Groups[1].Value)
                .Distinct()
                .ToList();
            if (missingTokens.Count > 0)
            {
                logger.LogWarning(
                    "Notification template for {Type}/{Channel} has unsubstituted token(s) {MissingTokens} - template and caller have gone out of sync.",
                    type, channel, string.Join(", ", missingTokens));
            }

            var notification = new Notification
            {
                BorrowerId = borrower.Id,
                Channel = channel,
                Type = type,
                Recipient = recipient,
                Subject = subject,
                Body = body,
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
