using LibrarySystem.Data;
using LibrarySystem.Models;
using LibrarySystem.Models.Enums;
using LibrarySystem.Models.ViewModels;
using LibrarySystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace LibrarySystem.Controllers
{
    /// <summary>Placing, viewing and cancelling item reservations for the Reception role.</summary>
    [Authorize(Roles = "Reception")]
    public class ReservationsController(IReservationService reservationService, ApplicationDbContext context, ILibraryClock clock) : Controller
    {
        /// <summary>Every active reservation across all items, newest first, optionally narrowed to Waiting or Ready.</summary>
        public async Task<IActionResult> Index(ReservationStatus? status)
        {
            var model = new ReservationListViewModel
            {
                StatusFilter = status,
                Today = clock.Today,
                Reservations = await reservationService.GetActiveReservationsAsync(status)
            };
            return View(model);
        }

        /// <summary>Renders the place-reservation form with an active-borrower picker.</summary>
        public async Task<IActionResult> Place()
        {
            return View(new PlaceReservationViewModel { Borrowers = await GetActiveBorrowerOptionsAsync() });
        }

        /// <summary>Places a reservation on a borrower's behalf for the item at the given library code.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Place(PlaceReservationViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.Borrowers = await GetActiveBorrowerOptionsAsync();
                return View(model);
            }

            var item = await context.Items.FirstOrDefaultAsync(i => i.LibraryCode == model.LibraryCode.Trim());
            if (item is null)
            {
                ModelState.AddModelError(nameof(PlaceReservationViewModel.LibraryCode), $"No item found with library code '{model.LibraryCode}'.");
                model.Borrowers = await GetActiveBorrowerOptionsAsync();
                return View(model);
            }

            var result = await reservationService.PlaceAsync(item.Id, model.BorrowerId);
            if (!result.Succeeded)
            {
                ModelState.AddModelError(string.Empty, result.Message);
                model.Borrowers = await GetActiveBorrowerOptionsAsync();
                return View(model);
            }

            TempData["StatusMessage"] = result.Message;
            TempData["StatusSucceeded"] = true;
            return RedirectToAction(nameof(Index));
        }

        /// <summary>Everyone queued for one item, in order - looked up by library code, or by item id when linked from Index.</summary>
        public async Task<IActionResult> Queue(int? itemId, string? libraryCode)
        {
            Item? item = null;
            if (itemId is { } id)
            {
                item = await context.Items.AsNoTracking().FirstOrDefaultAsync(i => i.Id == id);
                libraryCode = item?.LibraryCode;
            }
            else if (!string.IsNullOrWhiteSpace(libraryCode))
            {
                item = await context.Items.AsNoTracking().FirstOrDefaultAsync(i => i.LibraryCode == libraryCode.Trim());
            }

            var model = new ItemReservationQueueViewModel { LibraryCode = libraryCode, Today = clock.Today };

            if (item is not null)
            {
                model.ItemFound = true;
                model.ItemName = item.Name;
                model.Queue = await reservationService.GetQueueAsync(item.Id);
            }
            else if (!string.IsNullOrWhiteSpace(libraryCode))
            {
                model.ErrorMessage = $"No item found with library code '{libraryCode}'.";
            }

            return View(model);
        }

        /// <summary>Cancels a reservation - position recalculation for the rest of that item's queue happens in the service.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id, ReservationStatus? status)
        {
            var result = await reservationService.CancelAsync(id);
            TempData["StatusMessage"] = result.Message;
            TempData["StatusSucceeded"] = result.Succeeded;
            return RedirectToAction(nameof(Index), new { status });
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
