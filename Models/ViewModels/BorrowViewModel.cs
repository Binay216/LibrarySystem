using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace LibrarySystem.Models.ViewModels
{
    /// <summary>Backs the reception Borrow form - library code lookup plus a borrower pick.</summary>
    public class BorrowViewModel
    {
        [Required(ErrorMessage = "Enter the item's library code.")]
        [Display(Name = "Library Code")]
        public string LibraryCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Select a borrower.")]
        [Display(Name = "Borrower")]
        public int BorrowerId { get; set; }

        public IEnumerable<SelectListItem> Borrowers { get; set; } = [];
    }
}
