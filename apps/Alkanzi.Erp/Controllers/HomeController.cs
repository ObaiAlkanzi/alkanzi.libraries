using System.Diagnostics;
using Alkanzi.Erp.Models;
using Microsoft.AspNetCore.Mvc;

namespace Alkanzi.Erp.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger) => _logger = logger;

    public IActionResult Index()
    {
        ViewData["Title"] = "Dashboard";
        return View();
    }

    /// <summary>
    /// Placeholder dashboard feed. There is no database wired up yet — this exists so the
    /// AngularJS + DevExtreme stack can be verified end to end (controller → $http → widget)
    /// before a persistence choice is made. Replace with a real query, not with more
    /// hardcoded rows.
    /// </summary>
    [HttpGet]
    public IActionResult DashboardData()
    {
        var orders = new[]
        {
            new { Id = 5001, Vendor = "Crystal Trading FZE",           Date = DateTime.Today.AddDays(-2),  Amount = 18_400m, Status = "Pending"  },
            new { Id = 5002, Vendor = "Golden Aluminium Est.",         Date = DateTime.Today.AddDays(-4),  Amount = 42_150m, Status = "Approved" },
            new { Id = 5003, Vendor = "Star Hardware Trading Co.",     Date = DateTime.Today.AddDays(-5),  Amount = 7_920m,  Status = "Approved" },
            new { Id = 5004, Vendor = "Falcon Sanitary Ware Trading",  Date = DateTime.Today.AddDays(-8),  Amount = 25_600m, Status = "Draft"    },
            new { Id = 5005, Vendor = "Emirates Electricals Trading",  Date = DateTime.Today.AddDays(-11), Amount = 61_300m, Status = "Pending"  },
            new { Id = 5006, Vendor = "National Trading Co.",          Date = DateTime.Today.AddDays(-13), Amount = 12_050m, Status = "Rejected" },
        };

        return Json(new
        {
            kpis = new[]
            {
                new { key = "orders",   label = "Purchase Orders", value = orders.Length },
                new { key = "pending",  label = "Pending Approval", value = orders.Count(o => o.Status == "Pending") },
                new { key = "vendors",  label = "Active Vendors",   value = orders.Select(o => o.Vendor).Distinct().Count() },
                new { key = "value",    label = "Total Value (AED)", value = (int)orders.Sum(o => o.Amount) },
            },
            orders,
            byVendor = orders
                .GroupBy(o => o.Vendor)
                .Select(g => new { vendor = g.Key, amount = g.Sum(x => x.Amount) })
                .OrderByDescending(x => x.amount)
                .ToArray()
        });
    }

    public IActionResult Privacy()
    {
        ViewData["Title"] = "Privacy";
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
        => View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
}
