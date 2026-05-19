namespace Oracle_Health.Models.ViewModels;

public class LookupResponseViewModel
{
    public string? PatientName { get; set; }

    public List<LookupAppointmentViewModel> Appointments { get; set; }
        = new();
}