namespace LibrarySystem.Models.ViewModels
{
    /// <summary>
    /// One parsed CSV row plus its validation outcome. Carries every raw field,
    /// not just the ones shown in the preview table, so the same object can be
    /// used again at commit time to build the actual Item - no second parallel
    /// "extract fields for saving" path to drift from the preview.
    /// </summary>
    public class ImportRowPreview
    {
        /// <summary>1-based, counting the header as row 1 - matches what the admin sees opening the file in a text editor or Excel.</summary>
        public required int RowNumber { get; init; }

        public required bool IsValid { get; init; }

        public IReadOnlyList<string> Errors { get; init; } = [];

        public string Type { get; init; } = "";

        public string Name { get; init; } = "";

        public string Description { get; init; } = "";

        public string LibraryCode { get; init; } = "";

        public string HomeBranchCode { get; init; } = "";

        public string Author { get; init; } = "";

        public string Genre { get; init; } = "";

        public string Artist { get; init; } = "";

        public string Year { get; init; } = "";

        public string ToyType { get; init; } = "";

        public string RecommendedAge { get; init; } = "";

        /// <summary>Resolved only when IsValid - the branch matching HomeBranchCode.</summary>
        public int? HomeBranchId { get; init; }
    }
}
