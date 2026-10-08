using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace LibrarySystem.Models.ViewModels
{
    /// <summary>Backs the reception "Place Reservation" form - an item by library code, a borrower, submit.</summary>
    public class PlaceReservationViewModel
    {
        [Required(ErrorMessage = "Enter the item's library code.")]
        [Display(Name = "Library Code")]
        public string LibraryCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Select a borrower.")]
        [Display(Name = "Borrower")]
        public int BorrowerId { get; set; }

        /// <summary>Active borrowers for the picker - populated by the controller.</summary>
        public IEnumerable<SelectListItem> Borrowers { get; set; } = [];
    }
}
