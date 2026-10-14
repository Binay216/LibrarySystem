using LibrarySystem.Data;
using LibrarySystem.Models;
using LibrarySystem.Models.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibrarySystem.Controllers.Api
{
    /// <summary>Item types with counts, plus the distinct book genres and toy types in the collection.</summary>
    public class CategoriesController(ApplicationDbContext context) : ApiControllerBase
    {
        /// <summary>
        /// Gets the item type breakdown (Book/Music/Toy, with counts across the
        /// whole catalogue), plus every distinct book genre and toy type
        /// currently in the collection - useful for building a catalogue
        /// search's filter options.
        /// </summary>
        /// <response code="200">The category breakdown.</response>
        [HttpGet]
        [ProducesResponseType(typeof(CategoriesDto), StatusCodes.Status200OK)]
        public async Task<ActionResult<CategoriesDto>> Get()
        {
            var types = new List<ItemTypeCountDto>
            {
                new("Book", await context.Items.OfType<Book>().CountAsync()),
                new("Music", await context.Items.OfType<Music>().CountAsync()),
                new("Toy", await context.Items.OfType<Toy>().CountAsync())
            };

            var genres = await context.Items.OfType<Book>().Select(b => b.Genre).Distinct().OrderBy(g => g).ToListAsync();
            var toyTypes = await context.Items.OfType<Toy>().Select(t => t.ToyType).Distinct().OrderBy(t => t).ToListAsync();

            return Ok(new CategoriesDto(types, genres, toyTypes));
        }
    }
}
