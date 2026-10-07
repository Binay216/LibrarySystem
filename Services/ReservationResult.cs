using LibrarySystem.Models;

namespace LibrarySystem.Services
{
    /// <summary>
    /// Outcome of a <see cref="IReservationService"/> operation - mirrors
    /// <see cref="LoanResult"/> so a controller can show a clear message on
    /// failure (e.g. a duplicate reservation) without throwing for what is really
    /// just an expected business rejection.
    /// </summary>
    public class ReservationResult
    {
        public bool Succeeded { get; private init; }

        public string Message { get; private init; } = string.Empty;

        public Reservation? Reservation { get; private init; }

        public static ReservationResult Success(Reservation reservation, string message) => new() { Succeeded = true, Reservation = reservation, Message = message };

        public static ReservationResult Failure(string message) => new() { Succeeded = false, Message = message };
    }
}
