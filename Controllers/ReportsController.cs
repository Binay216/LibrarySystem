using System.Text;
using LibrarySystem.Data;
using LibrarySystem.Models.ViewModels;
using LibrarySystem.Services;
using LibrarySystem.Services.Pdf;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;

namespace LibrarySystem.Controllers
{
    /// <summary>
    /// On-screen manager reports plus their CSV/PDF downloads (IReportService).
    /// Each report has three actions sharing one name/suffix convention - e.g.
    /// Borrowing (the page), BorrowingCsv, BorrowingPdf - and all three resolve
    /// startDate/endDate/branchId through the same ResolveRange/report-service
    /// calls, so a page and the files exported from it can never show different
    /// figures for what looks like the same filters.
    /// </summary>
    [Authorize(Roles = "Manager")]
    public class ReportsController(IReportService reportService, ApplicationDbContext context, ILibraryClock clock) : Controller
    {
        // UTF-8 with a BOM: without one, Excel can misdetect a plain UTF-8 file's
        // encoding and garble anything outside ASCII when it's opened directly
        // (double-click), rather than through Excel's "Import Data" wizard where
        // you'd pick the encoding yourself.
        private static readonly UTF8Encoding CsvEncoding = new(encoderShouldEmitUTF8Identifier: true);

        /// <summary>Shows the borrowing report on screen, with CSV/PDF export links carrying the same filters.</summary>
        [HttpGet]
        public async Task<IActionResult> Borrowing(DateTime? startDate, DateTime? endDate, int? branchId)
        {
            var (start, end) = ResolveRange(startDate, endDate);
            var report = await reportService.BorrowingReportAsync(start, end, branchId);
            return View(await PageModelAsync(report, branchId));
        }

        /// <summary>Shows the fine revenue report on screen, with CSV/PDF export links carrying the same filters.</summary>
        [HttpGet]
        public async Task<IActionResult> FineRevenue(DateTime? startDate, DateTime? endDate, int? branchId)
        {
            var (start, end) = ResolveRange(startDate, endDate);
            var report = await reportService.FineRevenueReportAsync(start, end, branchId);
            return View(await PageModelAsync(report, branchId));
        }

        /// <summary>Shows the inventory health report on screen, with CSV/PDF export links carrying the same filters.</summary>
        [HttpGet]
        public async Task<IActionResult> InventoryHealth(DateTime? startDate, DateTime? endDate, int? branchId)
        {
            var (start, end) = ResolveRange(startDate, endDate);
            var report = await reportService.InventoryHealthReportAsync(start, end, branchId);
            return View(await PageModelAsync(report, branchId));
        }

        /// <summary>Downloads the borrowing report as CSV.</summary>
        [HttpGet]
        public async Task<IActionResult> BorrowingCsv(DateTime? startDate, DateTime? endDate, int? branchId)
        {
            var (start, end) = ResolveRange(startDate, endDate);
            var report = await reportService.BorrowingReportAsync(start, end, branchId);
            return CsvFile(ReportCsvWriter.Write(report), "borrowing");
        }

        /// <summary>Downloads the fine revenue report as CSV.</summary>
        [HttpGet]
        public async Task<IActionResult> FineRevenueCsv(DateTime? startDate, DateTime? endDate, int? branchId)
        {
            var (start, end) = ResolveRange(startDate, endDate);
            var report = await reportService.FineRevenueReportAsync(start, end, branchId);
            return CsvFile(ReportCsvWriter.Write(report), "fine-revenue");
        }

        /// <summary>Downloads the inventory health report as CSV.</summary>
        [HttpGet]
        public async Task<IActionResult> InventoryHealthCsv(DateTime? startDate, DateTime? endDate, int? branchId)
        {
            var (start, end) = ResolveRange(startDate, endDate);
            var report = await reportService.InventoryHealthReportAsync(start, end, branchId);
            return CsvFile(ReportCsvWriter.Write(report), "inventory-health");
        }

        /// <summary>Downloads the borrowing report as PDF.</summary>
        [HttpGet]
        public async Task<IActionResult> BorrowingPdf(DateTime? startDate, DateTime? endDate, int? branchId)
        {
            var (start, end) = ResolveRange(startDate, endDate);
            var report = await reportService.BorrowingReportAsync(start, end, branchId);
            var document = new BorrowingReportPdfDocument(report, clock.Now);
            return PdfFile(document, "borrowing");
        }

        /// <summary>Downloads the fine revenue report as PDF.</summary>
        [HttpGet]
        public async Task<IActionResult> FineRevenuePdf(DateTime? startDate, DateTime? endDate, int? branchId)
        {
            var (start, end) = ResolveRange(startDate, endDate);
            var report = await reportService.FineRevenueReportAsync(start, end, branchId);
            var document = new FineRevenueReportPdfDocument(report, clock.Now);
            return PdfFile(document, "fine-revenue");
        }

        /// <summary>Downloads the inventory health report as PDF.</summary>
        [HttpGet]
        public async Task<IActionResult> InventoryHealthPdf(DateTime? startDate, DateTime? endDate, int? branchId)
        {
            var (start, end) = ResolveRange(startDate, endDate);
            var report = await reportService.InventoryHealthReportAsync(start, end, branchId);
            var document = new InventoryHealthReportPdfDocument(report, clock.Now);
            return PdfFile(document, "inventory-health");
        }

        private (DateTime Start, DateTime End) ResolveRange(DateTime? startDate, DateTime? endDate)
        {
            var end = endDate ?? clock.Today;
            var start = startDate ?? end.AddMonths(-1);
            return (start, end);
        }

        private async Task<ReportPageViewModel<TReport>> PageModelAsync<TReport>(TReport report, int? branchId)
        {
            return new ReportPageViewModel<TReport>
            {
                Report = report,
                SelectedBranchId = branchId,
                Branches = await GetBranchOptionsAsync()
            };
        }

        private async Task<List<SelectListItem>> GetBranchOptionsAsync()
        {
            return await context.Branches
                .OrderBy(b => b.Name)
                .Select(b => new SelectListItem { Value = b.Id.ToString(), Text = b.Name })
                .ToListAsync();
        }

        private FileContentResult CsvFile(string csv, string reportName)
        {
            var fileName = $"{reportName}-{clock.Today:yyyy-MM-dd}.csv";
            return File(CsvEncoding.GetBytes(csv), "text/csv", fileName);
        }

        private FileContentResult PdfFile(QuestPDF.Infrastructure.IDocument document, string reportName)
        {
            var fileName = $"{reportName}-{clock.Today:yyyy-MM-dd}.pdf";
            return File(document.GeneratePdf(), "application/pdf", fileName);
        }
    }
}
