using Microsoft.Extensions.Options;

namespace LibrarySystem.Services
{
    /// <inheritdoc cref="ILibraryClock" />
    public class LibraryClock : ILibraryClock
    {
        private readonly TimeZoneInfo _timeZone;

        public LibraryClock(IOptions<LibraryClockOptions> options)
        {
            _timeZone = TimeZoneInfo.FindSystemTimeZoneById(options.Value.TimeZoneId);
        }

        /// <inheritdoc />
        public DateTime Today => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _timeZone).Date;
    }
}
