using Oracle_Health.Reporting.Dtos;
using Oracle_Health.Reporting.Models.ViewModels;

namespace Oracle_Health.Reporting.Services;

public interface IClinicApiService
{
    Task<LoginResponse?> LoginAsync(string email, string password);

    Task<ReportsDashboardViewModel> GetDashboardDataAsync();
}
