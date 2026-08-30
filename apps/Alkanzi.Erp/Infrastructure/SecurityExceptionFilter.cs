using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Alkanzi.Erp.Infrastructure;

/// <summary>
/// Turns a refused permission into a 403 instead of a 500.
/// <para>
/// <c>ISecurityService.DemandAsync</c> throws <see cref="UnauthorizedAccessException"/> when a
/// caller lacks a right. Left unhandled that becomes a server error, which is wrong twice
/// over: the request was understood and deliberately refused, which is exactly what 403
/// means, and an unhandled exception returns a stack trace describing the security internals
/// that did the refusing.
/// </para>
/// <para>
/// Browser navigations are sent to the access-denied page; anything asking for JSON gets a
/// small JSON body, because the AngularJS front end cannot do anything useful with an HTML
/// error page.
/// </para>
/// </summary>
public sealed class SecurityExceptionFilter : IExceptionFilter
{
    private readonly ILogger<SecurityExceptionFilter> _logger;

    public SecurityExceptionFilter(ILogger<SecurityExceptionFilter> logger) => _logger = logger;

    public void OnException(ExceptionContext context)
    {
        if (context.Exception is not UnauthorizedAccessException ex) return;

        var request = context.HttpContext.Request;

        // Logged as a warning, not an error: a refusal is the system working. It is still
        // worth recording, because a burst of them is how you notice a misconfigured group.
        _logger.LogWarning("Access denied for {User} on {Path}: {Message}",
            context.HttpContext.User.Identity?.Name ?? "anonymous", request.Path, ex.Message);

        var wantsJson =
            string.Equals(request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase)
            || (request.Headers.Accept.ToString()?.Contains("application/json", StringComparison.OrdinalIgnoreCase) ?? false);

        context.Result = wantsJson
            ? new JsonResult(new { error = "forbidden", message = "You do not have permission to perform this action." })
              { StatusCode = StatusCodes.Status403Forbidden }
            : new RedirectToActionResult("Denied", "Account", null);

        context.ExceptionHandled = true;
    }
}
