using LibrarySystem.Models.ViewModels;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace LibrarySystem.Services.Pdf
{
    public class InventoryHealthReportPdfDocument(InventoryHealthReportViewModel report, DateTime generatedAt)
        : ReportPdfDocumentBase("Inventory Health Report", report.Filters, generatedAt)
    {
        protected override void ComposeContent(IContainer container)
        {
            container.Column(column =>
            {
                ComposeTable(column, "Counts By Status",
                    ["Status", "Count"],
                    report.CountsByStatus,
                    (table, s) =>
                    {
                        table.Cell().Padding(4).Text(s.Status.ToString());
                        table.Cell().Padding(4).Text(s.Count.ToString());
                    });

                ComposeTable(column, "Breakdown By Type",
                    ["Type", "Available", "Borrowed", "Damaged", "Destroyed"],
                    report.BreakdownByType,
                    (table, t) =>
                    {
                        table.Cell().Padding(4).Text(t.TypeLabel);
                        table.Cell().Padding(4).Text(t.Available.ToString());
                        table.Cell().Padding(4).Text(t.Borrowed.ToString());
                        table.Cell().Padding(4).Text(t.Damaged.ToString());
                        table.Cell().Padding(4).Text(t.Destroyed.ToString());
                    });

                ComposeTable(column, "Breakdown By Branch",
                    ["Branch", "Available", "Borrowed", "Damaged", "Destroyed"],
                    report.BreakdownByBranch,
                    (table, b) =>
                    {
                        table.Cell().Padding(4).Text(b.BranchName);
                        table.Cell().Padding(4).Text(b.Available.ToString());
                        table.Cell().Padding(4).Text(b.Borrowed.ToString());
                        table.Cell().Padding(4).Text(b.Damaged.ToString());
                        table.Cell().Padding(4).Text(b.Destroyed.ToString());
                    });

                ComposeTable(column, $"Items Away From Home Branch ({report.AwayFromHomeCount})",
                    ["Item Name", "Library Code", "Home Branch", "Current Branch"],
                    report.AwayFromHomeItems,
                    (table, i) =>
                    {
                        table.Cell().Padding(4).Text(i.ItemName);
                        table.Cell().Padding(4).Text(i.LibraryCode);
                        table.Cell().Padding(4).Text(i.HomeBranchName);
                        table.Cell().Padding(4).Text(i.CurrentBranchName);
                    });

                ComposeTable(column, "Damaged Items",
                    ["Item Name", "Library Code", "Current Branch"],
                    report.DamagedItems,
                    (table, i) =>
                    {
                        table.Cell().Padding(4).Text(i.ItemName);
                        table.Cell().Padding(4).Text(i.LibraryCode);
                        table.Cell().Padding(4).Text(i.CurrentBranchName);
                    });

                ComposeTable(column, "Destroyed Items",
                    ["Item Name", "Library Code", "Current Branch"],
                    report.DestroyedItems,
                    (table, i) =>
                    {
                        table.Cell().Padding(4).Text(i.ItemName);
                        table.Cell().Padding(4).Text(i.LibraryCode);
                        table.Cell().Padding(4).Text(i.CurrentBranchName);
                    });
            });
        }
    }
}
