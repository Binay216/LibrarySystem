using LibrarySystem.Models.Enums;

namespace LibrarySystem.Models.ViewModels
{
    /// <summary>
    /// A flattened, type-agnostic summary of one item - shared by the admin item
    /// index and the public catalog, the two places that list items across all
    /// three types at once.
    /// </summary>
    public class ItemListItemViewModel
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public string LibraryCode { get; set; } = string.Empty;

        public string TypeLabel { get; set; } = string.Empty;

        public ItemStatus Status { get; set; }

        /// <summary>Type-specific summary, e.g. "J.R.R. Tolkien - Fantasy" for a book.</summary>
        public string Summary { get; set; } = string.Empty;

        /// <summary>The branch the item is physically at right now. Requires the caller to have included Item.CurrentBranch.</summary>
        public string CurrentBranchName { get; set; } = string.Empty;

        /// <summary>True when the item isn't currently at its home branch - e.g. mid-transfer.</summary>
        public bool IsAwayFromHome { get; set; }

        /// <summary>
        /// How many active reservations (Waiting or Ready) this item has - how
        /// many people are ahead of a patron who'd join the queue now. Zero by
        /// default; not set by FromItem since it comes from a separate query -
        /// the caller (CatalogController) fills it in afterward for whichever
        /// items it's displaying.
        /// </summary>
        public int QueueCount { get; set; }

        public static ItemListItemViewModel FromItem(Item item)
        {
            var (typeLabel, summary) = item switch
            {
                Book book => ("Book", $"{book.Author} - {book.Genre}"),
                Music music => ("Music", $"{music.Artist} ({music.Year})"),
                Toy toy => ("Toy", $"{toy.ToyType}, age {toy.RecommendedAge}+"),
                _ => ("Item", string.Empty)
            };

            return new ItemListItemViewModel
            {
                Id = item.Id,
                Name = item.Name,
                Description = item.Description,
                LibraryCode = item.LibraryCode,
                Status = item.Status,
                TypeLabel = typeLabel,
                Summary = summary,
                CurrentBranchName = item.CurrentBranch!.Name,
                IsAwayFromHome = item.CurrentBranchId != item.HomeBranchId
            };
        }
    }
}
