using LibrarySystem.Data;
using LibrarySystem.Models.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibrarySystem.Controllers.Api
{
    /// <summary>Public branch directory - active branches only.</summary>
    public class BranchesController(ApplicationDbContext context) : ApiControllerBase
    {
        [HttpGet]
        public async Task<ActionResult<List<BranchDto>>> Get()
        {
            var branches = await context.Branches
                .AsNoTracking()
                .Where(b => b.IsActive)
                .OrderBy(b => b.Name)
                .Select(b => new BranchDto(b.Code, b.Name, b.Address, b.Phone, b.OpeningHours))
                .ToListAsync();

            return Ok(branches);
        }
    }
}
