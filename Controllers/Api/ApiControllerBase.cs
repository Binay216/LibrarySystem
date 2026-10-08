using Microsoft.AspNetCore.Mvc;

namespace LibrarySystem.Controllers.Api
{
    /// <summary>
    /// Shared base for api/v1 controllers: the route prefix, and
    /// <see cref="ApiExceptionFilterAttribute"/> for a consistent JSON error
    /// shape on any unhandled exception. The filter attribute is applied here
    /// rather than registered globally in Program.cs, so it's scoped to
    /// exactly these controllers and can't change how the existing MVC pages
    /// handle errors.
    /// </summary>
    [ApiController]
    [Route("api/v1/[controller]")]
    [ApiExceptionFilter]
    public abstract class ApiControllerBase : ControllerBase
    {
    }
}
