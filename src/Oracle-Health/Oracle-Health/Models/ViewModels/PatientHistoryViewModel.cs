namespace Oracle_Health.Models.ViewModels;

public class PatientHistoryViewModel
{
    public string PatientName { get; set; } = string.Empty;
    public List<PatientHistoryItemViewModel> Visits { get; set; } = new();
}

public class PatientHistoryItemViewModel
{
    public long VisitId { get; set; }
    public string DoctorName { get; set; } = string.Empty;
    public string Specialization { get; set; } = string.Empty;
    public DateTime VisitDate { get; set; }
    public string Notes { get; set; } = string.Empty;
    public string? Prescription { get; set; }
}