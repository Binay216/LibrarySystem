namespace LibrarySystem.Models.Dto
{
    /// <summary>Public operating status - active branch count, items currently available to borrow, the API version, and the server's current time (ILibraryClock, library-local, not UTC).</summary>
    public record StatusDto(int BranchCount, int ItemsAvailable, string ApiVersion, DateTime ServerTime);
}
