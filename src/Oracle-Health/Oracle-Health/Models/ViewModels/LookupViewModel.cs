namespace Oracle_Health.Models.ViewModels;

public class LookupViewModel
{
    public long? PatientReference { get; set; }
    public List<LookupAppointmentViewModel> Appointments { get; set; } = new();
    public bool Searched { get; set; } = false;
    public string? PatientName { get; set; }
}

public class LookupAppointmentViewModel
{
    public string DoctorName { get; set; } = string.Empty;
    public string Specialization { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string Status { get; set; } = string.Empty;
}