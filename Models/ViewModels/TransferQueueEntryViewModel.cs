using LibrarySystem.Models.Enums;

namespace LibrarySystem.Models.ViewModels
{
    /// <summary>One row in the transfer queue - a flattened, display-ready summary of a Transfer.</summary>
    public class TransferQueueEntryViewModel
    {
        public int Id { get; set; }

        public string ItemName { get; set; } = string.Empty;

        public string LibraryCode { get; set; } = string.Empty;

        public string FromBranchName { get; set; } = string.Empty;

        public string ToBranchName { get; set; } = string.Empty;

        public TransferStatus Status { get; set; }

        public DateTime RequestedDate { get; set; }

        public DateTime? CompletedDate { get; set; }

        public string Reason { get; set; } = string.Empty;
    }
}
