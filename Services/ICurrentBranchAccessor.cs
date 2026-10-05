using LibrarySystem.Models;

namespace LibrarySystem.Services
{
    /// <summary>Resolves the library branch this running app instance is configured to operate as.</summary>
    public interface ICurrentBranchAccessor
    {
        /// <summary>The configured branch, resolved from the database and cached for the lifetime of the current request.</summary>
        Task<Branch> GetCurrentBranchAsync();
    }
}
