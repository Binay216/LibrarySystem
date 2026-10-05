using System.ComponentModel.DataAnnotations;

namespace LibrarySystem.Models
{
    /// <summary>A physical library branch. Items have a home branch and a current branch; loans and transfers reference the branches involved.</summary>
    public class Branch
    {
        public int Id { get; set; }

        /// <summary>Short code used to identify the branch, e.g. "CAS".</summary>
        [Required]
        [StringLength(10)]
        public string Code { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(300)]
        public string Address { get; set; } = string.Empty;

        [Phone]
        [StringLength(30)]
        public string Phone { get; set; } = string.Empty;

        [StringLength(100)]
        public string OpeningHours { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;
    }
}
