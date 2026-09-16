using System.ComponentModel.DataAnnotations;
using LibrarySystem.Models.Enums;

namespace LibrarySystem.Models.ViewModels
{
    /// <summary>Backs the reception Return form - library code lookup plus the item's condition.</summary>
    public class ReturnViewModel
    {
        [Required(ErrorMessage = "Enter the item's library code.")]
        [Display(Name = "Library Code")]
        public string LibraryCode { get; set; } = string.Empty;

        [Required]
        public ItemStatus Condition { get; set; } = ItemStatus.Available;
    }
}
