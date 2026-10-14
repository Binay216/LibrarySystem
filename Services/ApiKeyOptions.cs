namespace LibrarySystem.Services
{
    /// <summary>
    /// The API key required on every api/v1 request (X-Api-Key header), bound
    /// from the "ApiKey" section of appsettings.json. See the comment there -
    /// a visible dev key is fine for this demo app, same as the seeded Identity
    /// passwords in IdentitySeed.cs, but a real deployment would pull this from
    /// a secret store rather than committing it to source control.
    /// </summary>
    public class ApiKeyOptions
    {
        public const string SectionName = "ApiKey";

        public string Key { get; set; } = string.Empty;
    }
}
