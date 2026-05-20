using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Oracle_Health.Reporting.Models;

namespace Oracle_Health.Reporting.Controllers;

[Route("reports")]
public class ReportsController : Controller
{
    private const string AccessTokenSessionKey = "ManagerAccessToken";
    private const string ManagerNameSessionKey = "ManagerName";

    private readonly IHttpClientFactory _httpClientFactory;

    public ReportsController(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        if (!IsSignedIn())
        {
            return RedirectToAction(nameof(Login));
        }

        var client = CreateAuthorizedClient();
        var model = new ReportsDashboardViewModel
        {
            ManagerName = HttpContext.Session.GetString(ManagerNameSessionKey) ?? "Clinic Manager",
            Summary = await client.GetFromJsonAsync<ClinicSummaryReport>("api/reports/summary"),
            DoctorWorkload = await client.GetFromJsonAsync<IReadOnlyList<DoctorWorkloadReportItem>>("api/reports/doctor-workload") ?? [],
            Cancellations = await client.GetFromJsonAsync<CancellationReport>("api/reports/cancellations"),
            AppointmentStatuses = await client.GetFromJsonAsync<IReadOnlyList<AppointmentStatusReportItem>>("api/reports/appointment-status") ?? []
        };

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

        return View(new ManagerLoginViewModel());
    }

    [HttpPost("login")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(ManagerLoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var client = _httpClientFactory.CreateClient("OracleHealthApi");
        var response = await client.PostAsJsonAsync("api/auth/token", new TokenRequest(model.Email, model.Password));

        if (!response.IsSuccessStatusCode)
        {
            ModelState.AddModelError(string.Empty, "Invalid manager email or password.");
            return View(model);
        }

        var token = await response.Content.ReadFromJsonAsync<TokenResponse>();
        if (token is null || token.Role != "Admin")
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

    private HttpClient CreateAuthorizedClient()
    {
        var client = _httpClientFactory.CreateClient("OracleHealthApi");
        var token = HttpContext.Session.GetString(AccessTokenSessionKey);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }
}
