using System.Text;
using LibrarySystem.Services;
using LibrarySystem.Services.Pdf;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuestPDF.Fluent;

namespace LibrarySystem.Controllers
{
    /// <summary>
    /// CSV and PDF downloads of the manager reports (IReportService). No
    /// date-range picker view exists yet - these take startDate/endDate/branchId
    /// directly as query parameters, defaulting to the last month ending today
    /// when omitted, so the endpoints are usable before that view exists.
    /// </summary>
    [Authorize(Roles = "Manager")]
    public class ReportsController(IReportService reportService, ILibraryClock clock) : Controller
    {
        // UTF-8 with a BOM: without one, Excel can misdetect a plain UTF-8 file's
        // encoding and garble anything outside ASCII when it's opened directly
        // (double-click), rather than through Excel's "Import Data" wizard where
        // you'd pick the encoding yourself.
        private static readonly UTF8Encoding CsvEncoding = new(encoderShouldEmitUTF8Identifier: true);

        /// <summary>Downloads the borrowing report as CSV.</summary>
        [HttpGet]
        public async Task<IActionResult> Borrowing(DateTime? startDate, DateTime? endDate, int? branchId)
        {
            var (start, end) = ResolveRange(startDate, endDate);
            var report = await reportService.BorrowingReportAsync(start, end, branchId);
            return CsvFile(ReportCsvWriter.Write(report), "borrowing");
        }

        /// <summary>Downloads the fine revenue report as CSV.</summary>
        [HttpGet]
        public async Task<IActionResult> FineRevenue(DateTime? startDate, DateTime? endDate, int? branchId)
        {
            var (start, end) = ResolveRange(startDate, endDate);
            var report = await reportService.FineRevenueReportAsync(start, end, branchId);
            return CsvFile(ReportCsvWriter.Write(report), "fine-revenue");
        }

        /// <summary>Downloads the inventory health report as CSV.</summary>
        [HttpGet]
        public async Task<IActionResult> InventoryHealth(DateTime? startDate, DateTime? endDate, int? branchId)
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
