using LibrarySystem.Models.ViewModels;

namespace LibrarySystem.Services
{
    /// <summary>
    /// Builds the CSV content for each IReportService report. The filters
    /// (date range, branch) are written as leading lines above the data, so an
    /// exported file is self-describing on its own, and every data section
    /// writes its header row regardless of whether there's any data under it -
    /// an empty section becomes a header plus a "no data" line, not zero rows
    /// and not an exception.
    /// </summary>
    public static class ReportCsvWriter
    {
        public static string Write(BorrowingReportViewModel report)
        {
            var csv = new CsvWriter();
            AppendFilterHeader(csv, "Borrowing Report", report.Filters);

            csv.AddRow("Total Loans", report.TotalLoans.ToString());
            csv.AddRow("Active Loans", report.ActiveLoanCount.ToString());
            csv.AddRow("Overdue Loans", report.OverdueCount.ToString());
            csv.AddBlankLine();

            csv.AddLine("Most Borrowed Items");
            csv.AddRow("Item Name", "Library Code", "Loan Count");
            WriteRowsOrNoData(csv, report.MostBorrowed, i => csv.AddRow(i.ItemName, i.LibraryCode, i.LoanCount.ToString()));
            csv.AddBlankLine();

            csv.AddLine("Loans By Branch");
            csv.AddRow("Branch Name", "Loan Count");
            WriteRowsOrNoData(csv, report.BreakdownByBranch, b => csv.AddRow(b.BranchName, b.LoanCount.ToString()));

            return csv.Build();
        }

        public static string Write(FineRevenueReportViewModel report)
        {
            var csv = new CsvWriter();
            AppendFilterHeader(csv, "Fine Revenue Report", report.Filters);

            csv.AddRow("Total Accrued", report.TotalAccrued.ToString("0.00"));
            csv.AddRow("Total Paid", report.TotalPaid.ToString("0.00"));
            csv.AddRow("Total Outstanding", report.TotalOutstanding.ToString("0.00"));
            csv.AddBlankLine();

            csv.AddLine("Fines By Borrower");
            csv.AddRow("Borrower Name", "Accrued", "Paid", "Outstanding");
            WriteRowsOrNoData(csv, report.ByBorrower, b =>
                csv.AddRow(b.BorrowerName, b.Accrued.ToString("0.00"), b.Paid.ToString("0.00"), b.Outstanding.ToString("0.00")));
            csv.AddBlankLine();

            csv.AddLine("Fines By Period");
            csv.AddRow("Period", "Accrued", "Paid", "Outstanding");
            WriteRowsOrNoData(csv, report.ByPeriod, p =>
                csv.AddRow(p.Period, p.Accrued.ToString("0.00"), p.Paid.ToString("0.00"), p.Outstanding.ToString("0.00")));

            return csv.Build();
        }

        public static string Write(InventoryHealthReportViewModel report)
        {
            var csv = new CsvWriter();
            AppendFilterHeader(csv, "Inventory Health Report", report.Filters);

            csv.AddLine("Counts By Status");
            csv.AddRow("Status", "Count");
            WriteRowsOrNoData(csv, report.CountsByStatus, s => csv.AddRow(s.Status.ToString(), s.Count.ToString()));
            csv.AddBlankLine();

            csv.AddLine("Breakdown By Type");
            csv.AddRow("Type", "Available", "Borrowed", "Damaged", "Destroyed");
            WriteRowsOrNoData(csv, report.BreakdownByType, t =>
                csv.AddRow(t.TypeLabel, t.Available.ToString(), t.Borrowed.ToString(), t.Damaged.ToString(), t.Destroyed.ToString()));
            csv.AddBlankLine();

            csv.AddLine("Breakdown By Branch");
            csv.AddRow("Branch", "Available", "Borrowed", "Damaged", "Destroyed");
            WriteRowsOrNoData(csv, report.BreakdownByBranch, b =>
                csv.AddRow(b.BranchName, b.Available.ToString(), b.Borrowed.ToString(), b.Damaged.ToString(), b.Destroyed.ToString()));
            csv.AddBlankLine();

            csv.AddRow("Items Away From Home Branch", report.AwayFromHomeCount.ToString());
            csv.AddRow("Item Name", "Library Code", "Home Branch", "Current Branch");
            WriteRowsOrNoData(csv, report.AwayFromHomeItems, i =>
                csv.AddRow(i.ItemName, i.LibraryCode, i.HomeBranchName, i.CurrentBranchName));
            csv.AddBlankLine();

            csv.AddLine("Damaged Items");
            csv.AddRow("Item Name", "Library Code", "Current Branch");
            WriteRowsOrNoData(csv, report.DamagedItems, i => csv.AddRow(i.ItemName, i.LibraryCode, i.CurrentBranchName));
            csv.AddBlankLine();

            csv.AddLine("Destroyed Items");
            csv.AddRow("Item Name", "Library Code", "Current Branch");
            WriteRowsOrNoData(csv, report.DestroyedItems, i => csv.AddRow(i.ItemName, i.LibraryCode, i.CurrentBranchName));

            return csv.Build();
        }

        private static void AppendFilterHeader(CsvWriter csv, string reportName, ReportFilterInfo filters)
        {
            csv.AddRow("Report", reportName);
            csv.AddRow("Date Range", filters.StartDate.ToString("yyyy-MM-dd"), filters.EndDate.ToString("yyyy-MM-dd"));
            csv.AddRow("Branch", filters.BranchName);
            csv.AddBlankLine();
        }

        private static void WriteRowsOrNoData<T>(CsvWriter csv, IReadOnlyList<T> items, Action<T> writeRow)
        {
            if (items.Count == 0)
            {
                csv.AddLine("No data for this period.");
                return;
            }

            foreach (var item in items)
            {
                writeRow(item);
            }
        }
    }
}
