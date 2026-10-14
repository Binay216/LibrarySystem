using LibrarySystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibrarySystem.Controllers
{
    /// <summary>
    /// Admin-only bulk item import from a CSV. Upload -> parsed/validated preview
    /// (nothing saved yet) -> explicit confirm -> import. The confirm step never
    /// trusts the preview page's own markup: it re-parses and re-validates the
    /// original CSV text (round-tripped through a hidden field) from scratch, so
    /// a tampered form or data that changed between preview and confirm (e.g. a
    /// library code taken in the meantime) can't sneak a bad row through.
    /// </summary>
    [Authorize(Roles = "Admin")]
    public class ImportController(IImportService importService) : Controller
    {
        [HttpGet]
        public IActionResult Index() => View();

        /// <summary>Parses and validates the uploaded CSV, showing valid/invalid rows separately. Saves nothing.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Preview(IFormFile? file)
        {
            if (file is null || file.Length == 0)
            {
                ModelState.AddModelError(string.Empty, "Choose a CSV file to upload.");
                return View("Index");
            }

            if (!file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(string.Empty, "That file doesn't look like a CSV - choose a .csv file.");
                return View("Index");
            }

            using var reader = new StreamReader(file.OpenReadStream());
            var csvText = await reader.ReadToEndAsync();

            var result = await importService.PreviewAsync(csvText);
            if (result.FileError is not null)
            {
                ModelState.AddModelError(string.Empty, result.FileError);
                return View("Index");
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
    }
}
