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
    /// <summary>Public, anonymous item search - requirement 4.</summary>
    [AllowAnonymous]
    public class CatalogController(ApplicationDbContext context, IReservationService reservationService) : Controller
    {
        /// <summary>Searches items by name/description, optionally filtered by type and availability.</summary>
        public async Task<IActionResult> Index(CatalogSearchViewModel model)
        {
            var query = context.Items.AsNoTracking().Include(i => i.CurrentBranch).AsQueryable();

            if (!string.IsNullOrWhiteSpace(model.SearchTerm))
            {
                var term = model.SearchTerm.Trim();
                query = query.Where(i => EF.Functions.Like(i.Name, $"%{term}%") || EF.Functions.Like(i.Description, $"%{term}%"));
            }

            query = model.ItemType switch
            {
                ItemTypeOption.Book => query.OfType<Book>(),
                ItemTypeOption.Music => query.OfType<Music>(),
                ItemTypeOption.Toy => query.OfType<Toy>(),
                _ => query
            };

            if (model.AvailableOnly)
            {
                query = query.Where(i => i.Status == ItemStatus.Available);
            }

            if (model.BranchId is { } branchId)
            {
                query = query.Where(i => i.CurrentBranchId == branchId);
            }

            var items = await query.OrderBy(i => i.Name).ToListAsync();
            model.Results = items.Select(ItemListItemViewModel.FromItem).ToList();

            // Only items that aren't Available can have a queue, but it's just as
            // cheap to batch the lookup for every result as to filter first.
            var queueCounts = await reservationService.GetActiveReservationCountsAsync(items.Select(i => i.Id));
            foreach (var result in model.Results)
            {
                result.QueueCount = queueCounts.GetValueOrDefault(result.Id);
            }

            model.Branches = await GetActiveBranchOptionsAsync();

            return View(model);
        }

        private async Task<List<SelectListItem>> GetActiveBranchOptionsAsync()
        {
            return await context.Branches
                .Where(b => b.IsActive)
                .OrderBy(b => b.Name)
                .Select(b => new SelectListItem { Value = b.Id.ToString(), Text = b.Name })
                .ToListAsync();
        }
    }
}
