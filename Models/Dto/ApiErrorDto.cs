namespace LibrarySystem.Models.Dto
{
    /// <summary>The consistent JSON error shape returned by every api/v1 endpoint - never a stack trace.</summary>
    public record ApiErrorDto(int StatusCode, string Message);
}
