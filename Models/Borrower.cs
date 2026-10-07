using System.ComponentModel.DataAnnotations;

namespace LibrarySystem.Models
{
    /// <summary>A library member who can borrow items. Managed by reception.</summary>
    public class Borrower
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(256)]
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Optional - email is the mandatory contact field. Drives notification
        /// channel selection in NotificationService: SMS if this is set, email
        /// otherwise.
        /// </summary>
        [Phone]
        [StringLength(30)]
        public string? Phone { get; set; }

        [Required]
        [StringLength(300)]
        public string Address { get; set; } = string.Empty;

        [DataType(DataType.Date)]
        public DateTime MembershipDate { get; set; }

        public bool IsActive { get; set; } = true;

        public ICollection<Loan> Loans { get; set; } = new List<Loan>();
    }
}
