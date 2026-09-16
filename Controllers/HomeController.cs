using LibrarySystem.Models;
using LibrarySystem.Models.ViewModels;
using LibrarySystem.Services;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace LibrarySystem.Controllers
{
    /// <summary>The public landing page, plus the standard privacy and error pages.</summary>
    public class HomeController(IStatisticsService statisticsService) : Controller
    {
        /// <summary>Landing page with catalog-wide item stats and a few featured items.</summary>
        public async Task<IActionResult> Index()
        {
            var model = new HomeViewModel
            {
                ItemStatus = await statisticsService.GetItemStatusStatsAsync(),
                FeaturedItems = await statisticsService.GetFeaturedItemsAsync()
            };
            return View(model);
        }

        /// <summary>Static privacy statement page.</summary>
        public IActionResult Privacy()
        {
            return View();
        }

        /// <summary>Generic error page shown for unhandled exceptions outside Development.</summary>
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
