using Microsoft.AspNetCore.Mvc;

namespace Oracle_Health.Reporting.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        return RedirectToAction("Index", "Reports");
    }
}
