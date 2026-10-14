namespace LibrarySystem.Models.ViewModels
{
    /// <summary>The Import page's model: just the metadata search tab's state - the CSV tab is a plain static upload form with nothing to carry between requests.</summary>
    public class ImportIndexViewModel
    {
        public string? MetadataQuery { get; init; }

        public IReadOnlyList<MetadataRecord> MetadataResults { get; init; } = [];

        public bool HasSearched => !string.IsNullOrWhiteSpace(MetadataQuery);
    }
}
