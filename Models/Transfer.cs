using System.ComponentModel.DataAnnotations;
using LibrarySystem.Models.Enums;

namespace LibrarySystem.Models
{
    /// <summary>A request to move an item from one branch to another.</summary>
    public class Transfer
    {
        public int Id { get; set; }

        [Required]
        public int ItemId { get; set; }

        public Item? Item { get; set; }

        [Required]
        public int FromBranchId { get; set; }

        public Branch? FromBranch { get; set; }

        [Required]
        public int ToBranchId { get; set; }

        public Branch? ToBranch { get; set; }

        [DataType(DataType.Date)]
        public DateTime RequestedDate { get; set; }

        /// <summary>Null until the transfer finishes; set when it completes.</summary>
        [DataType(DataType.Date)]
        public DateTime? CompletedDate { get; set; }

        [Required]
        public TransferStatus Status { get; set; } = TransferStatus.Requested;

        /// <summary>Id of the AspNetUsers account that requested the transfer.</summary>
        [StringLength(450)]
        public string RequestedByUserId { get; set; } = string.Empty;

        [StringLength(500)]
        public string Reason { get; set; } = string.Empty;
    }
}
