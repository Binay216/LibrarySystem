using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace LibrarySystem.Models.ViewModels
{
    /// <summary>Backs the reception "Raise Transfer" form - an item at this branch, a destination, and a reason.</summary>
    public class RaiseTransferViewModel
    {
        [Required(ErrorMessage = "Select an item.")]
        [Display(Name = "Item")]
        public int ItemId { get; set; }

        [Required(ErrorMessage = "Select a destination branch.")]
        [Display(Name = "Destination Branch")]
        public int ToBranchId { get; set; }

        [Required(ErrorMessage = "Enter a reason for the transfer.")]
        [StringLength(500)]
        public string Reason { get; set; } = string.Empty;

        /// <summary>Available items at this branch - populated by the controller.</summary>
        public IEnumerable<SelectListItem> Items { get; set; } = [];

        /// <summary>Other active branches - populated by the controller.</summary>
        public IEnumerable<SelectListItem> Branches { get; set; } = [];
    }
}
