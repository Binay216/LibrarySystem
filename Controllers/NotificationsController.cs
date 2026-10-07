using LibrarySystem.Data;
using LibrarySystem.Models.Enums;
using LibrarySystem.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibrarySystem.Controllers
{
    /// <summary>
    /// Read-only report of every notification sent - the Manager role owns
    /// reporting, and this is a reporting view, not an operational one. No
    /// create, edit or delete: a notification is a record of what was sent.
    /// </summary>
    [Authorize(Roles = "Manager")]
    public class NotificationsController(ApplicationDbContext context) : Controller
    {
        private const int PageSize = 20;

        /// <summary>
        /// Every notification, newest first, filterable by channel/type/status/date
        /// range (combined with AND) and paged. The filter parameters are named to
        /// match NotificationListViewModel's ChannelFilter/TypeFilter/StatusFilter
        /// properties (case-insensitively) - those are what the asp-for-generated
        /// &lt;select&gt; elements in the view actually submit, so the names have to
        /// line up or the value never reaches here at all.
        /// </summary>
        public async Task<IActionResult> Index(
            NotificationChannel? channelFilter,
            NotificationType? typeFilter,
            NotificationStatus? statusFilter,
            DateTime? fromDate,
            DateTime? toDate,
            int page = 1)
        {
            var query = context.Notifications.AsNoTracking().Include(n => n.Borrower).AsQueryable();

            if (channelFilter is { } channel)
            {
                query = query.Where(n => n.Channel == channel);
            }
            if (typeFilter is { } type)
            {
                query = query.Where(n => n.Type == type);
            }
            if (statusFilter is { } status)
            {
                query = query.Where(n => n.Status == status);
            }
            if (fromDate is { } from)
            {
                query = query.Where(n => n.CreatedDate.Date >= from.Date);
            }
            if (toDate is { } to)
            {
                query = query.Where(n => n.CreatedDate.Date <= to.Date);
            }

            query = query.OrderByDescending(n => n.CreatedDate);

            var totalCount = await query.CountAsync();
            page = Math.Max(1, page);

            var items = await query
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .Select(n => new NotificationListItemViewModel
                {
                    Id = n.Id,
                    CreatedDate = n.CreatedDate,
                    BorrowerName = n.Borrower!.FirstName + " " + n.Borrower.LastName,
                    Channel = n.Channel,
                    Type = n.Type,
                    Recipient = n.Recipient,
                    Subject = n.Subject,
                    Status = n.Status
                })
                .ToListAsync();

            var model = new NotificationListViewModel
            {
                ChannelFilter = channelFilter,
                TypeFilter = typeFilter,
                StatusFilter = statusFilter,
                FromDate = fromDate,
                ToDate = toDate,
                Page = page,
                PageSize = PageSize,
                TotalCount = totalCount,
                Items = items
            };

            return View(model);
        }

        /// <summary>The full rendered notification - Body can be up to 2000 characters, too long for a table row.</summary>
        public async Task<IActionResult> Details(int id)
        {
            var notification = await context.Notifications
                .AsNoTracking()
                .Include(n => n.Borrower)
                .FirstOrDefaultAsync(n => n.Id == id);

            return notification is null ? NotFound() : View(notification);
        }
    }
}
