using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SecondHandGoods.Web.Controllers;

/// <summary>
/// Serves custom error pages (404 Not Found and 500 Server Error).
/// </summary>
[AllowAnonymous]
public class ErrorController : Controller
{
    private readonly ILogger<ErrorController> _logger;

    public ErrorController(ILogger<ErrorController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Invoked by UseStatusCodePagesWithReExecute for 4xx/5xx responses.
    /// </summary>
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Index(int? statusCode)
    {
        if (statusCode == 404)
            return View("NotFound");
        if (statusCode == 500)
            return View("ServerError");
        ViewData["StatusCode"] = statusCode ?? 0;
        return View("Error");
    }

    /// <summary>
    /// Shown when an unhandled exception occurs (UseExceptionHandler).
    /// </summary>
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult ServerError()
    {
        return View();
    }

    /// <summary>
    /// Demo only: show a custom error page by code (404 or 500) so you can present them without triggering a real error.
    /// e.g. /Error/Show/404 or /Error/Show/500
    /// </summary>
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Show(int code = 404)
    {
        if (code == 404)
            return View("NotFound");
        if (code == 500)
            return View("ServerError");
        ViewData["StatusCode"] = code;
        return View("Error");
    }
}
