using LibrarySystem.Models;

namespace LibrarySystem.Services
{
    /// <summary>
    /// Outcome of a <see cref="ILoanService"/> operation - lets controllers show a
    /// clear message on failure (e.g. an unavailable item) without throwing for
    /// what is really just an expected business rejection.
    /// </summary>
    public class LoanResult
    {
        public bool Succeeded { get; private init; }

        public string Message { get; private init; } = string.Empty;

        public Loan? Loan { get; private init; }

        public static LoanResult Success(Loan loan, string message) => new() { Succeeded = true, Loan = loan, Message = message };

        public static LoanResult Failure(string message) => new() { Succeeded = false, Message = message };
    }
}