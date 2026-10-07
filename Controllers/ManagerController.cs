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
    public class ManagerController(IStatisticsService statisticsService, ApplicationDbContext context) : Controller
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

        private async Task<List<SelectListItem>> GetBranchOptionsAsync()
        {
            return await context.Branches
                .OrderBy(b => b.Name)
                .Select(b => new SelectListItem { Value = b.Id.ToString(), Text = b.Name })
                .ToListAsync();
        }
    }
}
