using LibrarySystem.Data;
using LibrarySystem.Models;
using LibrarySystem.Models.Enums;
using LibrarySystem.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibrarySystem.Controllers
{
    /// <summary>
    /// Full CRUD over books, music and toys for the Admin role. One controller
    /// handles all three item types via <see cref="ItemFormViewModel"/> rather than
    /// a controller per type, per SPEC.md's structure.
    /// </summary>
    [Authorize(Roles = "Admin")]
    public class ItemsController(ApplicationDbContext context) : Controller
    {
        private readonly ApplicationDbContext _context = context;

        /// <summary>Lists every item across all three types with its type badge and status.</summary>
        public async Task<IActionResult> Index()
        {
            var items = await _context.Items.AsNoTracking().OrderBy(i => i.Name).ToListAsync();
            return View(items.Select(ItemListItemViewModel.FromItem));
        }

        /// <summary>Shows the full detail of a single item, including its type-specific fields.</summary>
        public async Task<IActionResult> Details(int id)
        {
            var item = await _context.Items.AsNoTracking().FirstOrDefaultAsync(i => i.Id == id);
            return item is null ? NotFound() : View(item);
        }

        /// <summary>Renders a blank create form defaulted to Book.</summary>
        public IActionResult Create()
        {
            return View(new ItemFormViewModel { ItemType = ItemTypeOption.Book, Status = ItemStatus.Available });
        }

        /// <summary>Creates a new item of the type selected in the form.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ItemFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            _context.Items.Add(BuildItem(model));

            if (!await TrySaveAsync())
            {
                return View(model);
            }

            TempData["StatusMessage"] = $"'{model.Name}' was created.";
            return RedirectToAction(nameof(Index));
        }

        /// <summary>Loads an existing item into the shared form, locked to its actual type.</summary>
        public async Task<IActionResult> Edit(int id)
        {
            var item = await _context.Items.FindAsync(id);
            return item is null ? NotFound() : View(ToFormViewModel(item));
        }

        /// <summary>Applies edited fields to an existing item.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ItemFormViewModel model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            var item = await _context.Items.FindAsync(id);
            if (item is null)
            {
                return NotFound();
            }

            // The item's concrete type never changes after creation.
            model.ItemType = ToFormViewModel(item).ItemType;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            ApplyTo(item, model);

            if (!await TrySaveAsync())
            {
                return View(model);
            }

            TempData["StatusMessage"] = $"'{model.Name}' was updated.";
            return RedirectToAction(nameof(Index));
        }

        /// <summary>Shows the delete confirmation, warning if the item has loan history.</summary>
        public async Task<IActionResult> Delete(int id)
        {
            var item = await _context.Items.AsNoTracking().FirstOrDefaultAsync(i => i.Id == id);
            if (item is null)
            {
                return NotFound();
            }

            ViewBag.HasLoanHistory = await _context.Loans.AnyAsync(l => l.ItemId == id);
            return View(item);
        }

        /// <summary>
        /// Deletes an item, unless it has loan history - in which case the delete is
        /// refused and the admin is pointed at Status = Destroyed instead. See
        /// decision_delete_with_loan_history: the Loan FKs are Restrict, and Loan is
        /// the sole data source for the manager dashboards, so history is never lost.
        /// </summary>
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var item = await _context.Items.FindAsync(id);
            if (item is null)
            {
                return NotFound();
            }

            if (await _context.Loans.AnyAsync(l => l.ItemId == id))
            {
                TempData["ErrorMessage"] = $"'{item.Name}' has loan history and can't be deleted. Set its status to Destroyed instead.";
                return RedirectToAction(nameof(Delete), new { id });
            }

            _context.Items.Remove(item);
            await _context.SaveChangesAsync();
            TempData["StatusMessage"] = $"'{item.Name}' was deleted.";
            return RedirectToAction(nameof(Index));
        }

        private static Item BuildItem(ItemFormViewModel model)
        {
            Item item = model.ItemType switch
            {
                ItemTypeOption.Book => new Book { Author = model.Author!, Genre = model.Genre! },
                ItemTypeOption.Music => new Music { Artist = model.Artist!, Year = model.Year!.Value },
                ItemTypeOption.Toy => new Toy { ToyType = model.ToyType!, RecommendedAge = model.RecommendedAge!.Value },
                _ => throw new InvalidOperationException($"Unknown item type '{model.ItemType}'.")
            };

            item.Name = model.Name;
            item.Description = model.Description;
            item.LibraryCode = model.LibraryCode;
            item.Status = model.Status;
            return item;
        }

        private static void ApplyTo(Item item, ItemFormViewModel model)
        {
            item.Name = model.Name;
            item.Description = model.Description;
            item.LibraryCode = model.LibraryCode;
            item.Status = model.Status;

            switch (item)
            {
                case Book book:
                    book.Author = model.Author!;
                    book.Genre = model.Genre!;
                    break;
                case Music music:
                    music.Artist = model.Artist!;
                    music.Year = model.Year!.Value;
                    break;
                case Toy toy:
                    toy.ToyType = model.ToyType!;
                    toy.RecommendedAge = model.RecommendedAge!.Value;
                    break;
            }
        }

        private static ItemFormViewModel ToFormViewModel(Item item)
        {
            var model = new ItemFormViewModel
            {
                Id = item.Id,
                Name = item.Name,
                Description = item.Description,
                LibraryCode = item.LibraryCode,
                Status = item.Status,
                ItemType = item switch
                {
                    Book => ItemTypeOption.Book,
                    Music => ItemTypeOption.Music,
                    Toy => ItemTypeOption.Toy,
                    _ => throw new InvalidOperationException($"Unknown item type '{item.GetType().Name}'.")
                }
            };

            switch (item)
            {
                case Book book:
                    model.Author = book.Author;
                    model.Genre = book.Genre;
                    break;
                case Music music:
                    model.Artist = music.Artist;
                    model.Year = music.Year;
                    break;
                case Toy toy:
                    model.ToyType = toy.ToyType;
                    model.RecommendedAge = toy.RecommendedAge;
                    break;
            }

            return model;
        }

        /// <summary>Saves pending changes, translating a duplicate library code into a form error.</summary>
        private async Task<bool> TrySaveAsync()
        {
            try
            {
                await _context.SaveChangesAsync();
                return true;
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError(nameof(ItemFormViewModel.LibraryCode), "That library code is already in use.");
                return false;
            }
        }
    }
}
