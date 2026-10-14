using Microsoft.AspNetCore.Mvc;

namespace LibrarySystem.Controllers.Api
{
    /// <summary>
    /// Shared base for api/v1 controllers: the route prefix,
    /// <see cref="ApiExceptionFilterAttribute"/> for a consistent JSON error
    /// shape on any unhandled exception, and <see cref="ApiKeyAuthFilterAttribute"/>
    /// requiring a valid X-Api-Key header. Both filter attributes are applied
    /// here rather than registered globally in Program.cs, so they're scoped
    /// to exactly these controllers and can't change how the existing MVC
    /// pages handle errors or require a key they were never meant to need.
    /// </summary>
    [ApiController]
    [Route("api/v1/[controller]")]
    [ApiExceptionFilter]
    [ApiKeyAuthFilter]
    public abstract class ApiControllerBase : ControllerBase
    {
    }
}
