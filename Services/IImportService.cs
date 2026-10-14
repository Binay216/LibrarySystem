using LibrarySystem.Models.ViewModels;

namespace LibrarySystem.Services
{
    /// <summary>Parses, validates and imports the Items CSV used by the admin bulk-import tool.</summary>
    public interface IImportService
    {
        /// <summary>Parses and validates the CSV without saving anything.</summary>
        Task<ImportPreviewResult> PreviewAsync(string csvText);

        /// <summary>
        /// Re-parses and re-validates the CSV (never trusts a client-supplied
        /// "this row was valid" claim), then imports the rows that are both
        /// still valid and in <paramref name="selectedRowNumbers"/>, in a
        /// single transaction.
        /// </summary>
        Task<ImportCommitResult> CommitAsync(string csvText, IReadOnlySet<int> selectedRowNumbers);

        /// <summary>
        /// Imports one external metadata record, validated exactly like a CSV
        /// row - library code not already taken, branch exists - the shared
        /// rule the CSV and metadata import paths are both required to apply.
        /// </summary>
        Task<MetadataImportResult> ImportMetadataRecordAsync(MetadataRecord record, string libraryCode, int homeBranchId);
    }
}
