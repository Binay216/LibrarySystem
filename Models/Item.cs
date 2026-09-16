using System.ComponentModel.DataAnnotations;
using LibrarySystem.Models.Enums;

namespace LibrarySystem.Models
{
    /// <summary>
    /// Base type for everything the library lends out. Mapped table-per-hierarchy
    /// (see <see cref="Data.ApplicationDbContext"/>) so the public search and admin
    /// listing can query every item type in a single table scan with no joins.
    /// </summary>
    public abstract class Item
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(1000)]
        public string Description { get; set; } = string.Empty;

        /// <summary>Unique code used at reception to look up an item for borrowing/returning.</summary>
        [Required]
        [StringLength(20)]
        public string LibraryCode { get; set; } = string.Empty;

        [Required]
        public ItemStatus Status { get; set; } = ItemStatus.Available;

        public ICollection<Loan> Loans { get; set; } = new List<Loan>();
    }
}
