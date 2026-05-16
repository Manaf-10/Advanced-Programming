namespace Oracle_Health.Models.ViewModels;

public class VisitRecordListItemViewModel
{
    public long Id { get; set; }

    public string PatientName { get; set; } = string.Empty;

    public string DoctorName { get; set; } = string.Empty;

    public DateTime AppointmentDate { get; set; }

    public DateTime? CreatedAt { get; set; }

    public string Notes { get; set; } = string.Empty;

    public string? Prescription { get; set; }
}
