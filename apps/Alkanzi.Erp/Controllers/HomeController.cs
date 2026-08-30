using System.Diagnostics;
using Alkanzi.Erp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Alkanzi.Erp.Controllers;

/// <summary>
/// Serves pages only. The data those pages show comes from Alkanzi.Erp.Api, fetched by the
/// front end — there is no DbContext here to query.
/// </summary>
[Authorize]
public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger) => _logger = logger;

    public IActionResult Index()
    {
        ViewData["Title"] = "Dashboard";
        return View();
    }

    public IActionResult Privacy()
    {
        ViewData["Title"] = "Privacy";
        return View();
    }

    [AllowAnonymous]   // the error page must render even when the failure was the auth pipeline itself
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
        => View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
}
