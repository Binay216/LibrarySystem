using LibrarySystem.Models.ViewModels;
using LibrarySystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibrarySystem.Controllers
{
    /// <summary>Read-only statistics dashboards for the Manager role.</summary>
    [Authorize(Roles = "Manager")]
    public class ManagerController(IStatisticsService statisticsService) : Controller
    {
        /// <summary>Shows borrowing, item status and fine statistics on one dashboard.</summary>
        public async Task<IActionResult> Index()
        {
            var model = new ManagerDashboardViewModel
            {
                Borrowing = await statisticsService.GetBorrowingStatsAsync(),
                ItemStatus = await statisticsService.GetItemStatusStatsAsync(),
                Fines = await statisticsService.GetFineStatsAsync()
            };

            return View(model);
        }
    }
}
