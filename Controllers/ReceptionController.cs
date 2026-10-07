using System.Security.Claims;
using LibrarySystem.Data;
using LibrarySystem.Models.ViewModels;
using LibrarySystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace LibrarySystem.Controllers
{
    /// <summary>Borrowing and returning items for the Reception role.</summary>
    [Authorize(Roles = "Reception")]
    public class ReceptionController(ILoanService loanService, ApplicationDbContext context) : Controller
    {
        /// <summary>Landing page with links to Borrow and Return.</summary>
        public IActionResult Index() => View();

        /// <summary>Renders the borrow form with an active-borrower picker.</summary>
        public async Task<IActionResult> Borrow()
        {
            return View(new BorrowViewModel { Borrowers = await GetActiveBorrowerOptionsAsync() });
        }

        /// <summary>Borrows the item at the given library code out to the selected borrower.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Borrow(BorrowViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.Borrowers = await GetActiveBorrowerOptionsAsync();
                return View(model);
            }

            var result = await loanService.BorrowAsync(model.LibraryCode, model.BorrowerId);
            if (!result.Succeeded)
            {
                ModelState.AddModelError(string.Empty, result.Message);
                model.Borrowers = await GetActiveBorrowerOptionsAsync();
                return View(model);
            }

            TempData["StatusMessage"] = result.Message;
            return RedirectToAction(nameof(Borrow));
        }

        /// <summary>Lists loans still out past their due date, most overdue first.</summary>
        public async Task<IActionResult> Overdue() => View(await loanService.GetOverdueLoansAsync());

        /// <summary>Lists returned loans with an unpaid fine.</summary>
        public async Task<IActionResult> Fines() => View(await loanService.GetOutstandingFinesAsync());

        /// <summary>Marks a loan's fine as paid.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkFinePaid(int loanId)
        {
            var result = await loanService.MarkFinePaidAsync(loanId);
            TempData["StatusMessage"] = result.Message;
            TempData["StatusSucceeded"] = result.Succeeded;
            return RedirectToAction(nameof(Fines));
        }

        /// <summary>Renders the return form.</summary>
        public IActionResult Return() => View(new ReturnViewModel());

        /// <summary>Returns the item at the given library code, computing its fine.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Return(ReturnViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var actingUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            var result = await loanService.ReturnAsync(model.LibraryCode, model.Condition, actingUserId);
            if (!result.Succeeded)
            {
                ModelState.AddModelError(string.Empty, result.Message);
                return View(model);
            }

            TempData["StatusMessage"] = result.Message;
            return RedirectToAction(nameof(Return));
        }

        private async Task<List<SelectListItem>> GetActiveBorrowerOptionsAsync()
        {
            return await context.Borrowers
                .Where(b => b.IsActive)
                .OrderBy(b => b.LastName)
                .Select(b => new SelectListItem
                {
                    Value = b.Id.ToString(),
                    Text = b.LastName + ", " + b.FirstName + " (" + b.Email + ")"
                })
                .ToListAsync();
        }
    }
}
