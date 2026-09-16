namespace LibrarySystem.Services
{
    /// <summary>
    /// Resolves "today" as the library's own local calendar date. Business dates
    /// (BorrowedDate, DueDate, ReturnedDate) are calendar days, and deriving them
    /// from DateTime.UtcNow.Date is wrong whenever the library's timezone is ahead
    /// of UTC - the UTC calendar day hasn't rolled over yet at local midnight.
    /// Anything that needs "today" for a library business date - LoanService now,
    /// StatisticsService's overdue checks later - should go through this, not
    /// DateTime.UtcNow.Date or DateTime.Now.Date directly.
    /// </summary>
    public interface ILibraryClock
    {
        DateTime Today { get; }
    }
}
