using LibrarySystem.Data;
using LibrarySystem.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace LibrarySystem.Services
{
    /// <inheritdoc cref="ICurrentBranchAccessor" />
    public class CurrentBranchAccessor(ApplicationDbContext context, IOptions<LibraryOptions> libraryOptions) : ICurrentBranchAccessor
    {
        private readonly string _branchCode = libraryOptions.Value.BranchCode;
        private Branch? _branch;

        /// <inheritdoc />
        public async Task<Branch> GetCurrentBranchAsync()
        {
            // Registered Scoped (one instance per request), so this field caches the
            // lookup for the rest of the request instead of querying every call.
            if (_branch is not null)
            {
                return _branch;
            }

            _branch = await context.Branches.FirstOrDefaultAsync(b => b.Code == _branchCode)
                ?? throw new InvalidOperationException($"Configured branch code '{_branchCode}' does not match any seeded branch. Program.cs validates this at startup, so this should never happen.");

            return _branch;
        }
    }
}
