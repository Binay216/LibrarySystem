using LibrarySystem.Data;
using LibrarySystem.Models;
using LibrarySystem.Models.Enums;
using LibrarySystem.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibrarySystem.Controllers
{
    /// <summary>Public, anonymous item search - requirement 4.</summary>
    [AllowAnonymous]
    public class CatalogController(ApplicationDbContext context) : Controller
    {
        /// <summary>Searches items by name/description, optionally filtered by type and availability.</summary>
        public async Task<IActionResult> Index(CatalogSearchViewModel model)
        {
            var query = context.Items.AsNoTracking().AsQueryable();

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

            var items = await query.OrderBy(i => i.Name).ToListAsync();
            model.Results = items.Select(ItemListItemViewModel.FromItem).ToList();

            return View(model);
        }
    }
}
