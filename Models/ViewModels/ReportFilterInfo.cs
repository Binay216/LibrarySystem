namespace LibrarySystem.Models.ViewModels
{
    /// <summary>
    /// The date range and branch scope a report was generated for - carried on
    /// every IReportService report so an export built from it can show what was
    /// actually filtered, not just the data.
    /// </summary>
    public class ReportFilterInfo
    {
        public DateTime StartDate { get; init; }

        public DateTime EndDate { get; init; }

        /// <summary>The filtered branch's name, or "All Branches" when no branch filter was applied.</summary>
        public string BranchName { get; init; } = "All Branches";
    }
}
