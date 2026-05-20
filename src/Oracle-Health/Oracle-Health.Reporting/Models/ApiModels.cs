using System.ComponentModel.DataAnnotations;

namespace Oracle_Health.Reporting.Models;

public class ManagerLoginViewModel
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = "manager@oraclehealth.test";

    [Required]
    [DataType(DataType.Password)]
    public string Password { get; set; } = "Password123!";
}

public record TokenRequest(string Email, string Password);

public record TokenResponse(string AccessToken, DateTime ExpiresAtUtc, string Role, string FullName);

public record ClinicSummaryReport(
    int TotalAppointments,
    int UpcomingAppointments,
    int CompletedThisMonth,
    int ActiveDoctors,
    int RegisteredPatients);

public record DoctorWorkloadReportItem(
    long DoctorId,
    string DoctorName,
    int TotalAppointments,
    int CompletedAppointments,
    int CancelledOrMissedAppointments);

public record CancellationReport(
    int TotalAppointments,
    int CancelledAppointments,
    int MissedAppointments,
    decimal CancelledOrMissedRate);

public record AppointmentStatusReportItem(string Status, int Count);

public class ReportsDashboardViewModel
{
    public string ManagerName { get; set; } = string.Empty;

    public ClinicSummaryReport? Summary { get; set; }

    public IReadOnlyList<DoctorWorkloadReportItem> DoctorWorkload { get; set; } = [];

    public CancellationReport? Cancellations { get; set; }

    public IReadOnlyList<AppointmentStatusReportItem> AppointmentStatuses { get; set; } = [];
}
