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

        [HttpGet]
        public async Task<ActionResult<StatusDto>> Get()
        {
            var branchCount = await context.Branches.CountAsync(b => b.IsActive);
            var itemsAvailable = await context.Items.CountAsync(i => i.Status == ItemStatus.Available);

            return Ok(new StatusDto(branchCount, itemsAvailable, ApiVersion, clock.Now));
        }
    }
}
