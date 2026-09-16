namespace LibrarySystem.Services
{
    /// <summary>
    /// The library's own timezone, bound from the "LibraryClock" section of
    /// appsettings.json. Used by <see cref="LibraryClock"/> to resolve "today" as
    /// the library's local calendar date rather than the server's.
    /// </summary>
    public class LibraryClockOptions
    {
        public const string SectionName = "LibraryClock";

        /// <summary>An IANA or Windows timezone id, e.g. "Australia/Adelaide".</summary>
        public string TimeZoneId { get; set; } = "UTC";
    }
}
