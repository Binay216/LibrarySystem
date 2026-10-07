using LibrarySystem.Models.Enums;

namespace LibrarySystem.Models.ViewModels
{
    /// <summary>
    /// Backs the Transfers Index page - the current branch's incoming and outgoing
    /// transfers, split so reception can tell at a glance which direction each one moves.
    /// </summary>
    public class TransferQueueViewModel
    {
        public List<TransferQueueEntryViewModel> Incoming { get; set; } = [];

        public List<TransferQueueEntryViewModel> Outgoing { get; set; } = [];

        /// <summary>The status being filtered to, or null for the default (Requested + InTransit) view.</summary>
        public TransferStatus? StatusFilter { get; set; }
    }
}
