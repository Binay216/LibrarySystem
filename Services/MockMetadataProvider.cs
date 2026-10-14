using System.Reflection;
using System.Text.Json;
using LibrarySystem.Models.ViewModels;

namespace LibrarySystem.Services
{
    /// <summary>
    /// Serves ~30 bundled book/music/toy records from an embedded JSON file -
    /// no network call, so it works on a machine with no internet access. The
    /// records are loaded once and cached for the process lifetime; this is
    /// read-only reference data, not something that changes at runtime.
    /// </summary>
    public class MockMetadataProvider : IMetadataProvider
    {
        private const string ResourceName = "LibrarySystem.Data.Metadata.metadata-catalog.json";

        private static readonly Lazy<IReadOnlyList<MetadataRecord>> Records = new(LoadRecords);

        public Task<IReadOnlyList<MetadataRecord>> SearchAsync(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return Task.FromResult<IReadOnlyList<MetadataRecord>>([]);
            }

            var term = query.Trim();
            var results = Records.Value
                .Where(r => r.Title.Contains(term, StringComparison.OrdinalIgnoreCase)
                    || r.Isbn.Contains(term, StringComparison.OrdinalIgnoreCase))
                .ToList();

            return Task.FromResult<IReadOnlyList<MetadataRecord>>(results);
        }

        public Task<MetadataRecord?> GetByIdAsync(string externalId)
        {
            var record = Records.Value.FirstOrDefault(r => r.ExternalId == externalId);
            return Task.FromResult(record);
        }

        private static IReadOnlyList<MetadataRecord> LoadRecords()
        {
            var assembly = Assembly.GetExecutingAssembly();
            using var stream = assembly.GetManifestResourceStream(ResourceName)
                ?? throw new InvalidOperationException($"Embedded metadata resource '{ResourceName}' was not found.");

            var records = JsonSerializer.Deserialize<List<MetadataRecord>>(stream)
                ?? throw new InvalidOperationException("Embedded metadata resource deserialised to null.");

            return records;
        }
    }
}
