using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LibrarySystem.Models
{
    /// <summary>
    /// Joins an <see cref="Item"/> to the <see cref="Borrower"/> who has it. The sole
    /// data source for all three manager dashboards (borrowing, item status, fines).
    /// </summary>
    public class Loan
    {
        public int Id { get; set; }

        [Required]
        public int ItemId { get; set; }

        public Item? Item { get; set; }

        [Required]
        public int BorrowerId { get; set; }

        public Borrower? Borrower { get; set; }

        [DataType(DataType.Date)]
        public DateTime BorrowedDate { get; set; }

        [DataType(DataType.Date)]
        public DateTime DueDate { get; set; }

        /// <summary>Null while the item is still out; set when it is returned.</summary>
        [DataType(DataType.Date)]
        public DateTime? ReturnedDate { get; set; }

        [Column(TypeName = "decimal(6,2)")]
        public decimal FineAmount { get; set; }

        public bool FinePaid { get; set; }
    }
}
