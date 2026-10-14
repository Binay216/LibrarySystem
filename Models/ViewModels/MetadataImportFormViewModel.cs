using Microsoft.AspNetCore.Mvc.Rendering;

namespace LibrarySystem.Models.ViewModels
{
    /// <summary>The "choose a home branch and library code" page for importing one metadata record.</summary>
    public class MetadataImportFormViewModel
    {
        public required MetadataRecord Record { get; init; }

        public IEnumerable<SelectListItem> Branches { get; init; } = [];

        public string? LibraryCode { get; init; }

        public int? HomeBranchId { get; init; }
    }
}
