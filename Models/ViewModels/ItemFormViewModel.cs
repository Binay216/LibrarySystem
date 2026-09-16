using System.ComponentModel.DataAnnotations;
using LibrarySystem.Models.Enums;

namespace LibrarySystem.Models.ViewModels
{
    /// <summary>
    /// Backs both the Create and Edit forms for all three item types. One shared
    /// view model keeps <see cref="Controllers.ItemsController"/> polymorphic instead
    /// of needing a type-specific controller per SPEC.md's structure. Type-specific
    /// fields are validated conditionally in <see cref="Validate"/> since a plain
    /// DataAnnotations attribute can't depend on the selected <see cref="ItemType"/>.
    /// </summary>
    public class ItemFormViewModel : IValidatableObject
    {
        public int Id { get; set; }

        [Required]
        public ItemTypeOption ItemType { get; set; }

        [Required(ErrorMessage = "Name is required.")]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Description is required.")]
        [StringLength(1000)]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Library code is required.")]
        [StringLength(20)]
        [Display(Name = "Library Code")]
        public string LibraryCode { get; set; } = string.Empty;

        [Required]
        public ItemStatus Status { get; set; } = ItemStatus.Available;

        [StringLength(150)]
        public string? Author { get; set; }

        [StringLength(100)]
        public string? Genre { get; set; }

        [StringLength(150)]
        public string? Artist { get; set; }

        public int? Year { get; set; }

        [StringLength(100)]
        [Display(Name = "Toy Type")]
        public string? ToyType { get; set; }

        [Display(Name = "Recommended Age")]
        public int? RecommendedAge { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            switch (ItemType)
            {
                case ItemTypeOption.Book:
                    if (string.IsNullOrWhiteSpace(Author))
                    {
                        yield return new ValidationResult("Author is required for books.", new[] { nameof(Author) });
                    }
                    if (string.IsNullOrWhiteSpace(Genre))
                    {
                        yield return new ValidationResult("Genre is required for books.", new[] { nameof(Genre) });
                    }
                    break;

                case ItemTypeOption.Music:
                    if (string.IsNullOrWhiteSpace(Artist))
                    {
                        yield return new ValidationResult("Artist is required for music.", new[] { nameof(Artist) });
                    }
                    if (Year is null or < 1900 or > 2100)
                    {
                        yield return new ValidationResult("Enter a valid year between 1900 and 2100.", new[] { nameof(Year) });
                    }
                    break;

                case ItemTypeOption.Toy:
                    if (string.IsNullOrWhiteSpace(ToyType))
                    {
                        yield return new ValidationResult("Toy type is required for toys.", new[] { nameof(ToyType) });
                    }
                    if (RecommendedAge is null or < 0 or > 18)
                    {
                        yield return new ValidationResult("Enter a recommended age between 0 and 18.", new[] { nameof(RecommendedAge) });
                    }
                    break;
            }
        }
    }
}
