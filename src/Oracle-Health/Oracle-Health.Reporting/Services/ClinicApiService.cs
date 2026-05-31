using System.Net.Http.Headers;
using System.Net.Http.Json;
using Oracle_Health.Reporting.Dtos;
using Oracle_Health.Reporting.Models.ViewModels;

namespace Oracle_Health.Reporting.Services;

public class ClinicApiService : IClinicApiService
{
    private const string AccessTokenSessionKey = "ManagerAccessToken";
    private const string ManagerNameSessionKey = "ManagerName";

    private readonly HttpClient _http;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ClinicApiService(HttpClient http, IHttpContextAccessor httpContextAccessor)
    {
        _http = http;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<LoginResponse?> LoginAsync(string email, string password)
    {
        var response = await _http.PostAsJsonAsync("api/auth/token", new LoginRequest(email, password));

        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<LoginResponse>()
            : null;
    }

    public async Task<ReportsDashboardViewModel> GetDashboardDataAsync()
    {
        AttachToken();

        return new ReportsDashboardViewModel
        {
            ManagerName = _httpContextAccessor.HttpContext?.Session.GetString(ManagerNameSessionKey) ?? "Clinic Manager",
            Summary = await _http.GetFromJsonAsync<ClinicSummaryReport>("api/reports/summary"),
            DoctorWorkload = await _http.GetFromJsonAsync<IReadOnlyList<DoctorWorkloadReportItem>>("api/reports/doctor-workload") ?? [],
            Cancellations = await _http.GetFromJsonAsync<CancellationReport>("api/reports/cancellations"),
            AppointmentStatuses = await _http.GetFromJsonAsync<IReadOnlyList<AppointmentStatusReportItem>>("api/reports/appointment-status") ?? [],
            RecentPatients = await _http.GetFromJsonAsync<IReadOnlyList<RecentPatientReportItem>>("api/reports/recent-patients") ?? []
        };
    }

    private void AttachToken()
    {
        var token = _httpContextAccessor.HttpContext?.Session.GetString(AccessTokenSessionKey);

        if (!string.IsNullOrWhiteSpace(token))
        {
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
    }
}
