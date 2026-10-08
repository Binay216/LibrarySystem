using LibrarySystem.Data;
using LibrarySystem.Models;
using LibrarySystem.Models.Dto;
using LibrarySystem.Models.Enums;
using LibrarySystem.Models.ViewModels;
using LibrarySystem.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibrarySystem.Controllers.Api
{
    /// <summary>
    /// Public, read-only item catalogue. Query filtering mirrors
    /// Controllers.CatalogController (the MVC public catalog) - there's no
    /// existing shared service for "search items by these filters" to reuse
    /// (CatalogController builds its query inline, same as this does), so this
    /// is a deliberate, acknowledged duplication rather than a missed reuse;
    /// extracting a shared item-search service would be a reasonable follow-up.
    /// Queue counts do reuse IReservationService.GetActiveReservationCountsAsync
    /// rather than querying Reservations directly.
    /// </summary>
    public class ItemsController(ApplicationDbContext context, IReservationService reservationService) : ApiControllerBase
    {
        private const int DefaultPageSize = 20;
        private const int MaxPageSize = 100;

        /// <summary>Paged, filterable catalogue search.</summary>
        [HttpGet]
        public async Task<ActionResult<PagedResultDto<ItemSummaryDto>>> Get(
            string? search,
            ItemTypeOption? type,
            int? branch,
            bool availableOnly = false,
            int page = 1,
            int pageSize = DefaultPageSize)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

            var query = context.Items.AsNoTracking().Include(i => i.CurrentBranch).AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                query = query.Where(i => EF.Functions.Like(i.Name, $"%{term}%") || EF.Functions.Like(i.Description, $"%{term}%"));
            }

            query = type switch
            {
                ItemTypeOption.Book => query.OfType<Book>(),
                ItemTypeOption.Music => query.OfType<Music>(),
                ItemTypeOption.Toy => query.OfType<Toy>(),
                _ => query
            };

            if (availableOnly)
            {
                query = query.Where(i => i.Status == ItemStatus.Available);
            }

            if (branch is { } branchId)
            {
                query = query.Where(i => i.CurrentBranchId == branchId);
            }

            // Name isn't unique (no unique index on it, unlike LibraryCode), so
            // ordering by it alone doesn't give SQL Server a deterministic row
            // order - ties could shift between pages or duplicate/skip across a
            // page boundary. LibraryCode is unique (see ApplicationDbContext's
            // index on it), so it's a safe, stable tiebreaker.
            query = query.OrderBy(i => i.Name).ThenBy(i => i.LibraryCode);

            var totalCount = await query.CountAsync();
            var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            var queueCounts = await reservationService.GetActiveReservationCountsAsync(items.Select(i => i.Id));
            var results = items.Select(i => ToSummaryDto(i, queueCounts.GetValueOrDefault(i.Id))).ToList();

            return Ok(new PagedResultDto<ItemSummaryDto>(results, totalCount, page, pageSize));
        }

        /// <summary>One item, looked up by its library code.</summary>
        [HttpGet("{libraryCode}")]
        public async Task<ActionResult<ItemDetailDto>> GetByLibraryCode(string libraryCode)
        {
            var item = await context.Items
                .AsNoTracking()
                .Include(i => i.CurrentBranch)
                .FirstOrDefaultAsync(i => i.LibraryCode == libraryCode.Trim());

            if (item is null)
            {
                return NotFound(new ApiErrorDto(StatusCodes.Status404NotFound, $"No item found with library code '{libraryCode}'."));
            }

            var queueCounts = await reservationService.GetActiveReservationCountsAsync([item.Id]);
            return Ok(ToDetailDto(item, queueCounts.GetValueOrDefault(item.Id)));
        }

        private static ItemSummaryDto ToSummaryDto(Item item, int queueCount)
        {
            var (type, summary) = item switch
            {
                Book book => ("Book", $"{book.Author} - {book.Genre}"),
                Music music => ("Music", $"{music.Artist} ({music.Year})"),
                Toy toy => ("Toy", $"{toy.ToyType}, age {toy.RecommendedAge}+"),
                _ => ("Item", string.Empty)
            };

            return new ItemSummaryDto(
                item.LibraryCode,
                item.Name,
                item.Description,
                type,
                item.Status.ToString(),
                summary,
                item.CurrentBranch!.Name,
                queueCount);
        }

        private static ItemDetailDto ToDetailDto(Item item, int queueCount)
        {
            var type = item switch
            {
                Book => "Book",
                Music => "Music",
                Toy => "Toy",
                _ => "Item"
            };

            return new ItemDetailDto(
                item.LibraryCode,
                item.Name,
                item.Description,
                type,
                item.Status.ToString(),
                item.CurrentBranch!.Name,
                queueCount,
                (item as Book)?.Author,
                (item as Book)?.Genre,
                (item as Music)?.Artist,
                (item as Music)?.Year,
                (item as Toy)?.ToyType,
                (item as Toy)?.RecommendedAge);
        }
    }
}
