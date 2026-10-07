using LibrarySystem.Data;
using LibrarySystem.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace LibrarySystem.Services
{
    /// <inheritdoc cref="INotificationSweepService" />
    public class NotificationSweepService(
        ApplicationDbContext context,
        ILibraryClock clock,
        ILoanService loanService,
        INotificationService notificationService,
        ILogger<NotificationSweepService> logger) : INotificationSweepService
    {
        /// <inheritdoc />
        public async Task<NotificationSweepResult> RunAsync()
        {
            var today = clock.Today;
            var dueSoonCutoff = today.AddDays(3);

            var openLoans = await context.Loans
                .Include(l => l.Item)
                .Where(l => l.ReturnedDate == null)
                .ToListAsync();

            // Idempotency key: (LoanId, Type, calendar day). Fetched once up front
            // rather than queried per loan - a loan can only be in one of these two
            // states on a given day, and a notification already sent for that
            // state today means this run has nothing new to do for it. A loan still
            // overdue tomorrow gets a fresh reminder then - that's a daily reminder,
            // not a dedup cycle - this key only blocks repeats on the same day.
            var alreadyNotifiedToday = await context.Notifications
                .Where(n => n.LoanId != null
                    && n.CreatedDate.Date == today
                    && (n.Type == NotificationType.DueSoon || n.Type == NotificationType.Overdue))
                .Select(n => new { n.LoanId, n.Type })
                .ToListAsync();
            var alreadyNotifiedSet = alreadyNotifiedToday.Select(n => (n.LoanId!.Value, n.Type)).ToHashSet();

            var dueSoonQueued = 0;
            var overdueQueued = 0;
            var skipped = 0;
            var failed = 0;

            foreach (var loan in openLoans)
            {
                var isOverdue = loan.DueDate.Date < today;
                var isDueSoon = !isOverdue && loan.DueDate.Date <= dueSoonCutoff;
                if (!isOverdue && !isDueSoon)
                {
                    continue;
                }

                var type = isOverdue ? NotificationType.Overdue : NotificationType.DueSoon;
                if (alreadyNotifiedSet.Contains((loan.Id, type)))
                {
                    skipped++;
                    continue;
                }

                var tokens = new Dictionary<string, string>
                {
                    ["ItemName"] = loan.Item!.Name,
                    ["LibraryCode"] = loan.Item.LibraryCode,
                    ["DueDate"] = loan.DueDate.ToString("d")
                };

                if (isOverdue)
                {
                    tokens["DaysOverdue"] = (today - loan.DueDate.Date).Days.ToString();
                    tokens["FineAmount"] = loanService.CalculateFine(loan.DueDate, today, ItemStatus.Available).ToString("C");
                }

                try
                {
                    await notificationService.QueueAsync(loan.BorrowerId, type, tokens, loan.Id);
                    if (isOverdue)
                    {
                        overdueQueued++;
                    }
                    else
                    {
                        dueSoonQueued++;
                    }
                }
                catch (Exception ex)
                {
                    failed++;
                    logger.LogError(ex, "Failed to queue a {Type} notification for loan {LoanId}.", type, loan.Id);
                }
            }

            return new NotificationSweepResult
            {
                DueSoonQueued = dueSoonQueued,
                OverdueQueued = overdueQueued,
                SkippedAlreadyNotified = skipped,
                Failed = failed
            };
        }
    }
}
