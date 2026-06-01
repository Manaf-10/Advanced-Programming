using Oracle_Health.Reporting.Dtos;

namespace Oracle_Health.Reporting.Models.ViewModels;

public class ReportsDashboardViewModel
{
    public string ManagerName { get; set; } = string.Empty;

    public ClinicSummaryReport? Summary { get; set; }

    public IReadOnlyList<DoctorWorkloadReportItem> DoctorWorkload { get; set; } = [];

    public CancellationReport? Cancellations { get; set; }

    public IReadOnlyList<AppointmentStatusReportItem> AppointmentStatuses { get; set; } = [];

    public IReadOnlyList<RecentPatientReportItem> RecentPatients { get; set; } = [];
}
