using Microsoft.AspNetCore.Mvc;
using Oracle_Health.Reporting.Models.ViewModels;
using Oracle_Health.Reporting.Services;

namespace Oracle_Health.Reporting.Controllers;

[Route("reports")]
public class ReportsController : Controller
{
    private const string AccessTokenSessionKey = "ManagerAccessToken";
    private const string ManagerNameSessionKey = "ManagerName";

    private readonly IClinicApiService _apiService;

    public ReportsController(IClinicApiService apiService)
    {
        _apiService = apiService;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        if (!IsSignedIn())
        {
            return RedirectToAction(nameof(Login));
        }

        var model = await _apiService.GetDashboardDataAsync();

        return View(model);
    }

    [HttpGet("statistics")]
    public Task<IActionResult> Statistics()
    {
        return Index();
    }

    [HttpGet("doctor-workload")]
    public Task<IActionResult> DoctorWorkload()
    {
        return Index();
    }

    [HttpGet("cancellations")]
    public Task<IActionResult> Cancellations()
    {
        return Index();
    }

    [HttpGet("login")]
    public IActionResult Login()
    {
        if (IsSignedIn())
        {
            return RedirectToAction(nameof(Index));
        }

        return View(new LoginViewModel());
    }

    [HttpPost("login")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var token = await _apiService.LoginAsync(model.Email, model.Password);
        if (token is null || token.Role != "Clinic Manager")
        {
            ModelState.AddModelError(string.Empty, "Only Clinic Manager accounts can access reporting.");
            return View(model);
        }

        HttpContext.Session.SetString(AccessTokenSessionKey, token.AccessToken);
        HttpContext.Session.SetString(ManagerNameSessionKey, token.FullName);

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("logout")]
    [ValidateAntiForgeryToken]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return RedirectToAction(nameof(Login));
    }

    private bool IsSignedIn()
    {
        return !string.IsNullOrWhiteSpace(HttpContext.Session.GetString(AccessTokenSessionKey));
    }

}
