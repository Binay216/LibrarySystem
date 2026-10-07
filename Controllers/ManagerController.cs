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
    public class ManagerController(IStatisticsService statisticsService, ApplicationDbContext context, INotificationSweepService notificationSweepService) : Controller
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

        /// <summary>Runs the due-soon/overdue notification sweep on demand and reports what it did.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SweepNotifications(int? branchId)
        {
            var result = await notificationSweepService.RunAsync();
            TempData["StatusMessage"] =
                $"Notification sweep complete: {result.DueSoonQueued} due-soon and {result.OverdueQueued} overdue notification(s) queued, " +
                $"{result.SkippedAlreadyNotified} already notified today, {result.Failed} failed.";
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
