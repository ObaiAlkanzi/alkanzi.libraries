using Microsoft.AspNetCore.Mvc;

namespace Alkanzi.SearchEngine.Demo.Controllers;

public class WorkspaceController : Controller
{
    private readonly IConfiguration _config;

    public WorkspaceController(IConfiguration config) => _config = config;

    public IActionResult Index()
    {
        // The AngularJS front end reads this to know where the API lives.
        ViewBag.ApiBaseUrl = _config["Api:BaseUrl"] ?? "http://localhost:5080";
        return View();
    }

    /// <summary>The dedicated, Google-style search screen (paged, large-data friendly).</summary>
    public IActionResult Search()
    {
        ViewBag.ApiBaseUrl = _config["Api:BaseUrl"] ?? "http://localhost:5080";
        return View();
    }
}
