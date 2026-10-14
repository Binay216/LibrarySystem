using LibrarySystem.Models.ViewModels;

namespace LibrarySystem.Services
{
    /// <summary>
    /// A source of external catalogue metadata for the admin import tool. This
    /// is the swappable abstraction requirement 5 asks for: ImportController and
    /// ImportService depend only on this interface, never on MockMetadataProvider
    /// directly, so a real provider (an ISBN/catalogue lookup API, say) can be
    /// registered in Program.cs in its place without any controller or view
    /// changes - only the DI registration line changes.
    /// </summary>
    public interface IMetadataProvider
    {
        /// <summary>Matches on title or ISBN, case-insensitive substring match.</summary>
        Task<IReadOnlyList<MetadataRecord>> SearchAsync(string query);

        /// <summary>
        /// Re-fetches one record by id. Used at import time instead of trusting
        /// field values posted back from the browser's search-results page - the
        /// same "never trust the client's copy of server data" principle the CSV
        /// import's preview/confirm round trip follows.
        /// </summary>
        Task<MetadataRecord?> GetByIdAsync(string externalId);
    }
}
