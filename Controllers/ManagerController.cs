using LibrarySystem.Data;
using LibrarySystem.Models.ViewModels;
using LibrarySystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace LibrarySystem.Controllers
{
    /// <summary>Read-only statistics dashboards for the Manager role.</summary>
    [Authorize(Roles = "Manager")]
    public class ManagerController(
        IStatisticsService statisticsService,
        ApplicationDbContext context,
        INotificationSweepService notificationSweepService,
        IReservationService reservationService) : Controller
    {
        /// <summary>
        /// Shows borrowing, item status and fine statistics on one dashboard,
        /// optionally restricted to a single branch - every branch (including
        /// inactive ones, so historical stats stay reachable) by default.
        /// </summary>
        public async Task<IActionResult> Index(int? branchId)
        {
            var model = new ManagerDashboardViewModel
            {
                SelectedBranchId = branchId,
                Branches = await GetBranchOptionsAsync(),
                Borrowing = await statisticsService.GetBorrowingStatsAsync(branchId),
                ItemStatus = await statisticsService.GetItemStatusStatsAsync(branchId),
                Fines = await statisticsService.GetFineStatsAsync(branchId)
            };

            return View(model);
        }

        /// <summary>
        /// Runs the due-soon/overdue notification sweep and the reservation expiry
        /// sweep on demand, as one manager action, and reports what both did.
        /// Composed here rather than inside NotificationSweepService: due-date
        /// reminders and reservation expiry are two independent concerns that
        /// happen to both want "manual trigger, on-demand, with a summary" - this
        /// is the orchestration layer, so it's the natural place to combine them
        /// without making either service depend on the other.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SweepNotifications(int? branchId)
        {
            var notificationResult = await notificationSweepService.RunAsync();
            var expiryResult = await reservationService.ExpireStaleAsync();

            TempData["StatusMessage"] =
                $"Notification sweep complete: {notificationResult.DueSoonQueued} due-soon and {notificationResult.OverdueQueued} overdue notification(s) queued, " +
                $"{notificationResult.SkippedAlreadyNotified} already notified today, {notificationResult.Failed} failed. " +
                $"Reservations: {expiryResult.ExpiredCount} expired, {expiryResult.PromotedCount} promoted to the next person in queue.";
            return RedirectToAction(nameof(Index), new { branchId });
        }

        private async Task<List<SelectListItem>> GetBranchOptionsAsync()
        {
            return await context.Branches
                .OrderBy(b => b.Name)
                .Select(b => new SelectListItem { Value = b.Id.ToString(), Text = b.Name })
                .ToListAsync();
        }
    }
}
