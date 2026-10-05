namespace LibrarySystem.Services
{
    /// <summary>
    /// Which library branch this running app instance operates as, bound from the
    /// "Library" section of appsettings.json. Validated against the seeded branches
    /// at startup in Program.cs - see <see cref="ICurrentBranchAccessor"/> for
    /// resolving this code to the actual <see cref="Models.Branch"/> entity.
    /// </summary>
    public class LibraryOptions
    {
        public const string SectionName = "Library";

        public string BranchCode { get; set; } = string.Empty;
    }
}
