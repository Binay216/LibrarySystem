using LibrarySystem.Models.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace LibrarySystem.Controllers.Api
{
    /// <summary>
    /// Converts any unhandled exception from an api/v1 action into the
    /// consistent <see cref="ApiErrorDto"/> JSON shape - no stack trace. A real
    /// filter (registered via attribute), not a controller implementing
    /// IExceptionFilter directly: that only self-registers for IActionFilter/
    /// IResultFilter, not IExceptionFilter, so it never actually runs.
    /// </summary>
    public class ApiExceptionFilterAttribute : ExceptionFilterAttribute
    {
        public override void OnException(ExceptionContext context)
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<ApiExceptionFilterAttribute>>();
            logger.LogError(context.Exception, "Unhandled exception in API controller for {Path}", context.HttpContext.Request.Path);

            context.Result = new ObjectResult(new ApiErrorDto(StatusCodes.Status500InternalServerError, "An unexpected error occurred."))
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
            context.ExceptionHandled = true;
        }
    }
}
