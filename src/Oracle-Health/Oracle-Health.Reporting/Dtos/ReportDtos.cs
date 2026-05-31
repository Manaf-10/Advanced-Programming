namespace Oracle_Health.Reporting.Dtos;

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

public record RecentPatientReportItem(
    long PatientId,
    string PatientName,
    DateTime LastVisitDate,
    string DoctorName,
    string Summary);
