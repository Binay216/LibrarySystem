using Microsoft.AspNetCore.Mvc.Rendering;

namespace LibrarySystem.Models.ViewModels
{
    /// <summary>
    /// Wraps one of the IReportService report view models with the filter-form
    /// state needed to render the on-screen report page: the branch dropdown
    /// and which branch is currently selected. The date range doesn't need its
    /// own copy here - the report's own Filters.StartDate/EndDate (the range it
    /// was actually generated for) is what both the date inputs and the export
    /// links read from, so the page can't drift from what's on screen.
    /// </summary>
    public class ReportPageViewModel<TReport>
    {
        public required TReport Report { get; init; }

        public int? SelectedBranchId { get; init; }

        public IEnumerable<SelectListItem> Branches { get; init; } = [];
    }
}
