namespace LibrarySystem.Models.ViewModels
{
    /// <summary>The outcome of importing one external metadata record into the catalogue.</summary>
    public class MetadataImportResult
    {
        public required bool Success { get; init; }

        public string? ErrorMessage { get; init; }
    }
}
