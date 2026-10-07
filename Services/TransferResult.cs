using LibrarySystem.Models;

namespace LibrarySystem.Services
{
    /// <summary>
    /// Outcome of a <see cref="ITransferService"/> operation - lets controllers show a
    /// clear message on failure (e.g. an item that's still on loan) without throwing for
    /// what is really just an expected business rejection. Mirrors <see cref="LoanResult"/>.
    /// </summary>
    public class TransferResult
    {
        public bool Succeeded { get; private init; }

        public string Message { get; private init; } = string.Empty;

        public Transfer? Transfer { get; private init; }

        public static TransferResult Success(Transfer transfer, string message) => new() { Succeeded = true, Transfer = transfer, Message = message };

        public static TransferResult Failure(string message) => new() { Succeeded = false, Message = message };
    }
}
