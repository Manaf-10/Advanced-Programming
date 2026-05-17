namespace Oracle_Health.Models.ViewModels;

public class PatientPrescriptionsViewModel
{
    public string PatientName { get; set; } = string.Empty;
    public List<PrescriptionItemViewModel> Prescriptions { get; set; } = new();
}

public class PrescriptionItemViewModel
{
    public long VisitId { get; set; }
    public string DoctorName { get; set; } = string.Empty;
    public string Specialization { get; set; } = string.Empty;
    public DateTime VisitDate { get; set; }
    public string Prescription { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
}
