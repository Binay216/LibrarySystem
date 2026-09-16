using System.ComponentModel.DataAnnotations;

namespace LibrarySystem.Models
{
    public class Music : Item
    {
        [Required]
        [StringLength(150)]
        public string Artist { get; set; } = string.Empty;

        [Range(1900, 2100)]
        public int Year { get; set; }
    }
}
