using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Alkanzi.Erp.Controllers;

/// <summary>
/// The IT workspace. Serves the page only — every read and write goes from the browser to
/// Alkanzi.Erp.Api, which enforces the same role check again. The attribute here keeps the
/// page out of the wrong hands; the API is what actually protects the data.
/// </summary>
[Authorize(Roles = "Super Admin")]
public class ItController : Controller
{
    public IActionResult Workspace()
    {
        ViewData["Title"] = "IT — Organization Structure";
        return View();
    }
}
