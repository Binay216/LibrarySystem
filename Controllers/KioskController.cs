using LibrarySystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibrarySystem.Controllers
{
    /// <summary>
    /// The public self-service kiosk - a fixed terminal at the branch, not a page
    /// on the website, hence [AllowAnonymous] and its own _KioskLayout (no site
    /// nav, no login links, no footer). This controller is the shell: idle screen,
    /// navigation to each action's entry screen, and a shared "coming soon" landing
    /// for the actions themselves, which are a separate task. CheckOut, Account and
    /// Search exist here only as the keypad-driven entry screens that demonstrate
    /// the shell's navigation and input pattern - none of them perform a real
    /// transaction yet.
    /// </summary>
    [AllowAnonymous]
    public class KioskController(ICurrentBranchAccessor currentBranchAccessor) : Controller
    {
        /// <summary>The idle screen - resets here after 60s of inactivity or when Cancel is pressed anywhere.</summary>
        public async Task<IActionResult> Index()
        {
            var branch = await currentBranchAccessor.GetCurrentBranchAsync();
            return View(model: branch.Name);
        }

        /// <summary>Entry screen for checking out an item - library code via the numeric keypad.</summary>
        public IActionResult CheckOut() => View();

        /// <summary>Entry screen for account lookup - card number via the numeric keypad.</summary>
        public IActionResult Account() => View();

        /// <summary>Entry screen for catalogue search - free text via the alphanumeric keypad.</summary>
        public IActionResult Search() => View();

        /// <summary>Shared placeholder landing for every action until its real transaction is built.</summary>
        public IActionResult ComingSoon(string? from) => View(model: from ?? "This feature");
    }
}
