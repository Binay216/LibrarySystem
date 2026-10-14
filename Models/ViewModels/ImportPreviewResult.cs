namespace LibrarySystem.Models.ViewModels
{
    /// <summary>The outcome of parsing and validating an uploaded CSV, before anything is saved.</summary>
    public class ImportPreviewResult
    {
        /// <summary>Set when the file itself can't be read as an import - empty, or missing expected columns. Rows are empty in that case.</summary>
        public string? FileError { get; init; }

        public IReadOnlyList<ImportRowPreview> Rows { get; init; } = [];

        public IEnumerable<ImportRowPreview> ValidRows => Rows.Where(r => r.IsValid);

        public IEnumerable<ImportRowPreview> InvalidRows => Rows.Where(r => !r.IsValid);
    }
}
