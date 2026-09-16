using System.ComponentModel.DataAnnotations;

namespace LibrarySystem.Models
{
    public class Toy : Item
    {
        [Required]
        [StringLength(100)]
        public string ToyType { get; set; } = string.Empty;

        [Range(0, 18)]
        public int RecommendedAge { get; set; }
    }
}
