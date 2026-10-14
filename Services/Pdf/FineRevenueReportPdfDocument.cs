using LibrarySystem.Models.ViewModels;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace LibrarySystem.Services.Pdf
{
    public class FineRevenueReportPdfDocument(FineRevenueReportViewModel report, DateTime generatedAt)
        : ReportPdfDocumentBase("Fine Revenue Report", report.Filters, generatedAt)
    {
        protected override void ComposeContent(IContainer container)
        {
            container.Column(column =>
            {
                ComposeSummary(column,
                    ("Total Accrued", report.TotalAccrued.ToString("C")),
                    ("Total Paid", report.TotalPaid.ToString("C")),
                    ("Total Outstanding", report.TotalOutstanding.ToString("C")));

                ComposeTable(column, "Fines By Borrower",
                    ["Borrower Name", "Accrued", "Paid", "Outstanding"],
                    report.ByBorrower,
                    (table, b) =>
                    {
                        table.Cell().Padding(4).Text(b.BorrowerName);
                        table.Cell().Padding(4).Text(b.Accrued.ToString("C"));
                        table.Cell().Padding(4).Text(b.Paid.ToString("C"));
                        table.Cell().Padding(4).Text(b.Outstanding.ToString("C"));
                    });

                ComposeTable(column, "Fines By Period",
                    ["Period", "Accrued", "Paid", "Outstanding"],
                    report.ByPeriod,
                    (table, p) =>
                    {
                        table.Cell().Padding(4).Text(p.Period);
                        table.Cell().Padding(4).Text(p.Accrued.ToString("C"));
                        table.Cell().Padding(4).Text(p.Paid.ToString("C"));
                        table.Cell().Padding(4).Text(p.Outstanding.ToString("C"));
                    });
            });
        }
    }
}
