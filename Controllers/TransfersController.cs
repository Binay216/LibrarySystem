using System.Security.Claims;
using LibrarySystem.Data;
using LibrarySystem.Models.Enums;
using LibrarySystem.Models.ViewModels;
using LibrarySystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace LibrarySystem.Controllers
{
    /// <summary>Raising and progressing branch-to-branch item transfers for the Reception role.</summary>
    [Authorize(Roles = "Reception")]
    public class TransfersController(ITransferService transferService, ApplicationDbContext context, ICurrentBranchAccessor currentBranchAccessor) : Controller
    {
        /// <summary>
        /// The current branch's transfer queue, split into incoming and outgoing. With
        /// no status filter this shows only the open ones (Requested, InTransit);
        /// filtering to Completed or Cancelled doubles as the history view.
        /// </summary>
        public async Task<IActionResult> Index(TransferStatus? status) => View(await transferService.GetQueueAsync(status));

        /// <summary>Renders the raise-transfer form with pickers for this branch's available items and other active branches.</summary>
        public async Task<IActionResult> Raise()
        {
            var model = new RaiseTransferViewModel
            {
                Items = await GetAvailableItemOptionsAsync(),
                Branches = await GetOtherActiveBranchOptionsAsync()
            };
            return View(model);
        }

        /// <summary>Raises a transfer for the selected item to the selected destination branch.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Raise(RaiseTransferViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.Items = await GetAvailableItemOptionsAsync();
                model.Branches = await GetOtherActiveBranchOptionsAsync();
                return View(model);
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            var result = await transferService.RaiseTransferAsync(model.ItemId, model.ToBranchId, model.Reason, userId);
            if (!result.Succeeded)
            {
                ModelState.AddModelError(string.Empty, result.Message);
                model.Items = await GetAvailableItemOptionsAsync();
                model.Branches = await GetOtherActiveBranchOptionsAsync();
                return View(model);
            }

            TempData["StatusMessage"] = result.Message;
            TempData["StatusSucceeded"] = true;
            return RedirectToAction(nameof(Index));
        }

        /// <summary>Marks a Requested transfer as in transit - only the source branch can do this.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkInTransit(int id)
        {
            var currentBranch = await currentBranchAccessor.GetCurrentBranchAsync();
            var result = await transferService.MarkInTransitAsync(id, currentBranch.Id);
            TempData["StatusMessage"] = result.Message;
            TempData["StatusSucceeded"] = result.Succeeded;
            return RedirectToAction(nameof(Index));
        }

        /// <summary>Completes an in-transit transfer - only the destination branch can do this.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Complete(int id)
        {
            var currentBranch = await currentBranchAccessor.GetCurrentBranchAsync();
            var result = await transferService.CompleteAsync(id, currentBranch.Id);
            TempData["StatusMessage"] = result.Message;
            TempData["StatusSucceeded"] = result.Succeeded;
            return RedirectToAction(nameof(Index));
        }

        /// <summary>Cancels a transfer that hasn't completed yet.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var result = await transferService.CancelAsync(id);
            TempData["StatusMessage"] = result.Message;
            TempData["StatusSucceeded"] = result.Succeeded;
            return RedirectToAction(nameof(Index));
        }

        /// <summary>Items physically at this branch, Available, and not already mid-transfer.</summary>
        private async Task<List<SelectListItem>> GetAvailableItemOptionsAsync()
        {
            var currentBranch = await currentBranchAccessor.GetCurrentBranchAsync();
            var activeTransferItemIds = context.Transfers
                .Where(t => t.Status != TransferStatus.Completed && t.Status != TransferStatus.Cancelled)
                .Select(t => t.ItemId);

            return await context.Items
                .Where(i => i.CurrentBranchId == currentBranch.Id
                    && i.Status == ItemStatus.Available
                    && !activeTransferItemIds.Contains(i.Id))
                .OrderBy(i => i.Name)
                .Select(i => new SelectListItem { Value = i.Id.ToString(), Text = i.Name + " (" + i.LibraryCode + ")" })
                .ToListAsync();
        }

        /// <summary>Active branches other than this one, as valid transfer destinations.</summary>
        private async Task<List<SelectListItem>> GetOtherActiveBranchOptionsAsync()
        {
            var currentBranch = await currentBranchAccessor.GetCurrentBranchAsync();
            return await context.Branches
                .Where(b => b.IsActive && b.Id != currentBranch.Id)
                .OrderBy(b => b.Name)
                .Select(b => new SelectListItem { Value = b.Id.ToString(), Text = b.Name })
                .ToListAsync();
        }
    }
}
