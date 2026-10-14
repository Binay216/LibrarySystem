using LibrarySystem.Data;
using LibrarySystem.Models.Dto;
using LibrarySystem.Models.Enums;
using LibrarySystem.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibrarySystem.Controllers.Api
{
    /// <summary>Public operating status.</summary>
    public class StatusController(ApplicationDbContext context, ILibraryClock clock) : ApiControllerBase
    {
        private const string ApiVersion = "1.0";

        /// <summary>
        /// Gets a snapshot of the library's current operating status: how many
        /// branches are active, how many items are available to borrow right
        /// now, the API version, and the server's current time (library-local,
        /// via ILibraryClock - not UTC).
        /// </summary>
        /// <response code="200">The current status.</response>
        [HttpGet]
        [ProducesResponseType(typeof(StatusDto), StatusCodes.Status200OK)]
        public async Task<ActionResult<StatusDto>> Get()
        {
            var branchCount = await context.Branches.CountAsync(b => b.IsActive);
            var itemsAvailable = await context.Items.CountAsync(i => i.Status == ItemStatus.Available);

            return Ok(new StatusDto(branchCount, itemsAvailable, ApiVersion, clock.Now));
        }
    }
}
