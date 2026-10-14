namespace LibrarySystem.Models.ViewModels
{
    /// <summary>The outcome of actually importing the rows the admin confirmed.</summary>
    public class ImportCommitResult
    {
        /// <summary>Set when the whole import was rolled back - e.g. a race on a library code between preview and confirm. Created is 0 in that case.</summary>
        public string? FileError { get; init; }

        public int Created { get; init; }

        /// <summary>Valid rows the admin left unchecked in the preview - not imported, but not rejected either.</summary>
        public int Skipped { get; init; }

        /// <summary>Rows that failed validation and were never eligible for import.</summary>
        public int Rejected { get; init; }
    }
}
