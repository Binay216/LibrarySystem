namespace LibrarySystem.Models.ViewModels
{
    /// <summary>
    /// A catalogue record from an external metadata source - deliberately shaped
    /// like the CSV import row (Type plus the type-specific fields) but without
    /// LibraryCode/HomeBranchCode: a metadata provider describes an item, it
    /// doesn't know this library's coding scheme or which branch should hold a
    /// copy - the admin supplies those at import time, same as the CSV path.
    /// </summary>
    public class MetadataRecord
    {
        /// <summary>Stable id within the provider, used to re-fetch the exact record at import time rather than trusting field values round-tripped through the browser.</summary>
        public required string ExternalId { get; init; }

        public required string Type { get; init; }

        public required string Title { get; init; }

        public string Description { get; init; } = "";

        public string Author { get; init; } = "";

        public string Genre { get; init; } = "";

        public string Isbn { get; init; } = "";

        public string Artist { get; init; } = "";

        public int? Year { get; init; }

        public string ToyType { get; init; } = "";

        public int? RecommendedAge { get; init; }
    }
}
