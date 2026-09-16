using LibrarySystem.Data;
using LibrarySystem.Models;
using LibrarySystem.Models.ViewModels;
using LibrarySystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibrarySystem.Controllers
{
    /// <summary>Full CRUD over borrower details for the Reception role.</summary>
    [Authorize(Roles = "Reception")]
    public class BorrowersController(ApplicationDbContext context, ILoanService loanService, ILibraryClock clock) : Controller
    {
        /// <summary>Lists every borrower.</summary>
        public async Task<IActionResult> Index()
        {
            return View(await context.Borrowers.AsNoTracking().OrderBy(b => b.LastName).ToListAsync());
        }

        /// <summary>Shows a single borrower's details.</summary>
        public async Task<IActionResult> Details(int id)
        {
            var borrower = await context.Borrowers.AsNoTracking().FirstOrDefaultAsync(b => b.Id == id);
            return borrower is null ? NotFound() : View(borrower);
        }

        /// <summary>Shows every loan a borrower has ever had, most recent first.</summary>
        public async Task<IActionResult> History(int id)
        {
            var borrower = await context.Borrowers.AsNoTracking().FirstOrDefaultAsync(b => b.Id == id);
            if (borrower is null)
            {
                return NotFound();
            }

            var model = new BorrowerHistoryViewModel
            {
                Borrower = borrower,
                Loans = await loanService.GetLoanHistoryAsync(id)
            };
            return View(model);
        }

        /// <summary>Renders a blank create form.</summary>
        public IActionResult Create()
        {
            return View(new Borrower { MembershipDate = clock.Today, IsActive = true });
        }

        /// <summary>Creates a new borrower.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("FirstName,LastName,Email,Phone,Address,MembershipDate,IsActive")] Borrower borrower)
        {
            if (!ModelState.IsValid)
            {
                return View(borrower);
            }

            context.Borrowers.Add(borrower);
            await context.SaveChangesAsync();
            TempData["StatusMessage"] = $"'{borrower.FirstName} {borrower.LastName}' was added.";
            return RedirectToAction(nameof(Index));
        }

        /// <summary>Loads an existing borrower into the edit form.</summary>
        public async Task<IActionResult> Edit(int id)
        {
            var borrower = await context.Borrowers.FindAsync(id);
            return borrower is null ? NotFound() : View(borrower);
        }

        /// <summary>Applies edited fields to an existing borrower.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,FirstName,LastName,Email,Phone,Address,MembershipDate,IsActive")] Borrower borrower)
        {
            if (id != borrower.Id)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return View(borrower);
            }

            context.Update(borrower);
            await context.SaveChangesAsync();
            TempData["StatusMessage"] = $"'{borrower.FirstName} {borrower.LastName}' was updated.";
            return RedirectToAction(nameof(Index));
        }

        /// <summary>Shows the delete confirmation, warning if the borrower has loan history.</summary>
        public async Task<IActionResult> Delete(int id)
        {
            var borrower = await context.Borrowers.AsNoTracking().FirstOrDefaultAsync(b => b.Id == id);
            if (borrower is null)
            {
                return NotFound();
            }

            ViewBag.HasLoanHistory = await context.Loans.AnyAsync(l => l.BorrowerId == id);
            return View(borrower);
        }

        /// <summary>
        /// Deletes a borrower, unless they have loan history - in which case the
        /// delete is refused and reception is pointed at IsActive = false instead.
        /// See decision_delete_with_loan_history.
        /// </summary>
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var borrower = await context.Borrowers.FindAsync(id);
            if (borrower is null)
            {
                return NotFound();
            }

            if (await context.Loans.AnyAsync(l => l.BorrowerId == id))
            {
                TempData["ErrorMessage"] = $"'{borrower.FirstName} {borrower.LastName}' has loan history and can't be deleted. Set them inactive instead.";
                return RedirectToAction(nameof(Delete), new { id });
            }

            context.Borrowers.Remove(borrower);
            await context.SaveChangesAsync();
            TempData["StatusMessage"] = $"'{borrower.FirstName} {borrower.LastName}' was deleted.";
            return RedirectToAction(nameof(Index));
        }
    }
}
