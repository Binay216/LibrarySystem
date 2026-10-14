using LibrarySystem.Models.ViewModels;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace LibrarySystem.Services.Pdf
{
    public class BorrowingReportPdfDocument(BorrowingReportViewModel report, DateTime generatedAt)
        : ReportPdfDocumentBase("Borrowing Report", report.Filters, generatedAt)
    {
        protected override void ComposeContent(IContainer container)
        {
            container.Column(column =>
            {
                ComposeSummary(column,
                    ("Total Loans", report.TotalLoans.ToString()),
                    ("Active Loans", report.ActiveLoanCount.ToString()),
                    ("Overdue Loans", report.OverdueCount.ToString()));

                ComposeTable(column, "Most Borrowed Items",
                    ["Item Name", "Library Code", "Loan Count"],
                    report.MostBorrowed,
                    (table, item) =>
                    {
                        table.Cell().Padding(4).Text(item.ItemName);
                        table.Cell().Padding(4).Text(item.LibraryCode);
                        table.Cell().Padding(4).Text(item.LoanCount.ToString());
                    });

                ComposeTable(column, "Loans By Branch",
                    ["Branch Name", "Loan Count"],
                    report.BreakdownByBranch,
                    (table, b) =>
                    {
                        table.Cell().Padding(4).Text(b.BranchName);
                        table.Cell().Padding(4).Text(b.LoanCount.ToString());
                    });
            });
        }
    }
}
