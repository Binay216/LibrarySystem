using System.ComponentModel.DataAnnotations;

namespace LibrarySystem.Models
{
    public class Book : Item
    {
        [Required]
        [StringLength(150)]
        public string Author { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Genre { get; set; } = string.Empty;
    }
}
