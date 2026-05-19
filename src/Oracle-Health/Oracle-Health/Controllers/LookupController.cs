using Microsoft.AspNetCore.Mvc;
using Oracle_Health.Models.ViewModels;
using System.Net.Http.Json;

namespace Oracle_Health.Controllers;

public class LookupController : Controller
{
    private readonly HttpClient _httpClient;

    public LookupController(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient("OracleHealthApi");
    }

    [HttpGet]
    public IActionResult Index()
    {
        return View(new LookupViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(LookupViewModel form)
    {
        var viewModel = new LookupViewModel
        {
            PatientReference = form.PatientReference,
            Cpr = form.Cpr,
            Searched = true
        };

        if (form.Cpr == null)
        {
            ModelState.AddModelError(nameof(form.Cpr),
                "Please enter your CPR number.");
        }

        if (form.PatientReference == null)
        {
            ModelState.AddModelError(nameof(form.PatientReference),
                "Please enter a patient reference number.");
        }

        if (!ModelState.IsValid)
        {
            return View(viewModel);
        }

        try
        {
            var response = await _httpClient.GetAsync(
                $"api/appointments/lookup?patientReference={form.PatientReference}&cpr={form.Cpr}");

            if (!response.IsSuccessStatusCode)
            {
                var errorMessage = await response.Content.ReadAsStringAsync();

                ModelState.AddModelError(string.Empty,
                    "No appointment information found.");

                return View(viewModel);
            }

            var result = await response.Content
                .ReadFromJsonAsync<LookupResponseViewModel>();

            var raw = await response.Content.ReadAsStringAsync();
            Console.WriteLine(raw);

            if (result == null)
            {
                ModelState.AddModelError(string.Empty,
                    "Unable to retrieve appointment data.");

                return View(viewModel);
            }

            viewModel.PatientName = result.PatientName;
            viewModel.Appointments = result.Appointments;

            return View(viewModel);
        }
        catch
        {
            ModelState.AddModelError(string.Empty,
                "Unable to connect to the lookup service.");

            return View(viewModel);
        }
    }
}