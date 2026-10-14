using LibrarySystem.Data;
using LibrarySystem.Models.ViewModels;
using LibrarySystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace LibrarySystem.Controllers
{
    /// <summary>
    /// Admin-only item import, two ways into the same catalogue: a bulk CSV
    /// upload (upload -> parsed/validated preview -> explicit confirm) and a
    /// search-and-import against an external metadata source. The CSV confirm
    /// step never trusts the preview page's own markup - it re-parses and
    /// re-validates the original CSV text (round-tripped through a hidden
    /// field) from scratch, so a tampered form or data that changed between
    /// preview and confirm (e.g. a library code taken in the meantime) can't
    /// sneak a bad row through. The metadata path follows the same principle:
    /// it re-fetches the chosen record by id rather than trusting field values
    /// posted back from the results page.
    ///
    /// This controller depends on IMetadataProvider, never MockMetadataProvider
    /// directly - swapping in a real provider (an ISBN/catalogue lookup API)
    /// only means changing the DI registration in Program.cs, nothing here.
    /// </summary>
    [Authorize(Roles = "Admin")]
    public class ImportController(IImportService importService, IMetadataProvider metadataProvider, ApplicationDbContext context) : Controller
    {
        [HttpGet]
        public async Task<IActionResult> Index(string? metadataQuery)
        {
            var results = string.IsNullOrWhiteSpace(metadataQuery)
                ? []
                : await metadataProvider.SearchAsync(metadataQuery);

            return View(new ImportIndexViewModel { MetadataQuery = metadataQuery, MetadataResults = results });
        }

        /// <summary>Parses and validates the uploaded CSV, showing valid/invalid rows separately. Saves nothing.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Preview(IFormFile? file)
        {
            if (file is null || file.Length == 0)
            {
                ModelState.AddModelError(string.Empty, "Choose a CSV file to upload.");
                return View("Index", new ImportIndexViewModel());
            }

            if (!file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(string.Empty, "That file doesn't look like a CSV - choose a .csv file.");
                return View("Index", new ImportIndexViewModel());
            }

            using var reader = new StreamReader(file.OpenReadStream());
            var csvText = await reader.ReadToEndAsync();

            var result = await importService.PreviewAsync(csvText);
            if (result.FileError is not null)
            {
                ModelState.AddModelError(string.Empty, result.FileError);
                return View("Index", new ImportIndexViewModel());
            }

            return View("Preview", new Models.ViewModels.ImportPreviewPageViewModel
            {
                RawCsv = csvText,
                ValidRows = [.. result.ValidRows],
                InvalidRows = [.. result.InvalidRows]
            });
        }

        /// <summary>Re-validates and imports the rows the admin left checked, in a single transaction.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Confirm(string rawCsv, List<int>? selectedRows)
        {
            var selected = (selectedRows ?? []).ToHashSet();
            var result = await importService.CommitAsync(rawCsv, selected);

            if (result.FileError is not null)
            {
                TempData["ErrorMessage"] = result.FileError;
            }
            else
            {
                TempData["StatusMessage"] =
                    $"Import complete: {result.Created} item(s) created, {result.Skipped} skipped, {result.Rejected} rejected.";
            }

            return RedirectToAction(nameof(Index));
        }

        /// <summary>Shows one metadata record with a form to choose a home branch and library code before importing it.</summary>
        [HttpGet]
        public async Task<IActionResult> MetadataImport(string externalId)
        {
            var record = await metadataProvider.GetByIdAsync(externalId);
            if (record is null)
            {
                return NotFound();
            }

            return View(new MetadataImportFormViewModel { Record = record, Branches = await GetBranchOptionsAsync() });
        }

        /// <summary>
        /// Re-fetches the record by id (never trusts field values posted back
        /// from the browser) and imports it, validated the same way a CSV row is:
        /// library code not already taken, branch exists.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MetadataImport(string externalId, string libraryCode, int homeBranchId)
        {
            var record = await metadataProvider.GetByIdAsync(externalId);
            if (record is null)
            {
                return NotFound();
            }

            var result = await importService.ImportMetadataRecordAsync(record, libraryCode, homeBranchId);
            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Import failed.");
                return View(new MetadataImportFormViewModel
                {
                    Record = record,
                    Branches = await GetBranchOptionsAsync(),
                    LibraryCode = libraryCode,
                    HomeBranchId = homeBranchId
                });
            }

            TempData["StatusMessage"] = $"'{record.Title}' was imported as '{libraryCode}'.";
            return RedirectToAction(nameof(Index));
        }

        private async Task<List<SelectListItem>> GetBranchOptionsAsync()
        {
            return await context.Branches
                .Where(b => b.IsActive)
                .OrderBy(b => b.Name)
                .Select(b => new SelectListItem { Value = b.Id.ToString(), Text = b.Name })
                .ToListAsync();
        }
    }
}
