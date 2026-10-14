using LibrarySystem.Models.Dto;
using LibrarySystem.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;

namespace LibrarySystem.Controllers.Api
{
    /// <summary>
    /// Requires a valid X-Api-Key header on every api/v1 request, checked
    /// against configuration (ApiKey:Key in appsettings.json). Same pattern as
    /// <see cref="ApiExceptionFilterAttribute"/>: applied to ApiControllerBase
    /// so it's scoped to exactly the API controllers and can't affect the MVC
    /// pages. A single shared key checked in an action filter is enough for
    /// this scope - a real deployment with per-user identity would use a
    /// proper authentication scheme instead of a bespoke filter.
    /// </summary>
    public class ApiKeyAuthFilterAttribute : ActionFilterAttribute
    {
        private const string HeaderName = "X-Api-Key";

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var options = context.HttpContext.RequestServices.GetRequiredService<IOptions<ApiKeyOptions>>().Value;

            var hasHeader = context.HttpContext.Request.Headers.TryGetValue(HeaderName, out var providedKeyValues);
            var providedKey = hasHeader ? providedKeyValues.ToString() : null;

            // Fails closed if the configured key is blank (misconfiguration),
            // rather than letting a blank header accidentally match a blank key.
            var isValid = !string.IsNullOrEmpty(options.Key)
                && !string.IsNullOrEmpty(providedKey)
                && string.Equals(providedKey, options.Key, StringComparison.Ordinal);

            if (!isValid)
            {
                context.Result = new ObjectResult(new ApiErrorDto(StatusCodes.Status401Unauthorized, "A valid X-Api-Key header is required."))
                {
                    StatusCode = StatusCodes.Status401Unauthorized
                };
                return;
            }

            base.OnActionExecuting(context);
        }
    }
}
