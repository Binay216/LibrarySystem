using LibrarySystem.Models.Dto;
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
    /// The 401 response type is declared here too, since every action on every
    /// derived controller can return it (the key is required everywhere).
    /// </summary>
    [ApiController]
    [Route("api/v1/[controller]")]
    [ApiExceptionFilter]
    [ApiKeyAuthFilter]
    [ProducesResponseType(typeof(ApiErrorDto), StatusCodes.Status401Unauthorized)]
    public abstract class ApiControllerBase : ControllerBase
    {
    }
}
