namespace LibrarySystem.Models.ViewModels
{
    /// <summary>
    /// The preview page's model: the parsed rows, plus the original CSV text
    /// round-tripped through a hidden field so Confirm can re-parse and
    /// re-validate from scratch rather than trusting anything about this
    /// preview's own markup (a row's "valid" status, its resolved branch id,
    /// etc. are never taken on faith from the client).
    /// </summary>
    public class ImportPreviewPageViewModel
    {
        public required string RawCsv { get; init; }

        public required IReadOnlyList<ImportRowPreview> ValidRows { get; init; }

        public required IReadOnlyList<ImportRowPreview> InvalidRows { get; init; }
    }
}
